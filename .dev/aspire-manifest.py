# Copyright (c) Microsoft. All rights reserved.

import sys
import subprocess
from pathlib import Path

# Check Python version
if sys.version_info < (3, 11):
    print("❌ Python 3.11+ is required", file=sys.stderr)
    sys.exit(1)

# Run: cd infra/Aspire.AppHost/ && dotnet run --project Aspire.AppHost.csproj -- --publisher manifest --output-path aspire-manifest.json
def main():
    root = Path(__file__).resolve().parent.parent
    infra_dir = root / "infra"
    apphost_dir = infra_dir / "Aspire.AppHost"

    if not apphost_dir.exists():
        print(f"Error: '{apphost_dir}' does not exist.")
        sys.exit(1)

    output_path = apphost_dir / "aspire-manifest.json"
    print("Running Aspire manifest generation...")
    subprocess.run([
        "dotnet", "run",
        "--project", "Aspire.AppHost.csproj",
        "--", "--publisher", "manifest",
        "--output-path", str(output_path)
    ], cwd=apphost_dir, check=True)

    if output_path.exists():
        print(f"\n✅ Manifest generated at: {output_path}")
    else:
        print(f"\n❌ Failed to generate manifest at: {output_path}")
        sys.exit(1)


if __name__ == "__main__":
    main()
