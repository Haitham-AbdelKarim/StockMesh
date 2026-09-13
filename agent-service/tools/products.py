"""Store catalog tool."""

from langchain_core.tools import tool

from ._http import api_get, observe


def make_get_store_products(api_base: str, auth_token: str):
    @tool
    async def get_store_products(search: str = "", page_size: int = 10) -> str:
        """List the store's product catalog, optionally filtered by search text."""
        data = await api_get(
            api_base,
            auth_token,
            "/api/v1/products",
            {"search": search, "page": 1, "pageSize": max(1, min(page_size, 20))},
        )
        return observe(data)

    return get_store_products
