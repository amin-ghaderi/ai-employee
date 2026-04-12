import json
import logging

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from ai.llama_client import JudgeOutputError, LlamaClient

_log = logging.getLogger(__name__)

app = FastAPI(title="AiEmployee AI Service")
llama_client = LlamaClient()


class JudgeRequest(BaseModel):
    user_id: str
    text: str


class JudgeFullRequest(BaseModel):
    user_id: str
    prompt: str


class JudgeResponse(BaseModel):
    winner: str
    reason: str


class LeadClassificationRequest(BaseModel):
    prompt: str


class LeadClassificationResponse(BaseModel):
    user_type: str
    intent: str
    potential: str


class ChatRequest(BaseModel):
    user_id: str
    prompt: str


class ChatResponse(BaseModel):
    response: str


@app.post("/ai/chat", response_model=ChatResponse)
async def chat(request: ChatRequest) -> ChatResponse:
    """Plain assistant completion for the chat flow (Ollama /api/generate)."""
    try:
        if not request.user_id or not request.user_id.strip():
            raise HTTPException(status_code=400, detail="user_id is required")

        if not request.prompt or not request.prompt.strip():
            raise HTTPException(status_code=400, detail="prompt is required")

        user_id = request.user_id.strip()
        _log.info("chat | user_id=%s", user_id)

        text = await llama_client.chat_plain(request.prompt)
        return ChatResponse(response=text)
    except JudgeOutputError as e:
        raise HTTPException(status_code=502, detail=str(e)) from e


@app.post("/ai/lead/classify", response_model=LeadClassificationResponse)
async def classify_lead(request: LeadClassificationRequest) -> LeadClassificationResponse:
    try:
        if not request.prompt or not request.prompt.strip():
            raise HTTPException(status_code=400, detail="prompt is required")

        prompt = request.prompt.strip()

        raw = await llama_client.ask_raw(prompt)
        data = json.loads(raw)

        return LeadClassificationResponse(
            user_type=data["user_type"],
            intent=data["intent"],
            potential=data["potential"],
        )
    except JudgeOutputError as e:
        raise HTTPException(status_code=502, detail=str(e)) from e
    except (json.JSONDecodeError, KeyError, TypeError) as e:
        raise HTTPException(status_code=502, detail=f"Invalid classification JSON: {e}") from e


@app.post("/ai/judge", response_model=JudgeResponse)
async def judge(request: JudgeRequest) -> JudgeResponse:
    """
    Deprecated: use POST /ai/judge/full with a caller-built full prompt.
    This endpoint forwards `text` to the model as-is (no server-side template).
    """
    try:
        if not request.user_id or not request.user_id.strip():
            raise HTTPException(status_code=400, detail="user_id is required")

        if not request.text or not request.text.strip():
            raise HTTPException(status_code=400, detail="text is required")

        user_id = request.user_id.strip()
        _log.warning(
            "DEPRECATED /ai/judge | user_id=%s | "
            "Migrate to POST /ai/judge/full with the full judge prompt built upstream.",
            user_id,
        )

        raw = await llama_client.run_judge_prompt(request.text)
        data = json.loads(raw)
        winner = data["winner"]
        reason = data["reason"]

        return JudgeResponse(winner=winner, reason=reason)
    except JudgeOutputError as e:
        raise HTTPException(status_code=502, detail=str(e)) from e
    except (json.JSONDecodeError, KeyError, TypeError) as e:
        raise HTTPException(status_code=502, detail=f"Invalid judge JSON: {e}") from e


@app.post("/ai/judge/full", response_model=JudgeResponse)
async def judge_full(request: JudgeFullRequest) -> JudgeResponse:
    """Accept a complete judge prompt; execute on Ollama; return validated winner/reason."""
    try:
        if not request.user_id or not request.user_id.strip():
            raise HTTPException(status_code=400, detail="user_id is required")

        if not request.prompt or not request.prompt.strip():
            raise HTTPException(status_code=400, detail="prompt is required")

        user_id = request.user_id.strip()
        _log.info("judge_full | user_id=%s", user_id)

        raw = await llama_client.run_judge_prompt(request.prompt)
        data = json.loads(raw)
        winner = data["winner"]
        reason = data["reason"]

        return JudgeResponse(winner=winner, reason=reason)
    except JudgeOutputError as e:
        raise HTTPException(status_code=502, detail=str(e)) from e
    except (json.JSONDecodeError, KeyError, TypeError) as e:
        raise HTTPException(status_code=502, detail=f"Invalid judge JSON: {e}") from e
