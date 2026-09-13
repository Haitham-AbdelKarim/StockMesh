"""Shared HTTP plumbing for the read-only .NET API tools.
Every tool uses the requesting user's own JWT (passed per request from the
factory in ``tools/__init__.py``), so tenant isolation is inherited — the
agent structurally cannot see another store's data. Credentials live in
per-request closures, never in module globals: concurrent ``/ask`` calls
carry different users' tokens.
"""

from __future__ import annotations

import functools
import json
from typing import Any, Callable

import httpx

TOOL_TIMEOUT_SECONDS = 10.0
MAX_OBSERVATION_CHARS = 3000


async def api_get(
    api_base: str,
    auth_token: str,
    path: str,
    params: dict[str, Any] | None = None,
) -> Any:
    url = api_base.rstrip("/") + path

    try:
        async with httpx.AsyncClient(timeout=TOOL_TIMEOUT_SECONDS) as client:
            response = await client.get(
                url,
                headers={"Authorization": f"Bearer {auth_token}"},
                params=params or {},
            )
            response.raise_for_status()
            return response.json()
    except Exception as exc:
        return {"error": f"Tool request to {path} failed: {exc}"}


def observe(value: Any) -> str:
    """Serialize a tool result, truncated to bound prompt size."""
    text = json.dumps(value, default=str)

    if len(text) > MAX_OBSERVATION_CHARS:
        return json.dumps({"truncated": True, "preview": text[:MAX_OBSERVATION_CHARS]})

    return text


def guard_coroutine(fn: Callable) -> Callable:
    """Turn an unexpected tool exception into an error observation.

    Applied centrally in ``build_tools`` so no tool can crash the loop —
    the model sees the failure and can continue or answer accordingly.
    """

    @functools.wraps(fn)
    async def wrapper(*args: Any, **kwargs: Any) -> str:
        try:
            return await fn(*args, **kwargs)
        except Exception as exc:
            return f"Tool failed unexpectedly: {exc}."

    return wrapper
