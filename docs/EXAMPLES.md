# Workflow examples

## Example 1: empty workflow

An empty workflow, with some payload. The response will be the same as the payload.

**Request:**

`POST /api/jobs`

```json
{
    "_workflow": {},
    "foo": 555,
    "bar": [ 1, 2 ]
}
```

**Response:**

```json
{
    "foo": 555,
    "bar": [ 1, 2 ]
}
```

## Example 2: simple workflow, transform input ("xin")

A basic workflow, without functions yet. "xin" is a JMESPath expression that transforms the input
for the function to invoke. There's no function yet, so the response will be the same as the transformed input.

**Request:**

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
              "xin": "{ field1: state.foo, field2: state.bar }"
            }
        ]
    },
    "foo": 555,
    "bar": [ 1, 2 ]
}
```

What happened?

- The input is transformed using the JMESPath expression in "xin".
- The expression is creating a new object, see leading '{' and trailing '}'.
- The object contains two fields: `field1` and `field2`.
- `field1` is set to the value of `state.foo`, which is 555.
- `field2` is set to the value of `state.bar`, which is [ 1, 2 ].

Note the use of `state.` prefix, which allows to reference the input payload. While running the workflow, the orchestrator
maintains an internal context object. The context object contains multiple fields, including:

- `start`: the initial payload, e.g. `start.foo` is 555 and `start.bar` is `[ 1, 2 ]`.
- `state`: the current state of the payload, which is updated as the workflow progresses.

**Response:**

```json
{
    "field1": 555,
    "field2": [ 1, 2 ]
}
```

## Example 3: simple workflow, transform input ("xin") and output ("xout")

A basic workflow, without functions yet. "xout" is a JMESPath expression that transforms the function output.
There's no function yet, so the transformation is applied to the output of "xin". The example shows also how to
use the `start.` prefix to reference the initial payload.

**Request:**

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
                "xin": "{ field1: start.foo, field2: start.bar }",
                "xout": "[ state.field1, state.field2[0] ]"
            }
        ]
    },
    "foo": 555,
    "bar": [ 1, 2 ]
}
```

What happened?

- The input was transformed using the JMESPath expression in "xin". After this step, the state
  object contains two fields: `field1` and `field2`: `{ field1: 555, field2: [ 1, 2 ] }`.
- There is no function to invoke, so the output is the same as the state object.
- The current state object is transformed using the JMESPath expression in "xout". The expression
  is creating a new array, see leading '[' and trailing ']'.
- The array contains two elements: `state.field1` and `state.field2[0]`.
- `state.field1` is `555` and `state.field2[0]` is `1`.
- The final output is `[ 555, 1 ]`.

**Response:**

```json
[ 555, 1]
```

## Example 4: simple workflow, invoke a function

The project includes a Wikipedia function based on [English Wikipedia API](https://en.wikipedia.org/w/api.php)
that can be used to fetch content, e.g. to run tests.

The following examples show how to fetch some content from Wikipedia, with and without transformations.

The "wikipedia" function expects in input a `title` field, which is provided in the request payload.

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
              "function": "wikipedia/en"
            }
        ]
    },
    "title": "Moon"
}
```

The following example transforms the output of the Wikipedia function using a JMESPath expression, so
that the output is a single string. Note that the string is JSON encoded.

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
                "function": "wikipedia/en",
                "xout": "state.content"
            }
        ]
    },
    "title": "Moon"
}
```

The following example transforms the input to read the `title` field from a `configuration` input object,
and applies the same transformation to return a string.

Using **configuration and context input objects** is a fundamental pattern in the framework, it allows
to change configurations on the fly, without redeploying the orchestrator or the functions. In fact
**functions work better with the framework when they don't have any configuration hardcoded**.

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
                "xin": "{ title: state.configuration.title }",
                "function": "wikipedia/en",
                "xout": "state.content"
            }
        ]
    },
    "configuration": {
        "title": "Moon"
    }
}
```

## Example 5: using the extractor tool

The extractor tool provides functions to extract text from documents and images. The following
example shows how to extract text from a Word file. The Word file is provided as a base64-encoded.

See the [examples](examples) folder for a real base64 value if you want to run the example.

**Request:**

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
                "function": "extractor"
            }
        ]
    },
    "fileName": "test.docx",
    "mimeType": "",
    "content": "UEsDBBQAAAAIALJ4WVrTkNHUd.....54bWxQSwUGAAAAAAwADAAIAwAADisAAAAA"
}
```

**Response:**

```json
{
    "sections": [
        {
            "content": "This is a test. The Word document has only one line of text.\n",
            "metadata": {
                "PageNumber": -1,
                "completeSentences": "true"
            }
        }
    ],
    "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "fullText": "This is a test. The Word document has only one line of text.\n",
    "size": 11820
}
```

## Example 6: sending a file, including tags, and merging values

When sending a request with data and a workflow, it can be useful to include data
that is not part of the workflow, or not part of the first step, and only need to
be used in later steps. A classic example is sending configuration settings for each
step. Another example is uploading a file and including tags used to classify the
content.
The following example shows how to send a file, including tags, and merging values
using the `merge` JMESPath function.

**Request:**

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
                "function": "extractor",
                "xout": "merge(content: state.fullText, { reference:start.reference, tags:start.tags })"
            }
        ]
    },
    "fileName": "test.docx",
    "content": "UEsDBBQAAAAIALJ4WVrTkNHUd.....54bWxQSwUGAAAAAAwADAAIAwAADisAAAAA",
    "tags": [ "user:001", "user:007", "source:doc" ],
    "reference": "https://sharepoint.com/sites/contoso/Shared%20Documents/test.docx"
}
```

**Response:**

```json
{
    "content": "This is a test. The Word document has only one line of text.\n",
    "reference": "https://sharepoint.com/sites/contoso/Shared%20Documents/test.docx",
    "tags": [
        "user:1",
        "user:2",
        "project:test"
    ]
}
```

Note the usage of `start.` prefix to reference the **initial payload**, `state.` prefix
to reference the **current state** of the payload, and `merge` function to merge data from
the two objects.

## Example 7: multiple steps: extract text from a Word file, and chunk it

The following example shows how to combine all the features seen so far, plus the ability
to concatenate multiple steps into a single workflow. The example shows how to extract text
from a Word file, and chunk it into smaller pieces. The request includes all the settings
for the chunking step under a `config` object.

The final response includes the output of the chunker step plus the `tags` and `reference`
field from the initial payload.

The response includes also a `count` field calculated by the JMESPath `length` function.
For more functions and JMESPath examples see the [JMESPath documentation](https://jmespath.org/specification.html).

**Request:**

`POST /api/jobs`

```json
{
    "_workflow": {
        "id": "base64upload",
        "steps": [
            {
                "function": "extractor",
                "xout": "merge({ tags:start.tags, reference:start.reference }, state)"
            },
            {
                "xin": "{ text: state.fullText, maxTokensPerChunk: start.config.chunkSize, overlap: start.config.overlap, chunkHeader: start.config.chunkHeader, tokenizer: start.config.tokenizer}",
                "function": "chunker",
                "xout": "{ chunks: state.chunks, count: length(state.chunks), tags: start.tags }"
            }
        ]
    },
    "config": {
        "chunkSize": 500,
        "overlap": 0,
        "chunkHeader":"==============\n",
        "tokenizer": "cl100k_base"
    },
    "tags": [
        "user:1",
        "user:2",
        "project:test"
    ],
    "reference": "https://sharepoint.com/sites/contoso/Shared%20Documents/test.docx",
    "fileName": "example.docx",
    "content": "UEsDBBQAAAAIALJ4WVrTkNHUd.....54bWxQSwUGAAAAAAwADAAIAwAADisAAAAA",
}
```

**Response:**

Note: the content is truncated for brevity.

```json
{
    "chunks": [
      "==============\nA strange and surprising event........\n",
      "==============\nId est, Rubio's 'lost in space........\n",
      [...]
      "==============\nReligious leaders offered dive........\n",
      "==============\nSatellite observation networks........"
    ],
    "count": 6,
    "tags": [
        "user:1",
        "user:2",
        "project:test"
    ]
}
```

## Example 8: multiple steps using Wikipedia content

This example is similar to the previous one, but uses Wikipedia tool which is included
in the project for learning/test purposes. Rather than uploading a file, the example
asks the Wikipedia tool to fetch some content, and then chunk it.

**Request:**

`POST /api/jobs`

```json
{
    "_workflow": {
        "steps": [
            {
                "xin": "{ title: start.config.page }",
                "function": "wikipedia/en"
            },
            {
                "xin": "{ text: state.content, maxTokensPerChunk: start.config.chunkSize, overlap: start.config.overlap, chunkHeader: start.config.chunkHeader, tokenizer: start.config.tokenizer}",
                "function": "chunker"
            }
        ]
    },
    "config": {
        "page": "Microsoft",
        "chunkSize": 500,
        "overlap": 0,
        "chunkHeader": "==============\n",
        "tokenizer": "cl100k_base"
    }
}
```

**Response:**

Note: the content is truncated for brevity.

```json
{
    "chunks": [
        "==============\nMicrosoft Corporation is.......... ",
        "==============\nIts flagship hardware pr.........\n",
        [...]
        "==============\nMicrosoft was the first .......... ",
        "==============\ngovernment to curb the p...........:"
    ]
}
```

## Example 9: text embeddings

The following example shows how to generate text embeddings using the `embedding-generator` tools,
plus a few additional features:

- The embedding generator can work on a single `input` string or multiple `inputs` strings, and
  vectors can be truncated to a specific number of dimensions (if the model supports it).
- String literals can be inlined using the `'` single quote, boolean and number values can be
  inlined using the backtick `` ` `` character.
- Steps can have a unique ID, allowing "input" and "output" of each step to be referenced
  in other steps, using the `<step id>.in.<field>` and `<step id>.out.<field>` syntax.

**Request:**

`POST /api/jobs`

```json
{
  "_workflow": {
    "steps": [
      {
        "id": "embedding1",
        "xin": "{ modelId: 'openai-text-embedding-3-small', input: 'one', dimensions: `3` }",
        "function": "embedding-generator"
      },
      {
        "id": "embedding2",
        "xin": "{ modelId: 'openai-text-embedding-3-small', inputs: ['one', 'two'], dimensions: `3` }",
        "function": "embedding-generator"
      },
      {
        "xout": "{ example1: embedding1.out.embedding, example2: embedding2.out.embeddings }"
      }
    ]
  }
}
```

**Response:**

```json
{
    "example1": [ -0.11096065, -0.762834, 0.63700235 ],
    "example2": [
        [ -0.11166364, -0.7626128, 0.6371444 ],
        [ -0.15845111, -0.3335682, 0.9293145 ]
    ]
}
```

## Example 10: chunking and multiple embedding models

The following example shows several features put together, chunking text, generating embeddings
with different models, and using custom models not present in the configuration.

- The input includes two strings, one to be chunked and vectorized, one to be just vectorized.
- The two vectorizations are generated with different embedding models.
- One of the embedding model is configured ("openai-text-embedding-3-small"), while the
  configuration for the other ("text-embedding-3-small") is specified in the workflow, including
  the endpoint to use.
- The workflow has a defined ID, so that state on disk is stored on a specific folder.

**Request:**

`POST /api/jobs`

```json
{
  "_workflow": {
    "id": "chunkAndEmbed",
    "steps": [
      {
        "id": "chunker",
        "xin": "{ text: state.content, maxTokensPerChunk: start.config.chunkSize, overlap: start.config.overlap }",
        "function": "chunker"
      },
      {
        "id": "sentenceembedding",
        "xin": "{ modelId: start.config.configuredModel, input: start.sentence, dimensions: `3` }",
        "function": "embedding-generator"
      },
      {
        "id": "chunksembedding",
        "xin": "{ provider: 'azureai', auth: 'AzureIdentity', endpoint: start.config.customEndpoint, modelId: start.config.customModel, inputs: chunker.out.chunks, supportsCustomDimensions: `true`, dimensions: `5` }",
        "function": "embedding-generator/custom"
      },
      {
        "xout": "{ tags: start.tags, sentence: sentenceembedding.out.embedding, chunks: chunksembedding.out.embeddings }"
      }
    ]
  },
  "config": {
    "chunkSize": 20, "overlap": 0,
    "customEndpoint": "https://contoso.cognitiveservices.azure.com/",
    "customModel": "text-embedding-3-small",
    "configuredModel": "openai-text-embedding-3-small"
  },
  "content": "Cerco l'estate tutto l'anno\nE all'improvviso eccola qua\nLei è partita per le spiagge\nE sono solo quassù in città\nSento fischiare sopra i tetti\nUn aeroplano che se ne va",
  "sentence": "Hello world, let's play a song",
  "tags": [ "type:song", "lang:it-it" ]
}
```

**Response:**

```json
{
    "tags": [ "type:song", "lang:it-it" ],
    "sentence": [ 0.088575736, -0.99545836, 0.034884922 ],
    "chunks": [
        [ 0.3107477, -0.36262417, 0.25312138, 0.8075352, 0.23612736 ],
        [ 0.7127997, -0.26156384, 0.64893836, 0.038096927, -0.030472761 ],
        [ 0.11102999, -0.13491312, 0.8772006, 0.17376299, -0.4120635 ]
    ]
}
```


# Next Read

Dive into [EXAMPLE-RAG-INGESTION.md](EXAMPLE-RAG-INGESTION.md) to learn more.
