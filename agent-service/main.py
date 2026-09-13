"""FastAPI entrypoint: thin SSE layer over the agent ReAct loop."""

from __future__ import annotations

import json
import logging
import os

import httpx
from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field

from agent import answer_stream
from tools import build_tools

from dotenv import load_dotenv

from langchain_google_genai import ChatGoogleGenerativeAI

logger = logging.getLogger("agent-service")

load_dotenv()

app = FastAPI(title="StockMesh Agent Service")

MAX_TOOL_CALLS = int(os.environ.get("MAX_TOOL_CALLS", "5"))
MODEL_NAME = os.environ.get("GEMINI_MODEL", "gemini-3.6-flash")


class HistoryTurn(BaseModel):
    question: str = ""
    answer: str = ""


class AskRequest(BaseModel):
    question: str = Field(min_length=1, max_length=1000)
    conversation_id: str = ""
    auth_token: str = ""
    store_id: str = ""
    user_id: str = ""
    history: list[HistoryTurn] = []


def _model():
    api_key = os.environ.get("GEMINI_API_KEY", "")

    if not api_key:
        raise HTTPException(
            status_code=503,
            detail="Agent LLM is not configured (GEMINI_API_KEY missing).",
        )


    return ChatGoogleGenerativeAI(
        model=MODEL_NAME, google_api_key=api_key, temperature=0.2
    )


def _settings():
    return {
        "api_base": os.environ.get("API_BASE_URL", ""),
        "callback_url": os.environ.get("AGENT_CALLBACK_URL", ""),
        "internal_key": os.environ.get("AGENT_INTERNAL_KEY", ""),
    }


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


def _format_frame(frame: dict) -> str:
    return f"event: {frame['event']}\ndata: {json.dumps(frame['data'])}\n\n"


@app.post("/ask")
async def ask(request: AskRequest):
    settings = _settings()
    model = _model()

    async def event_stream():
        trace: list[dict] = []
        answer = ""
        done: dict | None = None

        try:
            async for frame in answer_stream(
                model=model,
                tools=build_tools(settings["api_base"], request.auth_token),
                question=request.question,
                history=[turn.model_dump() for turn in request.history],
                conversation_id=request.conversation_id,
                model_version=MODEL_NAME,
                max_tool_calls=MAX_TOOL_CALLS,
            ):
                if frame["event"] == "tool_call":
                    trace.append(frame["data"])

                if frame["event"] == "done":
                    done = frame["data"]
                    answer = done.get("answer", "")

                yield _format_frame(frame)
        except Exception as exc:
            logger.exception("Agent loop failed.")
            yield _format_frame({"event": "error", "data": {"message": str(exc)[:500]}})
            return

        if not answer.strip():
            answer = "I could not produce an answer from the available data."

        await _persist_log(request, trace, answer, settings)

    return StreamingResponse(event_stream(), media_type="text/event-stream")


async def _persist_log(
    request: AskRequest, trace: list[dict], answer: str, settings: dict
) -> None:
    """Fire-and-forget audit callback — a lost log must never fail the answer."""
    if not settings["callback_url"] or not settings["internal_key"]:
        return

    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            response = await client.post(
                settings["callback_url"].rstrip("/") + "/api/v1/agent-logs",
                headers={"X-Agent-Internal-Key": settings["internal_key"]},
                json={
                    "storeId": request.store_id,
                    "userId": request.user_id,
                    "conversationId": request.conversation_id,
                    "question": request.question,
                    "toolCalls": json.dumps(trace)[:8000],
                    "finalAnswer": answer[:8000],
                },
            )

            if response.status_code >= 400:
                logger.warning("Agent log callback %s: %s", response.status_code, response.text[:500])
    except Exception:
        logger.exception("Agent log callback failed.")
