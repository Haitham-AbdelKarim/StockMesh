"""Sales trend tool."""

from langchain_core.tools import tool

from ._http import api_get, observe


def make_get_sales_trend(api_base: str, auth_token: str):
    @tool
    async def get_sales_trend(days: int = 30) -> str:
        """Daily sales totals for the store over the last N days (1-90)."""
        data = await api_get(
            api_base,
            auth_token,
            "/api/v1/dashboard/sales-trend",
            {"days": max(1, min(days, 90))},
        )
        return observe(data)

    return get_sales_trend
