"""Prebuilt-loop tests: grounding, declarative cap, failure recovery.

All deterministic — the fake model replaces the LLM and fake tools replace
HTTP, so no API key or network access is needed.
"""

import sys
from pathlib import Path

import pytest
from langchain_core.messages import AIMessage, ToolMessage
from langchain_core.tools import tool

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from agent import answer_stream, remember_requests, status_frames, tool_frames
from tools._http import guard_coroutine


class FakeModel:
    """Minimal bind_tools/ainvoke stand-in for the chat model."""

    def __init__(self, script):
        self._script = list(script)

    def bind_tools(self, tools, **kwargs):
        return self

    def bind(self, **kwargs):
        return self

    async def ainvoke(self, messages):
        return self._script.pop(0)


@tool
async def fake_top_sellers(days: int = 30) -> str:
    """Top sellers."""
    return '{"items": [{"name": "Paracetamol 500mg", "units": 42}]}'


@tool
async def fake_broken() -> str:
    """Always fails."""
    raise RuntimeError("backend exploded")


# Same guard production applies in build_tools: unexpected exceptions
# become error observations instead of crashing the loop.
fake_broken.coroutine = guard_coroutine(fake_broken.coroutine)


def _tool_call(name, args, call_id="call-1"):
    return AIMessage(
        content="",
        tool_calls=[{"name": name, "args": args, "id": call_id, "type": "tool_call"}],
    )


async def _collect(**kwargs):
    return [frame async for frame in answer_stream(**kwargs)]


def _base_kwargs(**overrides):
    params = {
        "model": None,
        "tools": [fake_top_sellers, fake_broken],
        "question": "What sells best?",
        "history": [],
        "conversation_id": "conv-1",
        "model_version": "test-model",
        "max_tool_calls": 5,
    }
    params.update(overrides)
    return params


@pytest.mark.asyncio
async def test_grounded_answer_references_tool_data():
    model = FakeModel(
        [
            _tool_call("fake_top_sellers", {"days": 30}),
            AIMessage(content="Paracetamol 500mg leads with 42 units sold."),
        ]
    )

    frames = await _collect(**_base_kwargs(model=model))

    events = [frame["event"] for frame in frames]
    assert "tool_call" in events
    assert events[-1] == "done"

    done = frames[-1]["data"]
    assert "Paracetamol 500mg" in done["answer"]
    assert "42" in done["answer"]
    assert done["toolCalls"][0]["name"] == "fake_top_sellers"
    assert done["modelVersion"] == "test-model"
    assert done["conversationId"] == "conv-1"


@pytest.mark.asyncio
async def test_tool_call_cap_stops_at_five():
    script = [_tool_call("fake_top_sellers", {}, f"call-{i}") for i in range(6)]
    script.append(AIMessage(content="Answer from partial data."))
    model = FakeModel(script)

    frames = await _collect(**_base_kwargs(model=model))

    tool_calls = [frame for frame in frames if frame["event"] == "tool_call"]
    assert len(tool_calls) == 5
    assert frames[-1]["event"] == "done"
    assert "partial data" in frames[-1]["data"]["answer"]


@pytest.mark.asyncio
async def test_tool_failure_recovers_with_answer():
    model = FakeModel(
        [
            _tool_call("fake_broken", {}),
            AIMessage(content="The backend is unavailable, so I have no data."),
        ]
    )

    frames = await _collect(**_base_kwargs(model=model))

    assert frames[-1]["event"] == "done"
    assert "unavailable" in frames[-1]["data"]["answer"]
    assert frames[-1]["data"]["toolCalls"][0]["name"] == "fake_broken"


def test_status_frames_map_agent_requests():
    message = _tool_call("fake_top_sellers", {"days": 7})

    frames = status_frames([message])

    assert [frame["event"] for frame in frames] == ["status"]
    assert "fake_top_sellers" in frames[0]["data"]["message"]


def test_tool_frames_join_args_and_skip_limit_blocks():
    pending: dict = {}
    remember_requests([_tool_call("fake_top_sellers", {"days": 7}, "call-1")], pending)
    remember_requests([_tool_call("fake_top_sellers", {}, "call-2")], pending)

    messages = [
        ToolMessage(content='{"items": []}', tool_call_id="call-1", name="fake_top_sellers"),
        ToolMessage(
            content="Tool call limit exceeded. Do not make additional tool calls.",
            tool_call_id="call-2",
            name="fake_top_sellers",
            status="error",
        ),
    ]

    frames = tool_frames(messages, pending)

    assert [frame["event"] for frame in frames] == ["tool_call"]
    assert frames[0]["data"]["args"] == {"days": 7}
    assert pending == {}
