#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet run --project src/SingleFlightCacheStampedeLab.Cli -c Release -- quick "$@"
