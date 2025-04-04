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

# Start all the projects
start:
    @if [ "${OS:-}" = "Windows_NT" ]; then \
        pwsh -File .dev/start.ps1; \
    else \
        bash .dev/start.sh; \
    fi

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

###################
#### Internals ####
###################

# First time Azure provisioning (using existing build)
_aspire-provision:
    @{{python}} .dev/aspire-provision.py

# Deploy to Azure (using existing build)
_aspire-deploy:
    @{{python}} .dev/aspire-deploy.py
