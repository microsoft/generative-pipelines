# Install Just to use the commands below.
#
# Run `just build` to build all projects.
# Run `just start` to launch all services.
# Run `just` to see all available commands.
#
# How to install Just:
#
#   cargo install just
#   brew install just
#   npm install -g rust-just
#   pipx install rust-just
#   choco install just
#   scoop install just
#   winget install --id Casey.Just --exact
#   apt install just
#   apk add just
#   snap install --edge --classic just
#   pkg install just

# Detect the python executable
python := if os_family() == "windows" { "python" } else { "python3" }

# Default command when no command is provided
_default:
    @just --list

# Build all the projects
build:
    @if [ "${OS:-}" = "Windows_NT" ]; then \
        pwsh -File .dev/build.ps1; \
    else \
        bash .dev/build.sh; \
    fi

# Start all the projects using Docker images
dev:
    @if [ "${OS:-}" = "Windows_NT" ]; then \
        pwsh -File .dev/start.ps1; \
    else \
        bash .dev/start.sh; \
    fi

# Start all the projects from source code using Aspire
start:
    @cd infra/dev-with-docker && docker compose up --pull always

# Remove temporary files, build artifacts, and caches
clean:
    @if [ "${OS:-}" = "Windows_NT" ]; then \
        pwsh -File .dev/clean.ps1; \
    else \
        bash .dev/clean.sh; \
    fi

# Test the orchestrator
test-orchestrator:
    @{{python}} .dev/test-orchestrator.py

# Initialize Azure settings
aspire-init:
    @{{python}} .dev/aspire-init.py

# Build all and deploy to Azure (aka first time provisioning)
aspire-provision: build _aspire-provision

# Build all and deploy to Azure (requires first time provisioning)
aspire-deploy: build _aspire-deploy

# Show Aspire manifest
aspire-manifest:
    @{{python}} .dev/aspire-manifest.py

# List files foreign to the repository, ignored by git
git-ignored:
    git ls-files --others --exclude-standard --ignored

# Check for typos
check-typos:
    @typos --config .github/_typos.toml

# Create dev image for the tools Orchestrator (tag: local)
dockerize-orchestrator:
    @docker buildx build --no-cache --progress tty --file service/Orchestrator/Dockerfile --tag gptools/orchestrator:local service

# Create dev image for the Chunker tool (tag: local)
dockerize-chunker:
    @docker buildx build --no-cache --progress tty --file tools/Chunker/Dockerfile --tag gptools/chunker:local tools

# Create dev image for the Embedding Generator tool (tag: local)
dockerize-embedding-generator:
    @docker buildx build --no-cache --progress tty --file tools/EmbeddingGenerator/Dockerfile --tag gptools/embedding-generator:local tools

# Create dev image for the Extractor tool (tag: local)
dockerize-extractor:
    @docker buildx build --no-cache --progress tty --file tools/Extractor/Dockerfile --tag gptools/extractor:local tools

# Create dev image for the Vector Storage SK tool (tag: local)
dockerize-vector-storage-sk:
    @docker buildx build --no-cache --progress tty --file tools/VectorStorageSk/Dockerfile --tag gptools/vector-storage-sk:local tools

# Create dev image for the TypeChat tool (tag: local)
dockerize-typechat:
    @docker buildx build --no-cache --progress tty --file tools/TypeChat/Dockerfile --tag gptools/typechat:local tools/TypeChat

# Create dev image for the Wikipedia tool (tag: local)
dockerize-wikipedia:
    @docker buildx build --no-cache --progress tty --file tools/Wikipedia/Dockerfile --tag gptools/wikipedia:local tools/Wikipedia

###################
#### Internals ####
###################

# First time Azure provisioning (using existing build)
_aspire-provision:
    @{{python}} .dev/aspire-provision.py

# Deploy to Azure (using existing build)
_aspire-deploy:
    @{{python}} .dev/aspire-deploy.py
