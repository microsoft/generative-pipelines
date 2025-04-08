# Usage: docker build --platform linux/amd64 --build-arg BUILD_IMAGE_TAG=9.0-noble-amd64 \
#                     --build-arg RUN_IMAGE_TAG=9.0-alpine-amd64 .
#
# Usage: docker build --platform linux/arm64 --build-arg BUILD_IMAGE_TAG=9.0-noble-arm64v8 \
#                     --build-arg RUN_IMAGE_TAG=9.0-alpine-arm64v8 .

# See https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md#full-tag-listing
ARG BUILD_IMAGE_TAG="9.0-noble"
ARG RUN_IMAGE_TAG="9.0-alpine"

#########################################################################
# .NET build
#########################################################################

FROM mcr.microsoft.com/dotnet/sdk:$BUILD_IMAGE_TAG AS build

ARG BUILD_CONFIGURATION=Release

# IMPORTANT: set context to /tools
COPY VectorStorageSk /src/VectorStorageSk
COPY _libs /src/_libs
WORKDIR "/src/VectorStorageSk"

RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build /p:RepoRoot=/src/
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:RepoRoot=/src/

#########################################################################
# prepare final content
#########################################################################

FROM mcr.microsoft.com/dotnet/aspnet:$RUN_IMAGE_TAG AS base

# Non-root user that will run the service
ARG USER=gp

WORKDIR /app

RUN \
    # Create user
    adduser --disabled-password --home /app --shell /bin/sh $USER && \
    # Allow user to access the build
    chown -R $USER:$USER /app

COPY --from=build --chown=gp:gp --chmod=0550 /app/publish .

#########################################################################
# runtime
#########################################################################

LABEL org.opencontainers.image.authors="Devis Lucato, https://github.com/dluc"

# Define current user
USER $USER

# See launchSettings.json
ENV ASPNETCORE_URLS=http://+:21365
ENV ASPNETCORE_HTTP_PORTS=21365

EXPOSE 21365

# Define executable
ENTRYPOINT ["dotnet", "VectorStorageSk.dll"]
