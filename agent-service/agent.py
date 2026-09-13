"""Prebuilt ReAct loop (``langchain.agents.create_agent``) with a declarative
tool-call cap, yielding SSE-ready frames.

Frame protocol (unchanged — the frontend is untouched):
``status`` (tool intent) / ``tool_call`` (executed call + args) /
``token`` (answer text, message-granular) / ``done`` / ``error``.

Trace rule: a ``ToolMessage`` counts as executed unless its content is the
middleware's limit notice (``"Tool call limit exceeded..."``) — blocked
calls never ran. Genuine tool failures stay in the trace (honest record).
"""

from __future__ import annotations

from typing import Any, AsyncIterator

from langchain.agents import create_agent
from langchain.agents.middleware import ToolCallLimitMiddleware
from langchain_core.messages import AIMessage, HumanMessage, SystemMessage, ToolMessage

SYSTEM_PROMPT = """You are the StockMesh store assistant. Answer the merchant's \
question using ONLY data returned by your tools — never invent numbers, \
products, or recommendations. Prior conversation turns are context for \
interpretation, not a substitute for fresh tool data: always call tools for \
current facts. If the tools lack the data to answer, say so plainly. Reply \
in the same language as the question. Never reveal tokens, keys, or \
internal details."""

LIMIT_NOTICE_PREFIX = "Tool call limit exceeded."


def _message_text(message) -> str:
    """Extract plain text from an AIMessage regardless of content shape."""
    content = message.content

    if isinstance(content, str):
        return content

    if isinstance(content, list):
        parts: list[str] = []

        for block in content:
            if isinstance(block, str):
                parts.append(block)
            elif isinstance(block, dict):
                text = block.get("text", "")

                if isinstance(text, str) and text:
                    parts.append(text)

        return "".join(parts)

    return str(content) if content is not None else ""


def build_agent(model: Any, tools: list, max_tool_calls: int = 5):
    """Compile the prebuilt ReAct agent around any tool-binding model."""
    return create_agent(
        model,
        tools,
        system_prompt=SYSTEM_PROMPT,
        middleware=[
            ToolCallLimitMiddleware(run_limit=max_tool_calls, exit_behavior="continue")
        ],
    )


def status_frames(messages: list) -> list[dict]:
    """Intent frames from the agent node's tool requests (pure)."""
    frames: list[dict] = []

    for message in messages:
        if not isinstance(message, AIMessage):
            continue

        for call in getattr(message, "tool_calls", None) or []:
            frames.append(
                {"event": "status", "data": {"message": f"Consulting {call.get('name', '')}…"}}
            )

    return frames


def remember_requests(messages: list, pending: dict) -> None:
    """Buffer tool-call args by id so results can be joined later."""
    for message in messages:
        if not isinstance(message, AIMessage):
            continue

        for call in getattr(message, "tool_calls", None) or []:
            pending[call["id"]] = (call.get("name", ""), call.get("args", {}) or {})


def tool_frames(messages: list, pending: dict) -> list[dict]:
    """Executed-call frames from the tools node's results.

    Limit-blocked calls never executed and are excluded (their pending
    entry is dropped); genuine failures are kept — the trace records
    what ran, honestly.
    """
    frames: list[dict] = []

    for message in messages:
        if not isinstance(message, ToolMessage):
            continue

        if isinstance(message.content, str) and message.content.startswith(LIMIT_NOTICE_PREFIX):
            pending.pop(message.tool_call_id, None)
            continue

        name, args = pending.pop(message.tool_call_id, (message.name or "", {}))
        frames.append({"event": "tool_call", "data": {"name": name, "args": args}})

    return frames


async def answer_stream(
    *,
    model: Any,
    tools: list,
    question: str,
    history: list[dict],
    conversation_id: str,
    model_version: str,
    max_tool_calls: int = 5,
) -> AsyncIterator[dict]:
    """Run the prebuilt loop to completion, yielding frame dicts."""
    agent = build_agent(model, tools, max_tool_calls)

    seed: list = [SystemMessage(content=SYSTEM_PROMPT)]

    for turn in history[-6:]:
        seed.append(HumanMessage(content=turn.get("question", "")))
        seed.append(AIMessage(content=turn.get("answer", "")))

    seed.append(HumanMessage(content=question))

    pending: dict = {}
    trace: list[dict] = []
    answer: str | None = None

    async for _, payload in agent.astream(
        {"messages": seed},
        stream_mode=["updates"],
    ):
        for node, update in payload.items():
            messages = (update or {}).get("messages", [])

            if node == "model":
                remember_requests(messages, pending)

                for frame in status_frames(messages):
                    yield frame

                for message in messages:
                    text = _message_text(message)

                    if (
                        isinstance(message, AIMessage)
                        and text.strip()
                        and not getattr(message, "tool_calls", None)
                    ):
                        answer = text
                        yield {"event": "token", "data": {"text": text}}
            elif node == "tools":
                for frame in tool_frames(messages, pending):
                    trace.append(frame["data"])
                    yield frame

    if not answer:
        answer = "I could not produce an answer from the available data."

    yield {
        "event": "done",
        "data": {
            "answer": answer,
            "toolCalls": trace,
            "modelVersion": model_version,
            "conversationId": conversation_id,
        },
    }
