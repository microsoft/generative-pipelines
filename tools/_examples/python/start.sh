#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$HERE"

PORT=${PORT:-8000}

poetry install

#cd app
#poetry run uvicorn main:app --reload --port "$PORT"

poetry run uvicorn app.main:app --reload --port "$PORT"
