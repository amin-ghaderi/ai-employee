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
from history_format import build_prompt_input, format_history
from repositories.judgment_repository import JudgmentRepository


app = FastAPI(title="AiEmployee AI Service")
llama_client = LlamaClient()


class JudgeRequest(BaseModel):
    user_id: str
    text: str


class JudgeResponse(BaseModel):
    winner: str
    reason: str


@app.post("/ai/judge", response_model=JudgeResponse)
async def judge(request: JudgeRequest) -> JudgeResponse:
    try:
        if not request.user_id or not request.user_id.strip():
            raise HTTPException(status_code=400, detail="user_id is required")

        user_id = request.user_id.strip()

        db = SessionLocal()
        try:
            repo = JudgmentRepository(db)
            recent = repo.get_recent_judgments(user_id=user_id, limit=5)
            history = format_history(recent)
            prompt_input = build_prompt_input(history, request.text)

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
