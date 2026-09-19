#!/usr/bin/env bash
set -euo pipefail

dotnet restore SingleFlightCacheStampedeLab.slnx
dotnet build SingleFlightCacheStampedeLab.slnx -c Release --no-restore
dotnet test SingleFlightCacheStampedeLab.slnx -c Release --no-build
dotnet format SingleFlightCacheStampedeLab.slnx --verify-no-changes --no-restore
