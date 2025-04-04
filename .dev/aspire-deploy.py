# Copyright (c) Microsoft. All rights reserved.

import sys
import os
import subprocess
from pathlib import Path

# Check Python version
if sys.version_info < (3, 11):
    print("❌ Python 3.11+ is required", file=sys.stderr)
    sys.exit(1)


def main():
    root = Path(__file__).resolve().parent.parent
    infra_dir = root / "infra"
    apphost_dir = infra_dir / "Aspire.AppHost"

    if not apphost_dir.exists():
        print(f"Error: '{apphost_dir}' does not exist.")
        sys.exit(1)

    print(f"Running 'azd deploy' in {apphost_dir}")
    subprocess.run(["azd", "deploy"], cwd=apphost_dir, check=True)


if __name__ == '__main__':
    main()
