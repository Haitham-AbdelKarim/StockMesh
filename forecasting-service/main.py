"""FastAPI entrypoint: thin HTTP layer over forecasting.fit_forecast."""

from fastapi import FastAPI
from pydantic import BaseModel, Field

from forecasting import fit_forecast, has_enough_data

app = FastAPI(title="StockMesh Forecasting Service")


class SeriesPoint(BaseModel):
    date: str
    value: float


class ForecastRequest(BaseModel):
    series: list[SeriesPoint]
    periods_ahead: int = Field(gt=0, le=90)


class ForecastResponse(BaseModel):
    insufficient_data: bool
    dates: list[str] = []
    trend: list[float] = []
    yhat: list[float] = []
    yhat_lower: list[float] = []
    yhat_upper: list[float] = []


@app.get("/health")
def health() -> dict:
    return {"status": "ok"}


@app.post("/forecast", response_model=ForecastResponse)
def forecast(request: ForecastRequest) -> ForecastResponse:
    values = [point.value for point in request.series]

    if not has_enough_data(values):
        return ForecastResponse(insufficient_data=True)

    dates = [point.date for point in request.series]
    result = fit_forecast(dates, values, request.periods_ahead)

    return ForecastResponse(insufficient_data=False, **result)
