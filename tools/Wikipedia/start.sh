#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$HERE"

UVICORN_PORT=${UVICORN_PORT:-5021}

poetry install

poetry run uvicorn app.main:app --reload --port "$UVICORN_PORT"
