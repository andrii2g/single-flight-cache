#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet restore SingleFlightCacheStampedeLab.slnx
dotnet build SingleFlightCacheStampedeLab.slnx -c Release --no-restore
dotnet test --solution SingleFlightCacheStampedeLab.slnx -c Release --no-build --timeout 60s
dotnet format SingleFlightCacheStampedeLab.slnx --verify-no-changes --no-restore
