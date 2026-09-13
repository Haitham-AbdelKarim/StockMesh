"""Tool assembly: one file per tool, combined here into the agent's list.

Credentials are bound per request via the factory — never module globals —
so concurrent conversations cannot leak tokens across tenants.
"""

from ._http import guard_coroutine
from .low_stock import make_get_low_stock_items
from .network_demand import make_get_network_demand
from .products import make_get_store_products
from .recommendations import make_get_current_recommendations
from .sales_trend import make_get_sales_trend
from .top_sellers import make_get_top_sellers

__all__ = ["build_tools"]


def build_tools(api_base: str, auth_token: str) -> list:
    """Create the six read-only tools bound to one request's credentials."""
    tools = [
        make_get_store_products(api_base, auth_token),
        make_get_sales_trend(api_base, auth_token),
        make_get_network_demand(api_base, auth_token),
        make_get_current_recommendations(api_base, auth_token),
        make_get_low_stock_items(api_base, auth_token),
        make_get_top_sellers(api_base, auth_token),
    ]

    for tool in tools:
        tool.coroutine = guard_coroutine(tool.coroutine)

    return tools
