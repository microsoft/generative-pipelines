# Copyright (c) Microsoft. All rights reserved.

import sys
import requests

# Check Python version
if sys.version_info < (3, 11):
    print("❌ Python 3.11+ is required", file=sys.stderr)
    sys.exit(1)

#print(f"📂 Working directory: {os.getcwd()}")

url = 'http://localhost:60000/api/jobs'
headers = {'Content-Type': 'application/x-yaml'}
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
    print(response.text)

    if response.status_code >= 400:
        print("❌ Request failed. Check the endpoint, input format, or server status.")

except requests.exceptions.ConnectionError:
    print(f"❌ Failed to connect to the server. Is it running at {url}?")
except requests.exceptions.Timeout:
    print("❌ The request timed out. Try again later.")
except requests.exceptions.RequestException as e:
    print(f"❌ An error occurred while making the request: {e}")
