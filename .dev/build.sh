#!/usr/bin/env bash

set -e

# Set working directory to the repo root.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && cd .. && pwd)"
cd "$ROOT"

TOOLS_DIR="tools"

# Iterate over directories in tools/ (ignoring those starting with "_")
for dir in "$TOOLS_DIR"/*/; do
  dir_name=$(basename "$dir")

  # Skip directories that start with "_"
  if [[ "$dir_name" == _* ]]; then
    continue
  fi

  # Check for a .csproj file (indicating a .NET project)
  if ls "$dir"/*.csproj &>/dev/null; then
    echo "----------------------------------------------------"
    echo "Building .NET project in $dir_name..."
    (cd "$dir" && dotnet build)
  fi

  # Check for a package.json file (indicating a Node.js project)
  if [[ -f "$dir/package.json" ]]; then
    echo "----------------------------------------------------"
    echo "Building Node.js project in $dir_name..."
    (cd "$dir" && pnpm install && pnpm build)
  fi

  # Check for a pyproject.toml file (indicating a Python project)
  if [[ -f "$dir/pyproject.toml" ]]; then
    echo "----------------------------------------------------"
    echo "Building Python project in $dir_name..."
    (cd "$dir" && poetry install)
  fi
done

echo "----------------------------------------------------"
echo "Building Orchestrator..."
cd "$ROOT/service/Orchestrator"
dotnet build

echo "----------------------------------------------------"
echo "Building .NET Aspire Host..."
cd "$ROOT/infra/Aspire.AppHost"
dotnet build
