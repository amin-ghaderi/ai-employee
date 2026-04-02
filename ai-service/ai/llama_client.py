import asyncio
import json
import logging
import os
from typing import Any

import httpx

logger = logging.getLogger(__name__)
logger.setLevel(logging.INFO)
if not logger.handlers:
    _stream_handler = logging.StreamHandler()
    _stream_handler.setFormatter(
        logging.Formatter("%(asctime)s [%(levelname)s] %(name)s: %(message)s")
    )
    logger.addHandler(_stream_handler)
    logger.propagate = False

OLLAMA_GENERATE_URL = os.environ.get(
    "OLLAMA_BASE_URL", "http://localhost:11434"
).rstrip("/") + "/api/generate"
OLLAMA_MODEL = os.getenv("OLLAMA_MODEL", "phi3")
OLLAMA_TIMEOUT = float(os.environ.get("OLLAMA_TIMEOUT_SECONDS", "120"))

logger.info("Using LLM model: %s", OLLAMA_MODEL)

# 1 initial attempt + 3 retries; backoff before attempts 2–4
_MAX_OLLAMA_ATTEMPTS = 4
_RETRY_BACKOFF_SECONDS = (0.5, 1.0, 2.0)

_PROMPT_LOG_MAX_CHARS = 200
_RESPONSE_LOG_MAX_CHARS = 4000


class JudgeOutputError(Exception):
    """Raised when Ollama output is missing, invalid JSON, or fails validation."""


JUDGE_PROMPT_TEMPLATE = """You are an impartial AI judge.

Your task is to analyze a disagreement between two people and decide who has the stronger argument.

IMPORTANT RULES:

* You MUST return ONLY valid JSON.
* Do NOT include any explanation outside the JSON.
* Do NOT include markdown, text, or formatting.
* The JSON must match EXACTLY this schema:

{
"winner": "string",
"reason": "string"
}

GUIDELINES:

* The "winner" must be the name of the person with the stronger argument.
* The "reason" must be a short, clear explanation (1-2 sentences).
* Be neutral and logical. Do not take sides emotionally.

INPUT:
{{input}}
"""


def _truncate(text: str, max_chars: int) -> str:
    if len(text) <= max_chars:
        return text
    return text[:max_chars] + "…[truncated]"


def _extract_json_object(text: str) -> dict[str, Any]:
    """Parse strict JSON from model output; allow minimal surrounding noise."""
    text = text.strip()
    if not text:
        raise JudgeOutputError("Empty model response")

    try:
        parsed = json.loads(text)
    except json.JSONDecodeError:
        start = text.find("{")
        end = text.rfind("}")
        if start == -1 or end == -1 or end <= start:
            raise JudgeOutputError("Model response is not valid JSON") from None
        try:
            parsed = json.loads(text[start : end + 1])
        except json.JSONDecodeError as e:
            raise JudgeOutputError("Model response is not valid JSON") from e

    if not isinstance(parsed, dict):
        raise JudgeOutputError("Parsed JSON must be an object with winner and reason")
    return parsed


def _validate_judgment(data: dict[str, Any]) -> None:
    winner = data.get("winner")
    reason = data.get("reason")

    if winner is None or not str(winner).strip():
        raise JudgeOutputError('Field "winner" is missing or empty')
    if reason is None or not str(reason).strip():
        raise JudgeOutputError('Field "reason" is missing or empty')


async def _post_ollama_generate(client: httpx.AsyncClient, payload: dict[str, Any]) -> dict[str, Any]:
    """
    POST to Ollama /api/generate with retries on network errors and timeouts only.
    HTTP status errors are not retried.
    """
    last_error: Exception | None = None

    for attempt in range(_MAX_OLLAMA_ATTEMPTS):
        if attempt > 0:
            delay = _RETRY_BACKOFF_SECONDS[attempt - 1]
            logger.warning(
                "ollama_retry_backoff | attempt=%s/%s delay_s=%s last_error=%r",
                attempt + 1,
                _MAX_OLLAMA_ATTEMPTS,
                delay,
                last_error,
            )
            await asyncio.sleep(delay)

        try:
            response = await client.post(OLLAMA_GENERATE_URL, json=payload)
            response.raise_for_status()
            try:
                body = response.json()
            except ValueError as e:
                logger.error(
                    "ollama_body_not_json | error=%s",
                    e,
                    exc_info=True,
                )
                raise JudgeOutputError("Ollama returned invalid JSON") from e
            return body
        except httpx.HTTPStatusError as e:
            logger.error(
                "ollama_http_status_error | status=%s body_preview=%s",
                e.response.status_code,
                _truncate(e.response.text, 500),
                exc_info=True,
            )
            raise JudgeOutputError(f"Ollama HTTP error: {e.response.status_code}") from e
        except httpx.RequestError as e:
            last_error = e
            logger.error(
                "ollama_request_failed | attempt=%s/%s error=%s",
                attempt + 1,
                _MAX_OLLAMA_ATTEMPTS,
                e,
                exc_info=True,
            )

    raise JudgeOutputError("LLM service unavailable after retries") from last_error


class LlamaClient:
    async def ask(self, user_input: str) -> str:
        full_prompt = JUDGE_PROMPT_TEMPLATE.replace("{{input}}", user_input)

        logger.info(
            "llm_call_starting | prompt_preview=%s",
            _truncate(full_prompt, _PROMPT_LOG_MAX_CHARS),
        )

        payload = {
            "model": OLLAMA_MODEL,
            "prompt": full_prompt,
            "stream": False,
        }

        async with httpx.AsyncClient(timeout=OLLAMA_TIMEOUT) as client:
            ollama_body = await _post_ollama_generate(client, payload)

        if not isinstance(ollama_body, dict) or "response" not in ollama_body:
            logger.error(
                "ollama_response_missing_field | keys=%s",
                list(ollama_body.keys()) if isinstance(ollama_body, dict) else type(ollama_body),
            )
            raise JudgeOutputError("Ollama response missing 'response' field")

        raw_model_text = ollama_body["response"]
        if not isinstance(raw_model_text, str):
            raise JudgeOutputError("Ollama 'response' must be a string")

        logger.info(
            "llm_raw_response | response_preview=%s",
            _truncate(raw_model_text, _RESPONSE_LOG_MAX_CHARS),
        )

        try:
            parsed = _extract_json_object(raw_model_text)
            _validate_judgment(parsed)
        except JudgeOutputError as e:
            logger.error("llm_output_validation_failed | error=%s", e, exc_info=True)
            raise

        winner = str(parsed["winner"]).strip()
        reason = str(parsed["reason"]).strip()
        return json.dumps({"winner": winner, "reason": reason}, ensure_ascii=False)
