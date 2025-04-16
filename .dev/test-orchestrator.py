# Copyright (c) Microsoft. All rights reserved.

import sys
import requests
import json
import os

# Check Python version
if sys.version_info < (3, 11):
    print("❌ Python 3.11+ is required", file=sys.stderr)
    sys.exit(1)

#print(f"📂 Working directory: {os.getcwd()}")


# To override the defaults, run:
#
#     ENDPOINT=http://localhost:60000 KEY="..." python .dev/test-orchestrator.py
#
#     ENDPOINT=https://orchestrator.....azurecontainerapps.io KEY="..." python .dev/test-orchestrator.py

url = os.getenv("ENDPOINT", "http://localhost:60000").rstrip("/") + "/api/jobs"
accessKey = os.getenv("KEY", "")

headers = {
    'Content-Type': 'application/x-yaml',
    'Authorization': accessKey
}
pipeline = """
title: Trope
  
_workflow:
    steps:
    - function: wikipedia/en
"""

print("=== PIPELNE ===")
print(pipeline)

print("=== EXECUTION ===")
try:
    response = requests.post(url, pipeline, headers=headers)
    print("\n=== HTTP STATUS ===")
    print(f"Status code: {response.status_code}\n")

    print("=== RESULT ===")
    try:
        parsed = json.loads(response.text)
        print(json.dumps(parsed, indent=4))
    except Exception:
        print(response.text)

    if response.status_code >= 400:
        print("❌ Request failed. Check the endpoint, input format, or server status.")

except requests.exceptions.ConnectionError:
    print(f"❌ Failed to connect to the server. Is it running at {url}?")
except requests.exceptions.Timeout:
    print("❌ The request timed out. Try again later.")
except requests.exceptions.RequestException as e:
    print(f"❌ An error occurred while making the request: {e}")
