FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build-env
WORKDIR /app

COPY . ./

RUN dotnet publish -c Release -o out \
    --runtime linux-musl-x64 \
    --self-contained true \
    -p:PublishTrimmed=true \
    -p:TrimMode=Link \
    -p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine

WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["./ServicebusCloner"]

HEALTHCHECK --interval=10s --timeout=3s --retries=60 --start-period=10s CMD test -f /health/ready || exit 1
