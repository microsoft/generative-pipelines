#!/usr/bin/env bash

#	-e: exit immediately if any command returns a non-zero exit code.
#	-u: treat unset variables as errors and exit.
#	-o pipefail: a pipeline returns a failure if any command (not just the last one) fails.
set -euo pipefail

# Default options
DRY_RUN=false
VERBOSE=false

# Color codes for logging
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Set working directory to the repo root.
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && cd .. && pwd)"
cd "$ROOT"
echo -e "${GREEN}Starting cleanup in:${NC} $ROOT"

usage() {
  cat <<EOF
Usage: $0 [OPTIONS]

Options:
  -n, --dry-run    Show what would be deleted without removing anything.
  -v, --verbose    Enable verbose output.
  -h, --help       Show this help message.
EOF
  exit 1
}

# Parse command-line options
while [[ $# -gt 0 ]]; do
  case "$1" in
    -n|--dry-run)
      DRY_RUN=true
      shift
      ;;
    -v|--verbose)
      VERBOSE=true
      shift
      ;;
    -h|--help)
      usage
      ;;
    *)
      echo -e "${RED}Unknown option:$NC $1"
      usage
      ;;
  esac
done

# Logging helpers
log_info() {
  if $VERBOSE; then
    echo -e "${YELLOW}[INFO]${NC} $*"
  fi
}

log_action() {
  echo -e "${GREEN}[ACTION]${NC} $*"
}

# Safe removal wrapper
safe_rm() {
  if $DRY_RUN; then
    echo -e "${YELLOW}[Dry-run]${NC} Would remove: $*"
  else
    rm -rf "$@"
  fi
}

##############################
# .NET cleanup: Remove bin and obj folders from projects
##############################
cleanup_dotnet_projects() {
  log_info "Cleaning .NET projects..."
  find . -type f -name '*.csproj' -print0 | while IFS= read -r -d '' csproj; do
    project_dir="$(dirname "$csproj")"
    local dirs_to_remove=("bin" "obj" "TestResults" "Publish" "publish" "artifacts" "packages")
    for sub in "${dirs_to_remove[@]}"; do
      target="$project_dir/$sub"
      if [ -d "$target" ]; then
        log_action "Deleting .NET folder: $target"
        safe_rm "$target"
      fi
    done
  done

  log_info "Cleaning .nupkg files..."
  find . -type f -name '*.nupkg' -print0 | while IFS= read -r -d '' pkg; do
    log_action "Deleting package file: $pkg"
    safe_rm "$pkg"
  done
}

##############################
# Node.js projects cleanup: Remove build artifacts and dependencies
##############################
cleanup_nodejs_projects() {
  log_info "Cleaning Node.js projects..."
  
  # Identify Node.js/ypeScript projects by presence of package.json (optionally check for tsconfig.json)
  find . -type f -name "package.json" -print0 | while IFS= read -r -d '' pkg; do
    project_dir="$(dirname "$pkg")"
    
    # Remove common artifact directories
    for dir in node_modules dist build coverage out .turbo .next .parcel-cache .vite; do
      TARGET="$project_dir/$dir"
      if [ -d "$TARGET" ]; then
        log_action "Deleting artifact directory: $TARGET"
        safe_rm "$TARGET"
      fi
    done
    
    # Remove TypeScript-specific build info files
    find "$project_dir" -maxdepth 1 -type f -name "*.tsbuildinfo" -print0 2>/dev/null || true | while IFS= read -r -d '' info; do
      log_action "Deleting TypeScript build info file: $info"
      safe_rm "$info"
    done
  done
}

##############################
# Python projects cleanup: Remove caches, temporary files, and virtual environments
##############################
cleanup_python_projects() {
  log_info "Cleaning Python projects..."
  # Identify Python projects by common markers.
  find . -type f \( -name "requirements.txt" -o -name "setup.py" -o -name "pyproject.toml" \) -print0 | while IFS= read -r -d '' marker; do
    project_dir="$(dirname "$marker")"
    
    # Remove cache directories
    for d in __pycache__ .pytest_cache .mypy_cache; do
      if [ -d "$project_dir/$d" ]; then
        log_action "Deleting Python cache directory: $project_dir/$d"
        safe_rm "$project_dir/$d"
      fi
    done

    # Remove build artifact directories
    for artifact in build dist htmlcov .eggs; do
      if [ -d "$project_dir/$artifact" ]; then
        log_action "Deleting Python build artifact directory: $project_dir/$artifact"
        safe_rm "$project_dir/$artifact"
      fi
    done

    # Remove egg-info directories (glob pattern)
    find "$project_dir" -maxdepth 1 -type d -name "*.egg-info" -print0 | while IFS= read -r -d '' egg; do
      log_action "Deleting Python egg-info directory: $egg"
      safe_rm "$egg"
    done

    # Remove temporary Python files (*.pyc, *.pyo)
    find "$project_dir" -maxdepth 1 -type f \( -name '*.pyc' -o -name '*.pyo' \) -print0 | while IFS= read -r -d '' file; do
      log_action "Deleting Python file: $file"
      safe_rm "$file"
    done

    # Remove virtual environments if they exist
    for env in venv .venv; do
      if [ -d "$project_dir/$env" ]; then
        log_action "Deleting Python virtual environment: $project_dir/$env"
        safe_rm "$project_dir/$env"
      fi
    done
  done
}

##############################
# Java cleanup: Remove build artifacts (target directories)
##############################
cleanup_java_projects() {
  log_info "Cleaning Java projects..."
  # Look for pom.xml or build.gradle to identify Java projects.
  find . -type f \( -name "pom.xml" -o -name "build.gradle" \) -print0 | while IFS= read -r -d '' marker; do
    project_dir="$(dirname "$marker")"
    target_dir="$project_dir/target"
    if [ -d "$target_dir" ]; then
      log_action "Deleting Java target directory: $target_dir"
      safe_rm "$target_dir"
    fi
  done
}

##############################
# Rust cleanup: Remove Cargo target directories
##############################
cleanup_rust_projects() {
  log_info "Cleaning Rust projects..."
  # Identify Rust projects by Cargo.toml
  find . -type f -name "Cargo.toml" -print0 | while IFS= read -r -d '' cargo_file; do
    project_dir="$(dirname "$cargo_file")"
    target_dir="$project_dir/target"
    if [ -d "$target_dir" ]; then
      log_action "Deleting Rust target directory: $target_dir"
      safe_rm "$target_dir"
    fi
  done
}

##############################
# PHP cleanup: Remove common cache directories in PHP projects
##############################
cleanup_php_projects() {
  log_info "Cleaning PHP projects..."
  # Identify PHP projects by composer.json
  find . -type f -name "composer.json" -print0 | while IFS= read -r -d '' composer_file; do
    project_dir="$(dirname "$composer_file")"
    # Common PHP cache directories (e.g., Symfony, Laravel)
    php_cache_dirs=("bootstrap/cache" "storage/framework/cache" "cache")
    for sub in "${php_cache_dirs[@]}"; do
      TARGET="$project_dir/$sub"
      if [ -d "$TARGET" ]; then
        log_action "Deleting PHP cache directory: $TARGET"
        safe_rm "$TARGET"
      fi
    done
  done
}

##############################
# Go projects cleanup: Remove build artifacts for Go projects
##############################
cleanup_go_projects() {
  log_info "Cleaning Go projects..."
  # Identify Go projects by looking for a go.mod file.
  find . -type f -name "go.mod" -print0 | while IFS= read -r -d '' gomod; do
    project_dir="$(dirname "$gomod")"
    # Remove 'bin' directory if it exists (common place for built binaries)
    if [ -d "$project_dir/bin" ]; then
      log_action "Removing Go build directory: $project_dir/bin"
      safe_rm "$project_dir/bin"
    fi
    # Remove 'pkg' directory if it exists (optional, for build artifacts)
    if [ -d "$project_dir/pkg" ]; then
      log_action "Removing Go pkg directory: $project_dir/pkg"
      safe_rm "$project_dir/pkg"
    fi
  done
}

##############################
# Ruby projects cleanup: Remove temporary directories for Ruby projects
##############################
cleanup_ruby_projects() {
  log_info "Cleaning Ruby projects..."
  # Identify Ruby projects by the presence of a Gemfile.
  find . -type f -name "Gemfile" -print0 | while IFS= read -r -d '' gemfile; do
    project_dir="$(dirname "$gemfile")"
    # Remove common Ruby temporary directories: tmp and log
    for sub in tmp log; do
      TARGET="$project_dir/$sub"
      if [ -d "$TARGET" ]; then
        log_action "Deleting Ruby directory: $TARGET"
        safe_rm "$TARGET"
      fi
    done
    # Optionally remove vendor/bundle (if it's generated and not checked into version control)
    if [ -d "$project_dir/vendor/bundle" ]; then
      log_action "Deleting Ruby vendor bundle directory: $project_dir/vendor/bundle"
      safe_rm "$project_dir/vendor/bundle"
    fi
  done
}

##############################
# Generic temporary directories cleanup (for any projects)
##############################
cleanup_temp_dirs() {
  local temp_dirs=("node_modules" "dist" "build" "__pycache__")
  log_info "Cleaning generic temporary directories: ${temp_dirs[*]}"
  local find_args=()
  for dir in "${temp_dirs[@]}"; do
    find_args+=( -name "$dir" -o )
  done
  unset 'find_args[${#find_args[@]}-1]'
  find . -type d \( "${find_args[@]}" \) -prune -print0 | while IFS= read -r -d '' tmpdir; do
    log_action "Deleting temporary directory: $tmpdir"
    safe_rm "$tmpdir"
  done
}

##############################
# Generic temporary files cleanup (Python .pyc and .pyo)
##############################
cleanup_temp_files() {
  local temp_files=("*.pyc" "*.pyo")
  log_info "Cleaning generic temporary files: ${temp_files[*]}"
  for pattern in "${temp_files[@]}"; do
    find . -type f -name "$pattern" -print0 | while IFS= read -r -d '' tmpfile; do
      log_action "Deleting temporary file: $tmpfile"
      safe_rm "$tmpfile"
    done
  done
}

# Execute cleanup functions
cleanup_dotnet_projects
cleanup_nodejs_projects
cleanup_python_projects
cleanup_java_projects
cleanup_rust_projects
cleanup_php_projects
cleanup_go_projects
cleanup_ruby_projects
cleanup_temp_dirs
cleanup_temp_files

echo -e "${GREEN}Cleanup done.${NC}"
