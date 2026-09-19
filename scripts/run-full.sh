#!/usr/bin/env bash
set -euo pipefail

dotnet run --project src/SingleFlightCacheStampedeLab.Cli -c Release -- sweep --profile full "$@"
