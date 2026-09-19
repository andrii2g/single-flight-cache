# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props SingleFlightCacheStampedeLab.slnx ./
COPY src/SingleFlightCacheStampedeLab.Core/*.csproj src/SingleFlightCacheStampedeLab.Core/
COPY src/SingleFlightCacheStampedeLab.Cli/*.csproj src/SingleFlightCacheStampedeLab.Cli/
COPY tests/SingleFlightCacheStampedeLab.Tests/*.csproj tests/SingleFlightCacheStampedeLab.Tests/
RUN dotnet restore SingleFlightCacheStampedeLab.slnx

COPY . .
RUN dotnet build SingleFlightCacheStampedeLab.slnx -c Release --no-restore

FROM build AS tests
RUN dotnet test SingleFlightCacheStampedeLab.slnx -c Release --no-build

FROM build AS publish
RUN dotnet publish src/SingleFlightCacheStampedeLab.Cli/SingleFlightCacheStampedeLab.Cli.csproj \
    -c Release \
    --no-build \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0-noble AS runtime
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SingleFlightCacheStampedeLab.Cli.dll"]
CMD ["quick"]
