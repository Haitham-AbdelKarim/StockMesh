"""Top-sellers tool."""

from langchain_core.tools import tool

from ._http import api_get, observe


def make_get_top_sellers(api_base: str, auth_token: str):
    @tool
    async def get_top_sellers(days: int = 30, top: int = 5) -> str:
        """Best-selling products for the store over the last N days."""
        data = await api_get(
            api_base,
            auth_token,
            "/api/v1/dashboard/top-sellers",
            {"days": max(1, min(days, 90)), "top": max(1, min(top, 20))},
        )
        return observe(data)

    return get_top_sellers
