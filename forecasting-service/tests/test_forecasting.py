"""Phase 11 tests: trend direction on a synthetic series + minimum-data gating."""

from datetime import date, timedelta

import pytest

from forecasting import fit_forecast, has_enough_data


def make_dates(n: int) -> list[str]:
    start = date(2026, 1, 1)
    return [(start + timedelta(days=i)).isoformat() for i in range(n)]


def test_upward_trend_produces_upward_yhat():
    pytest.importorskip("prophet")

    dates = make_dates(40)
    values = [float(i + 1) for i in range(40)]

    result = fit_forecast(dates, values, periods_ahead=7)

    assert len(result["yhat"]) == 40 + 7
    assert len(result["dates"]) == 40 + 7
    assert len(result["trend"]) == 40 + 7
    assert len(result["yhat_lower"]) == 40 + 7
    assert len(result["yhat_upper"]) == 40 + 7
    assert result["yhat"][-1] > result["yhat"][0]


def test_sparse_series_fails_minimum_data_gate():
    assert has_enough_data([0.0] * 30 + [5.0] * 5) is False
    assert has_enough_data([0.0] * 40) is False
    assert has_enough_data([2.0] * 14) is True
    assert has_enough_data([float(i % 3) for i in range(60)]) is True
