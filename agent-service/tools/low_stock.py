"""Low-stock tool."""

from langchain_core.tools import tool

from ._http import api_get, observe


def make_get_low_stock_items(api_base: str, auth_token: str):
    @tool
    async def get_low_stock_items() -> str:
        """Products running low on stock for the store."""
        data = await api_get(api_base, auth_token, "/api/v1/dashboard/low-stock")
        return observe(data)

    return get_low_stock_items
