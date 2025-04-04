#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$HERE"

# Transpile TypeScript & Generate Swagger
pnpm run build

pnpm run start
