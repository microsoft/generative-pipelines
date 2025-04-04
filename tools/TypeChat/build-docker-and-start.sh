#!/usr/bin/env bash

set -e

HERE="$(cd "$(dirname "${BASH_SOURCE[0]:-$0}")" && pwd)"
cd "$HERE"

docker build -t generative-pipelines/typechat .

docker run --rm --init -it -p 3000:3000 -e PORT=3000 generative-pipelines/typechat
