#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$HERE"

# Barrelsby is a tool for automatically generating TypeScript barrel files (index.ts)
# that export modules from a directory. This makes imports cleaner and easier to manage.

npx barrelsby --config barrelsby.json
