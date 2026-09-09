FROM python:3.12-slim

# Pre-downloaded at build time so the first request never pays the CmdStan cost.
ENV CMDSTANPFX=/root/.cmdstan
ENV PROPHET_REPACKAGE_CMDSTAN=false

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt \
    && python -c "import prophet; print('prophet', prophet.__version__)"

COPY main.py forecasting.py ./

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
