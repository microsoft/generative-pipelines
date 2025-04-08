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
    print("⚠️  The first deployment might take several minutes to complete (e.g. 30–40 minutes).")
    confirm = input("Do you want to continue? [y/N]: ").strip().lower()
    if confirm != 'y':
        print("❌ Deployment canceled.")
        sys.exit(0)

    root = Path(__file__).resolve().parent.parent
    infra_dir = root / "infra"
    apphost_dir = infra_dir / "dev-with-aspire"

    if not apphost_dir.exists():
        print(f"Error: '{apphost_dir}' does not exist.")
        sys.exit(1)

    print(f"Running 'azd provision' in {apphost_dir}")
    subprocess.run(["azd", "provision"], cwd=apphost_dir, check=True)


if __name__ == '__main__':
    main()
