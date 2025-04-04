# Set the correct Azure OpenAI "endpoint" and "deployment" name below
# Start the services locally using Aspire
# run: python quickstart.py

AzureEmbeddingEndpoint = "https://contoso.cognitiveservices.azure.com/"
AzureEmbeddingDeployment = "text-embedding-3-small"

import json
import requests
import base64


with open("example.docx", "rb") as file:
    content_base64 = base64.b64encode(file.read()).decode("utf-8")

yaml_payload = f"""\
config:
  embEndpoint:   {AzureEmbeddingEndpoint}
  embDeployment: {AzureEmbeddingDeployment}
  embCustomDims: True
  chunkSize:     1000

fileName: example.docx
content: { content_base64 }

_workflow:
  steps:
    - function: extractor/extract
    - function: chunker/chunk
      id: chunking
      xin: >
        {{
            text:              state.fullText, 
            maxTokensPerChunk: start.config.chunkSize
        }}
      xout: >
        {{
            chunks: state.chunks
        }}
    - function: embedding-generator/vectorize-custom
      xin: >
        {{
            provider:  'azureai',
            endpoint:   start.config.embEndpoint,
            modelId:    start.config.embDeployment, 
            inputs:     chunking.out.chunks,
            dimensions: `5`,
            supportsCustomDimensions: contains(['true','True'], start.config.embCustomDims)
        }}
"""

headers = {"Content-Type": "application/x-yaml"}
response = requests.post("http://localhost:60000/api/jobs", headers=headers, data=yaml_payload)

response_json = response.json()
print(json.dumps(response_json, indent=4))
