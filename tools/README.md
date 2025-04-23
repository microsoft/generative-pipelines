# Built-in tools

## Chunking

Split text into structured chunks

## EmbeddingGenerator

Generate embeddings for a list of strings.

## TextGenerator

Generate text using LLMs.

## Extractor

Extract text from PDF, Word, Excel, PowerPoint, Image files

## TypeChat

Use TypeChat library.

## Wikipedia

Fetch content from Wikipedia.

# Creating tools and functions

## C#

1. `dotnet new create webapi --name MyTool`
2. Expose a `POST /` endpoint that accepts a JSON payload and returns a JSON response.
   This will be the main function in the tool, when no function name is specified.
3. Additional functions can be added by adding new endpoints.

**Optional:**

- Add `Scalar.AspNetCore` and/or `Swashbuckle.AspNetCore.SwaggerUI` packages and configure Swagger.
  See `_dotnetExample` for an example.
- Change the ports in `Properties/launchSettings.json` in case of conflicts.

## Node.js

1. clone the `_examples/nodejs` folder and rename it to `MyTool`
2. Expose a `POST /` endpoint that accepts a JSON payload and returns a JSON response.
   This will be the main function in the tool, when no function name is specified.
3. Additional functions can be added by adding new endpoints.

### Python tool

1. `cd tools`
2. clone the `_examples/python` folder and rename it to `MyTool`
3. Expose a `POST /` endpoint that accepts a JSON payload and returns a JSON response.
   This will be the main function in the tool, when no function name is specified.
4. Additional functions can be added by adding new endpoints.

Notes:

- The project should use `poetry` for package management.
- The project should include a `Dockerfile` for cloud deployments.
