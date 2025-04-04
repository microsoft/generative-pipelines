// Set the correct Azure OpenAI "endpoint" and "deployment" name below
// Start the services locally using Aspire
// run: node quickstart

const AzureEmbeddingEndpoint = "https://contoso.cognitiveservices.azure.com/";
const AzureEmbeddingDeployment = "text-embedding-3-small";

const fs = require("fs");

const filePath = "example.docx";
const apiUrl = "http://localhost:60000/api/jobs";

const contentBase64 = fs.readFileSync(filePath, "base64");

const json_payload = {
  config: {
    embEndpoint:   AzureEmbeddingEndpoint,
    embDeployment: AzureEmbeddingDeployment,
    embCustomDims: true,
    chunkSize:     1000,
  },
  fileName: "example.docx",
  content:  contentBase64,

  _workflow: {
    steps: [
      { function: "extractor/extract" },
      {
        function: "chunker/chunk",
        id:       "chunking",
        xin:      "{ text: state.fullText, maxTokensPerChunk: start.config.chunkSize }",
        xout:     "{ chunks: state.chunks }",
      },
      {
        function: "embedding-generator/vectorize-custom",
        xin: `{
                provider:  'azureai',
                endpoint:  start.config.embEndpoint,
                modelId:   start.config.embDeployment,
                inputs:    chunking.out.chunks,
                dimensions: \`5\`,
                supportsCustomDimensions: start.config.embCustomDims 
             }`,
      },
    ],
  },
};

fetch(apiUrl, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify(json_payload),
})
  .then((res) => res.json())
  .then((data) => console.log(JSON.stringify(data, null, 4)))
  .catch((err) => console.error("Error:", err));
