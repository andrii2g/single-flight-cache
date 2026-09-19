#!/usr/bin/env bash
set -euo pipefail

docker build --target tests -t singleflight-cache-stampede-lab-tests .
