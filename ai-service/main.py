from fastapi import FastAPI
from pydantic import BaseModel

from ai.llama_client import LlamaClient


app = FastAPI(title="AiEmployee AI Service")
llama_client = LlamaClient()


class JudgeRequest(BaseModel):
    text: str


class JudgeResponse(BaseModel):
    result: str


@app.post("/ai/judge", response_model=JudgeResponse)
async def judge(request: JudgeRequest) -> JudgeResponse:
    _ = await llama_client.ask(request.text)
    return JudgeResponse(result="Mock AI response")
