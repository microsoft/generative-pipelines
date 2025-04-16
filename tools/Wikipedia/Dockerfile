FROM python:3.12-slim

WORKDIR /app

ENV PYTHONUNBUFFERED=1 \
    POETRY_VERSION=1.8.3 \
    POETRY_HOME="/opt/poetry" \
    PATH="/opt/poetry/bin:$PATH"

RUN apt-get update && apt-get install -y curl \
    && curl -sSL https://install.python-poetry.org | python - \
    && poetry config virtualenvs.create false

COPY pyproject.toml poetry.lock ./

RUN poetry install --no-root --no-interaction --no-ansi

# Copy app into /app/app so that "app" is a proper Python package root
COPY ./app /app/app

# Keep working dir as /app
WORKDIR /app

CMD ["sh", "-c", "uvicorn app.main:app --host 0.0.0.0 --port ${UVICORN_PORT:-6001}"]