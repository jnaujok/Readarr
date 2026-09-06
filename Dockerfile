# syntax=docker/dockerfile:1.6
# Multi-stage image for Readarr (net10.0, linux-x64, framework-dependent).
# Build: docker build --build-arg READARRVERSION=0.4.19.0 --build-arg BRANCH=develop -t readarr:local .
# Run:   docker run --rm -p 8787:8787 -v readarr-config:/config readarr:local

ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG NODE_IMAGE=node:20.11.1-bookworm

# -----------------------------------------------------------------------------
# Frontend (webpack production UI)
# -----------------------------------------------------------------------------
FROM ${NODE_IMAGE} AS frontend
WORKDIR /src

COPY package.json yarn.lock ./
RUN --mount=type=cache,target=/root/.cache/yarn \
    yarn install --frozen-lockfile --network-timeout 120000

COPY tsconfig.json ./
COPY frontend ./frontend
RUN yarn run build --env production

# -----------------------------------------------------------------------------
# Backend publish + linux-x64 package
# -----------------------------------------------------------------------------
FROM ${SDK_IMAGE} AS backend

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_NOLOGO=true \
    NUGET_XMLDOC_MODE=skip \
    SentryUploadSymbols=false

ARG READARRVERSION=0.0.0.0
ARG BRANCH=develop
ENV READARRVERSION=${READARRVERSION} \
    BUILD_SOURCEBRANCHNAME=${BRANCH}

WORKDIR /src
COPY build.sh LICENSE.md global.json ./
COPY src ./src
RUN --mount=type=cache,target=/root/.nuget/packages \
    chmod +x build.sh \
    && ./build.sh --backend -r linux-x64 -f net10.0

COPY --from=frontend /src/_output/UI /src/_output/UI
RUN ./build.sh --packages -r linux-x64 -f net10.0

# -----------------------------------------------------------------------------
# Runtime
# -----------------------------------------------------------------------------
FROM ${RUNTIME_IMAGE} AS runtime

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    XDG_CONFIG_HOME=/config \
    TZ=Etc/UTC

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl tzdata ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --gid 1000 readarr \
    && useradd --uid 1000 --gid 1000 --create-home --home-dir /config readarr \
    && mkdir -p /app /books /downloads \
    && chown -R readarr:readarr /app /config /books /downloads

WORKDIR /app
COPY --from=backend --chown=readarr:readarr /src/_artifacts/linux-x64/net10.0/Readarr/ ./
RUN find /app -type f \( -name Readarr -o -name Readarr.Update \) -exec chmod 755 {} \;

USER readarr
EXPOSE 8787
VOLUME ["/config"]

HEALTHCHECK --interval=30s --timeout=5s --start-period=45s --retries=5 \
    CMD curl -fsS http://127.0.0.1:8787/ping || exit 1

ENTRYPOINT ["./Readarr"]
CMD ["-nobrowser", "-data=/config"]
