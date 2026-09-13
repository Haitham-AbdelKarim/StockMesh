"""Recommendations tool."""

from langchain_core.tools import tool

from ._http import api_get, observe


def make_get_current_recommendations(api_base: str, auth_token: str):
    @tool
    async def get_current_recommendations(action: str = "") -> str:
        """Current AI recommendations for the store, optionally filtered by
        action (Hold, Reorder, Share, UrgentShare, MarketOpportunity)."""
        params: dict = {"page": 1, "pageSize": 20}

        if action:
            params["recommendedAction"] = action

        data = await api_get(api_base, auth_token, "/api/v1/recommendations", params)
        return observe(data)

    return get_current_recommendations
