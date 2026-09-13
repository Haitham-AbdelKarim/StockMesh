"""Network demand (market signal) tool."""

from langchain_core.tools import tool

from ._http import api_get, observe


def make_get_network_demand(api_base: str, auth_token: str):
    @tool
    async def get_network_demand(product_id: str, days: int = 90) -> str:
        """Vertical-wide network demand history (transfer volume, reservations)
        for one product — the market signal behind MarketOpportunity."""
        data = await api_get(
            api_base,
            auth_token,
            "/api/v1/recommendations/market",
            {"productId": product_id, "days": max(1, min(days, 365))},
        )
        return observe(data)

    return get_network_demand
