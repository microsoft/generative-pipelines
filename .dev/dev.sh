#!/usr/bin/env bash

set -e

MIN_NODE="18.12.0"

# Set working directory to the repo root.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && cd .. && pwd)"
cd "$ROOT"

ensure_node_version() {
  # Try switching to NodeJS v20 if nvm is already available
  if command -v nvm >/dev/null 2>&1; then
    nvm use 20 >/dev/null 2>&1 || true
  fi
  
  # Get Node version; default to 0.0.0 if node isn't installed
  NODE_RAW=$(node -v 2>/dev/null || echo "v0.0.0")
  NODE_VERSION="${NODE_RAW#v}"
  echo "Node version is $NODE_RAW (v$MIN_NODE or higher required)."
  
  # Compare versions using sort -V
  if [ "$(printf '%s\n' "$MIN_NODE" "$NODE_VERSION" | sort -V | head -n1)" != "$MIN_NODE" ]; then
  
    # Source nvm only if available
    if [ -s "$NVM_DIR/nvm.sh" ]; then
      . "$NVM_DIR/nvm.sh"
    elif [ -s "$HOME/.nvm/nvm.sh" ]; then
      export NVM_DIR="$HOME/.nvm"
      . "$NVM_DIR/nvm.sh"
    else
      echo "nvm not found. Please install Node.js v$MIN_NODE or higher manually."
      exit 1
    fi
  
    echo "Installing latest LTS Node.js version via nvm..."
    nvm install --lts
    nvm use --lts
  
    NODE_RAW=$(node -v)
    NODE_VERSION="${NODE_RAW#v}"
    if [ "$(printf '%s\n' "$MIN_NODE" "$NODE_VERSION" | sort -V | head -n1)" != "$MIN_NODE" ]; then
      echo "Failed to switch to Node.js v$MIN_NODE or higher via nvm. You have $NODE_RAW."
      exit 1
    fi
  fi
}

ensure_node_version

cd infra/dev-with-aspire

export DOTNET_USE_POLLING_FILE_WATCHER=1
dotnet watch
