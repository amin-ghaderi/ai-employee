import json
import logging

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

from ai.llama_client import JudgeOutputError, LlamaClient


app = FastAPI(title="AiEmployee AI Service")
llama_client = LlamaClient()


class JudgeRequest(BaseModel):
    text: str


class JudgeResponse(BaseModel):
    winner: str
    reason: str


@app.post("/ai/judge", response_model=JudgeResponse)
async def judge(request: JudgeRequest) -> JudgeResponse:
    try:
        raw = await llama_client.ask(request.text)
        data = json.loads(raw)
        return JudgeResponse(winner=data["winner"], reason=data["reason"])
    except JudgeOutputError as e:
        raise HTTPException(status_code=502, detail=str(e)) from e
    except (json.JSONDecodeError, KeyError, TypeError) as e:
        raise HTTPException(status_code=502, detail=f"Invalid judge JSON: {e}") from e
