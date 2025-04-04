#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$HERE"

# Creates/Updates src/swagger.json

npx tsoa routes
npx tsoa spec

if [ -d "dist" ]; then
    cp src/swagger.json dist/
fi
