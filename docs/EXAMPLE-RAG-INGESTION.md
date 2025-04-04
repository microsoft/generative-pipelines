# Example: RAG ingestion

- [Input parameters](#input)
- [Full pipeline YAML](#complete-pipeline-definition)
- [Python and C# clients](#python-and-c-clients)

Before approaching this example, please read the [Pipelines Introduction](PIPELINES-INTRO.md) doc
to understand the concepts of pipelines, functions, and transformations.

This example demonstrates how to build a real-world pipeline using the orchestrator to
ingest data from a web page, making the data available for RAG (retrieval-augmented generation).

Let's first identify the steps we need to take:

- Download the web page
- Extract the text from the web page
- Split the text into chunks that are small enough for the LLM to process and
  generate embedding vectors.
- For each chunk, generate an embedding vector, using an embedding model, e.g. from OpenAI.
- Ensure there is a collection in the vector database to store the vectors.
- Store details, chunks of text, and corresponding vectors in the vector database.
- For the purpose of this demo, show a list of the records created.

Here are the tools and functions we will use:

- `wikipedia/en` (aka `en` function from the `wikipedia` tool) to download the web
  page and extract the text. The tool is a wrapper around the Wikipedia API. It provides
  also other functions such as `wikipedia/it` to download the page in Italian,
  `wikipedia/fr` to download the page in French, etc. All these functions expect
  a `title` parameter, which is the title of the Wikipedia page to download.
- `chunker/chunk` (aka `chunk` function from the `chunker` tool) to split the text into
  chunks. The function expects a `text` parameter, which is the text to split, a
  `maxTokensPerChunk` parameter, which is the maximum number of tokens per chunk, and
  other optional parameters we won't use here.
- `embedding-generator/vectorize` (aka `vectorize` function from the
  `embedding-generator` tool) to generate the embedding vectors. The function expects
  an `inputs` parameter, which is the list of strings to vectorize, a `modelId` parameter,
  which is the name of the model to use, and `dimensions` parameter, which is the
  number of dimensions of the vectors to generate.
- `vector-storage-sk/create-collection` to create a collection in the vector database.
- `vector-storage-sk/upsert` to store the vectors in the vector database. See the pipeline
  below for the details of the parameters.
- `vector-storage-sk/search` to show the list of records created.

Some more details about the implementation:
- We're using OpenAI, so we need to configure the tool with the OpenAI API key. We
  could pass the key in the workflow, that that would be a security risk. The key can
  be stored in
  [EmbeddingGenerator/appsettings.Development.json](../tools/EmbeddingGenerator/appsettings.Development.json)
  ```json
    {
      "App": {
        "OpenAI": {
          "ApiKey": "sk-..."
        }
      }
    }
  ```
  or in a env variable called `App__OpenAI__ApiKey` in the EmbeddingGenerator tool. You
  can also use Azure OpenAI with Entra, see [EmbeddingGenerator/appsettings.json](../tools/EmbeddingGenerator/appsettings.json) for details.
- Records will be stored in Postgres. When the orchestrator is started with Aspire, it
  already includes a Postgres instance with pgvector extension. Qdrant is also available,
  and Azure AI Search if a connection string is configured.
  See the [VectorStorageSk](../tools/VectorStorageSk) tool for details.
- Records will be stored in a table with a pre-defined schema. Custom schemas can be
  created editing the VectorStorageSk tool. In future schemas will be created on the fly,
  without the need to create or edit any file. The existing schema is quite flexible,
  supporting tags, references, and other metadata.
  See [MemoryRecord.cs](../tools/VectorStorageSk/DataTypes/MemoryRecord.cs) for details.
- Chunks size will be measured using the `cl100k_base` tokenizer, provided by the
  `embedding-generator` tool. The tool includes a few other tokenizers if needed.
- Each record will have a common `sourceId` value, used to identify which records are
  related to the same source, ie the web page used. This is useful when reimporting the
  same page, as we can find which records need to be replaced. 
- We'll add some tags, just for fun. Tags are used to filter the records when
  searching. For example, we could use tags to filter the records by language, or by
  topic.

## Input

```yaml
input:
    # Unique id to correlate records, used for updates/deletions
    sourceId:      4b4a0a89897c480f8eb02bfc9160e677
    # Title of the Wikipedia page to download
    page:          Tristan_da_Cunha
    # URL, used only for reference in the storage
    url:           https://en.wikipedia.org/wiki/Tristan_da_Cunha
    # Custom tags
    tags:          ['user=me','type=wiki','lang=en']
    # Size of the text chunks used to generate the embedding vectors
    chunkSize:     500
    # Optional number of tokens shared by consecutive chunks
    chunkOverlap:  0
    # Name of the tokenizer used to measure the size of the text chunks
    tokenizer:     cl100k_base
    # OpenAI model used to generate embedding vectors
    vectorModel:   openai-text-embedding-3-small
    # Size of the embedding vectors (supported only by Matryoshka models)
    vectorSize:    1536
    # Storage used (supported: azureaisearch, postgres, qdrant)
    vectorStorage: postgres
    # Name of the collection/table used to store the vectors, chunks and metadata
    collection:    wikipedia
    # Name of the schema used to create the collection/table (this is the default available out of the box)
    dataType: MemoryRecord
    # Details about the collection schema.
    # This is here to support future scenario where the schema will be defined on the fly
    # without the need to create a dataType class file.
    fields:
     - name: Id
       type: id
     - name: Content
       type: text
     - name: ContentEmbedding
       type: vector
       # Note: this is the same value from above
       vectorSize: 1536
       vectorDistance: CosineSimilarity
     - name: Tags
       type: listOfText
     - name: SourceId
       type: text
     - name: IsTest
       type: bool
     - name: Title
       type: text
     - name: Reference
       type: text
     - name: TimeStamp
       type: int
     - name: Other
       type: text
```

## Fetch web page

Here's the step to invoke the Wikipedia function, passing in the title of the page to download.
The JMESPath expression is used to prepare the input, using the `page` parameter from the input.

```yaml
- function: wikipedia/en
  # In order to reference the output parameter later, we assign an id to the step.
  # This allows to reference input & output using "wikipedia.in.*" and "wikipedia.out.*"
  id: wikipedia
  xin: >
    { 
      title: start.input.page 
    }
```

The function returns a JSON object with the following structure:

```json
{
  "title": "...",
  "content": "..."
}
```

when processing the rest of the pipeline, this data is available in the context with the following keys:

- `state.title`: available only to the next function
- `state.content`: available only to the next function
- `wikipedia.out.title`: available to all steps that follow
- `wikipedia.out.content`: available to all steps that follow

Note that `start.input.page` will differ from `wikipedia.out.title` if the title include spaces or other
symbols, e.g. `Tristan_da_Cunha` vs `Tristan da Cunha` in this case.

## Chunk the content

This step is pretty straightforward, we just need to pass the content to the chunker function,
which expects four parameters.

```yaml
- function: chunker/chunk
  id: chunking
  xin: >
    {
      text:              state.content,
      maxTokensPerChunk: start.input.chunkSize,
      overlap:           start.input.chunkOverlap,
      tokenizer:         start.input.tokenizer
    }
```

The function returns an array of strings with the following structure:

```json
{
  "chunks": [ "...", "...", "...", ... ]
}
```

when processing the rest of the pipeline, this data is available in the context with the following keys:

- `state.chunks`: available only to the next function
- `chunking.out.chunks`: available to all steps that follow

## Calculate embeddings using OpenAI LLM

```yaml
- function: embedding-generator/vectorize
  id: vectors
  xin: >
    {
      inputs:     chunking.out.chunks,
      modelId:    start.input.vectorModel,
      dimensions: start.input.vectorSize
    }
```

The function returns a detail about the tokens spent, and an array of vectors, one vector
of 1536 floating number for each chunk of text, using the following structure:

```json
{
  "promptTokens": 7951,
  "totalTokens": 7951,
  "embeddings": [
    [ -0.0024764163, -0.009698434, ... ],
    [ -0.0076416324, -0.006432491, ... ],
    ...
  ]
}
```

## Create storage collection

```yaml    
- function: vector-storage-sk/create-collection
  xin: >
    {
      storageType: start.input.vectorStorage,
      collection:  start.input.collection, 
      fields:      start.input.fields
    }
```

## Prepare data for storage

This step is a bit more complex, as we need to prepare the data to be stored in the
storage, combining each chunk with the corresponding vector, and adding some metadata.

The orchestrator allows to have steps without a function, doing only JMESPath transformations,
which can be useful to debug the pipeline.

Here's the JMESPath expression used to prepare data for the storage function:

```yaml
- id: combine
  xin: >
    {
      values: map(&{
        Content:          @[0],
        ContentEmbedding: @[1],
        SourceId:         $.start.input.sourceId,
        Tags:             $.start.input.tags,
        IsTest:           'True',
        Title:            $.wikipedia.out.title,
        Reference:        $.start.input.url
      }, zip(chunking.out.chunks, vectors.out.embeddings))
    }
```
The expression uses the `zip` function to combine the two arrays `chunks` and `embeddings`
generated by two separate steps, `chunking` and `vectors` respectively.

Then, the `map` function is used to iterate over the zipped array, and for each element
it creates a new object with the following properties:
- `Content`: the chunk of text, which is the first element of the zipped array (index 0)
- `ContentEmbedding`: the vector, which is the second element of the zipped array (index 1)
- `SourceId`: the source ID, which is passed in the input
- `Tags`: the tags, which are passed in the input
- `IsTest`: a boolean value, set to `True`
- `Title`: the title of the page returned by the Wikipedia function
- `Reference`: the URL of the page, which is passed in the input

Note the special syntax `@[0]` and `@[1]`, which is used to reference the elements of the zipped array,
and `$.start.*` and `$.wikipedia.*` to reference external parameters. The reason for the special `$.` prefix
is JMESPath needs to reference data outside the `map` function execution context, thus the use of `$.start` instead of `start.`, and `$.wikipedia` instead of `wikipedia.`.

The step returns an array of objects with the following structure:

```json
{
  "values": [
    {
      "Content": "...",
      "ContentEmbedding": [ -0.0024764163, -0.009698434, ... ],
      "SourceId": "4b4a0a89897c480f8eb02bfc9160e677",
      "Tags": ["user=me","type=wiki","lang=en"],
      "IsTest": true,
      "Title": "Tristan da Cunha",
      "Reference": "https://en.wikipedia.org/wiki/Tristan_da_Cunha"
    },
    ...
  ]
}
```

This data is ready for insertion in the storage, and is available as `state.values`
or `combine.out.values` so we won't need further transformations in the next step.

## Store records

```yaml
- function: vector-storage-sk/upsert
  xin: >
    {
      storageType: start.input.vectorStorage,
      collection:  start.input.collection,
      fields:      start.input.fields,
      values:      combine.out.values
    }
```

## Show records created

Finally we can show the records created, using the `search` function from the `vector-storage-sk`
tool, searching by `sourceId` and `collection` to filter the records created in this pipeline.

To avoid showing too much data, we use `xout`, an output transformation, to limit the number of fields.

The search function accepts in input and OData filter expression. Since the expression needs to
contain an input parameter, we use the `join` function from JMESPath to build the expression, taking
care of escaping the internal quotes.

- SourceId: `4b4a0a89897c480f8eb02bfc9160e677`
- OData filter: `SourceId eq '4b4a0a89897c480f8eb02bfc9160e677'`
- JMESPath expression: `join('', ['SourceId eq ', '\'', $.start.input.sourceId, '\''])`
  - The expression joins with `''` (empty string) four strings:
    - `SourceId eq `
    - `'` (single quote, escaped)
    - `$.start.input.sourceId`
    - `'` (single quote, escaped)

```yaml
- function: vector-storage-sk/search
  xin: >
    {
      storageType: start.input.vectorStorage,
      collection:  start.input.collection,
      dataType:    start.input.dataType,
      fields:      start.input.fields,
      filter:      join('',['SourceId eq ','\'',$.start.input.sourceId,'\'']),
      skip:        `0`,
      top:         `1000`
    }
  xout: >
    {
      results: state.results[*].{value: {
          id: value.id,
          sourceId: value.sourceId
        }
      }
    }
```

The output looks like this:

```json
{
  "results": [
    {
      "value": {
        "id": "8c67234c-fda2-417a-997a-ce3f12ea59cf",
        "sourceId": "4b4a0a89897c480f8eb02bfc9160e677"
      }
    },
    {
      "value": {
        "id": "5ee249aa-bcef-495b-9b2c-92cedc1c442f",
        "sourceId": "4b4a0a89897c480f8eb02bfc9160e677"
      }
    },
    ...
  ]
}
```

Note: this pipeline is meant to illustrate the use of the orchestrator to ingest data from a web page.
Some steps, for instance deleting previous records to avoid duplications, are missing.

## Complete pipeline definition

```yaml
input:
  # Unique id to correlate records, used for updates/deletions
  sourceId:      4b4a0a89897c480f8eb02bfc9160e677
  # Title of the Wikipedia page to download
  page:          Tristan_da_Cunha
  # URL, used only for reference in the storage
  url:           https://en.wikipedia.org/wiki/Tristan_da_Cunha
  # Custom tags
  tags:          ['user=me','type=wiki','lang=en']
  # Size of the text chunks used to generate the embedding vectors
  chunkSize:     500
  # Optional number of tokens shared by consecutive chunks
  chunkOverlap:  0
  # Name of the tokenizer used to measure the size of the text chunks
  tokenizer:     cl100k_base
  # OpenAI model used to generate embedding vectors
  vectorModel:   openai-text-embedding-3-small
  # Size of the embedding vectors (supported only by Matryoshka models)
  vectorSize:    1536
  # Storage used (supported: azureaisearch, postgres, qdrant)
  vectorStorage: postgres
  # Name of the collection/table used to store the vectors, chunks and metadata
  collection:    wikipedia
  # Name of the schema used to create the collection/table (this is the default available out of the box)
  dataType: MemoryRecord
  # Details about the collection schema.
  # This is here to support future scenario where the schema will be defined on the fly
  # without the need to create a dataType class file.
  fields:
    - name: Id
      type: id
    - name: Content
      type: text
    - name: ContentEmbedding
      type: vector
      # Note: this is the same value from above
      vectorSize: 1536
      vectorDistance: CosineSimilarity
    - name: Tags
      type: listOfText
    - name: SourceId
      type: text
    - name: IsTest
      type: bool
    - name: Title
      type: text
    - name: Reference
      type: text
    - name: TimeStamp
      type: int
    - name: Other
      type: text

_workflow:
  steps:
    - function: wikipedia/en
      id: wikipedia
      xin: >
        { 
          title: start.input.page 
        }
    - function: chunker/chunk
      id: chunking
      xin: >
        {
          text:              state.content,
          maxTokensPerChunk: start.input.chunkSize,
          overlap:           start.input.chunkOverlap,
          tokenizer:         start.input.tokenizer
        }
    - function: embedding-generator/vectorize
      id: vectors
      xin: >
        {
          inputs:     chunking.out.chunks,
          modelId:    start.input.vectorModel,
          dimensions: start.input.vectorSize
        }
    - function: vector-storage-sk/create-collection
      xin: >
        {
          storageType: start.input.vectorStorage,
          collection:  start.input.collection, 
          fields:      start.input.fields
        }
    - id: combine
      xin: >
        {
          values: map(&{
            Content:          @[0],
            ContentEmbedding: @[1],
            SourceId:         $.start.input.sourceId,
            Tags:             $.start.input.tags,
            IsTest:           'True',
            Title:            $.wikipedia.out.title,
            Reference:        $.start.input.url
          }, zip(chunking.out.chunks, vectors.out.embeddings))
        }
    - function: vector-storage-sk/upsert
      xin: >
        {
          storageType: start.input.vectorStorage,
          collection:  start.input.collection,
          fields:      start.input.fields,
          values:      combine.out.values
        }
    - function: vector-storage-sk/search
      xin: >
        {
          storageType: start.input.vectorStorage,
          collection:  start.input.collection,
          dataType:    start.input.dataType,
          fields:      start.input.fields,
          filter:      join('',['SourceId eq ','\'',$.start.input.sourceId,'\'']),
          skip:        `0`,
          top:         `1000`
        }
      xout: >
        {
          results: state.results[*].{value: {
              id: value.id,
              sourceId: value.sourceId
            }
          }
        }
```

# Python and C# clients

The project includes also a
[Python](../service/clients/python) and a [C# client](../service/clients/dotnet/Client/)
to help creating and running generative pipelines from code. These clients are simply
wrappers around an HTTP client, making it easier to prepare the payload to pass to the
orchestrator.

Here's the same pipeline from above, using the Python client:

```python
from generative_pipelines_client import GPClient
from types import SimpleNamespace
import json

client = GPClient("http://localhost:60000")  # Orchestrator URL
pipeline = client.new_pipeline()

# Input
pipeline.input = SimpleNamespace()

pipeline.input.sourceId = "4b4a0a89897c480f8eb02bfc9160e677"
pipeline.input.page = "Tristan_da_Cunha"
pipeline.input.url = "https://en.wikipedia.org/wiki/Tristan_da_Cunha"
pipeline.input.tags = ["user=me", "type=wiki", "lang=en"]
pipeline.input.chunkSize = 500
pipeline.input.chunkOverlap = 0
pipeline.input.tokenizer = "cl100k_base"
pipeline.input.vectorModel = "openai-text-embedding-3-small"
pipeline.input.vectorSize = 1536
pipeline.input.vectorStorage = "postgres"
pipeline.input.collection = "wikipedia"
pipeline.input.dataType = "MemoryRecord"

# Vector storage schema
pipeline.input.fields = [
    {"name": "Id", "type": "id"},
    {"name": "Content", "type": "text"},
    {"name": "ContentEmbedding", "type": "vector", "vectorSize": 1536, "vectorDistance": "CosineSimilarity"},
    {"name": "Tags", "type": "listOfText"},
    {"name": "SourceId", "type": "text"},
    {"name": "IsTest", "type": "bool"},
    {"name": "Title", "type": "text"},
    {"name": "Reference", "type": "text"},
    {"name": "TimeStamp", "type": "int"},
    {"name": "Other", "type": "text"},
]

# Download web page and extract its content
pipeline.add_step(
    function="wikipedia/en",
    xin="""
        { 
            title: start.input.page 
        }
    """,
)

# Split content in small chunks ready for vectorization
pipeline.add_step(
    function="chunker/chunk",
    id="chunking",
    xin="""
        { 
            text:              state.content,
            maxTokensPerChunk: start.input.chunkSize,
            overlap:           start.input.chunkOverlap, 
            tokenizer:         start.input.tokenizer
        }
    """,
)

# Vectorize each chunk
pipeline.add_step(
    function="embedding-generator/vectorize",
    id="vectors",
    xin="""
        { 
            inputs:     chunking.out.chunks,
            modelId:    start.input.vectorModel,
            dimensions: start.input.vectorSize
        }
    """,
)

# Create vector storage record collection
pipeline.add_step(
    function="vector-storage-sk/create-collection",
    xin="""
        {
            storageType: start.input.vectorStorage,
            collection:  start.input.collection, 
            fields:      start.input.fields
        }
    """,
)

# Combine chunks and vectors
pipeline.add_step(
    id="combine",
    xin="""
        {
            values: map(&{
                Content:          @[0],
                ContentEmbedding: @[1],
                SourceId:         $.start.input.sourceId,
                Tags:             $.start.input.tags,
                IsTest:           'True',
                Title:            $.wikipedia.out.title,
                Reference:        $.start.input.url
            }, zip(chunking.out.chunks, vectors.out.embeddings))
        }
    """,
)

# Store records in vector db
pipeline.add_step(
    function="vector-storage-sk/upsert",
    xin="""
        {
            storageType: start.input.vectorStorage,
            collection:  start.input.collection,
            fields:      start.input.fields, 
            values:      combine.out.values
        }
    """,
)

# Show records created
pipeline.add_step(
    id="find_records",
    function="vector-storage-sk/search",
    xin="""
        {
            storageType: start.input.vectorStorage,
            collection:  start.input.collection,
            dataType:    start.input.dataType,
            fields:      start.input.fields,
            filter:      join('',['SourceId eq ','\\'',$.start.input.sourceId,'\\'']),
            skip:        `0`,
            top:         `1000`
        }
    """,
    xout="""
        {
            results: state.results[*].{value: {
                id: value.id,
                sourceId: value.sourceId
            }}
        }
    """,
)

# Run pipeline
result = await client.run_pipeline(pipeline)
print(json.dumps(result, indent=2))
```

and using the C# client (truncated for brevity):

```csharp
using Microsoft.GenerativePipelines.Client;
using Microsoft.Extensions.DependencyInjection;

// Dependencies setup
var provider = new ServiceCollection()
    .UseGenerativePipelines("http://localhost:60000") // Orchestrator URL
    .BuildServiceProvider();

var client = provider.GetRequiredService<GPClient>();

var pipeline = client.NewPipeline();

// Input
pipeline.Input = new
{
    sourceId = "4b4a0a89897c480f8eb02bfc9160e677",
    page = "Tristan_da_Cunha",
    url = "https://en.wikipedia.org/wiki/Tristan_da_Cunha",
    // ...
};

// Download web page and extract its content
pipeline.AddStep(
    function: "wikipedia/en",
    xin: """
         { title: start.input.page }
         """
);

// Split content in small chunks ready for vectorization
pipeline.AddStep(
    function: "chunker/chunk",
    id: "chunking",
    // ...
);

// ... other steps ...

// Run pipeline
var result = await client.RunPipelineAsync(pipeline).ConfigureAwait(false);
```


# Next Read

Dive into [DEVELOPMENT.md](DEVELOPMENT.md) to learn more.
