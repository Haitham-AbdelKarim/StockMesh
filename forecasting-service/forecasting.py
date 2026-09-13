"""Generic Prophet time-series forecasting: series in, forecast out.

No knowledge of sales, stores, or market signals — the same endpoint serves
both the per-product demand series and the vertical market series (Phase 13).

Prophet is imported lazily inside fit_forecast so the minimum-data gating
stays testable on machines without prophet installed.
"""
import pandas as pd
from prophet import Prophet


MIN_NONZERO_POINTS = 14


def count_nonzero(values: list[float]) -> int:
    """Number of data points carrying a signal (zeros are sparsity, not demand)."""
    return sum(1 for value in values if value != 0)


def has_enough_data(values: list[float]) -> bool:
    """Minimum-data gate (§9.2): at least 14 days with a non-zero data point."""
    return count_nonzero(values) >= MIN_NONZERO_POINTS


def fit_forecast(dates: list[str], values: list[float], periods_ahead: int) -> dict:
    """Fit Prophet (MAP estimate: fast + deterministic) and predict history + horizon."""

    frame = pd.DataFrame({"ds": pd.to_datetime(dates), "y": values})

    model = Prophet(
        interval_width=0.8,
        mcmc_samples=0,
    )
    model.fit(frame)

    future = model.make_future_dataframe(periods=periods_ahead, freq="D")
    forecast = model.predict(future)

    return {
        "dates": forecast["ds"].dt.strftime("%Y-%m-%d").tolist(),
        "trend": forecast["trend"].tolist(),
        "yhat": forecast["yhat"].tolist(),
        "yhat_lower": forecast["yhat_lower"].tolist(),
        "yhat_upper": forecast["yhat_upper"].tolist(),
    }
