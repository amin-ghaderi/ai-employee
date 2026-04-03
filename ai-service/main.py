import json
import logging

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from ai.llama_client import JudgeOutputError, LlamaClient
from db import SessionLocal
from history_format import build_prompt_input
from repositories.judgment_repository import JudgmentRepository


app = FastAPI(title="AiEmployee AI Service")
llama_client = LlamaClient()


class JudgeRequest(BaseModel):
    user_id: str
    text: str


class JudgeResponse(BaseModel):
    winner: str
    reason: str


class LeadClassificationRequest(BaseModel):
    prompt: str


class LeadClassificationResponse(BaseModel):
    user_type: str
    intent: str
    potential: str


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
    try:
        if not request.user_id or not request.user_id.strip():
            raise HTTPException(status_code=400, detail="user_id is required")

        user_id = request.user_id.strip()

        db = SessionLocal()
        try:
            repo = JudgmentRepository(db)
            # TEMP: Memory disabled to avoid bias in judgment (no prior judgments in prompt).
            prompt_input = build_prompt_input(request.text)

            raw = await llama_client.ask(prompt_input)
            data = json.loads(raw)
            winner = data["winner"]
            reason = data["reason"]

            repo.save_judgment(
                user_id=user_id,
                input_text=request.text,
                winner=winner,
                reason=reason,
            )
        finally:
            db.close()

        return JudgeResponse(winner=winner, reason=reason)
    except JudgeOutputError as e:
        raise HTTPException(status_code=502, detail=str(e)) from e
    except (json.JSONDecodeError, KeyError, TypeError) as e:
        raise HTTPException(status_code=502, detail=f"Invalid judge JSON: {e}") from e
