# Copyright (c) Microsoft. All rights reserved.

from generative_pipelines_client import GPClient
from types import SimpleNamespace
import pytest
import json


@pytest.mark.asyncio
async def test_readme_async():
    client = GPClient("http://localhost:60000")  # Update as needed
    pipeline = client.new_pipeline()

    # Input
    pipeline.input = SimpleNamespace()

    pipeline.input.sourceId = "itwikidolomiti"
    pipeline.input.page = "Dolomiti"
    pipeline.input.url = "https://it.wikipedia.org/wiki/Dolomiti"
    pipeline.input.tags = ["user=devis", "type=wiki", "lang=it"]
    pipeline.input.chunkSize = 500
    pipeline.input.chunkOverlap = 0
    pipeline.input.tokenizer = "cl100k_base"
    pipeline.input.vectorModel = "openai-text-embedding-3-small"
    pipeline.input.vectorSize = 1536
    pipeline.input.vectorStorage = "postgres"
    pipeline.input.collection = "italy"
    pipeline.input.dataType = "MemoryRecord"

    pipeline.input.fields = [
        {"name": "Id", "type": "id"},
        {"name": "Content", "type": "text"},
        {
            "name": "ContentEmbedding",
            "type": "vector",
            "vectorSize": 1536,
            "vectorDistance": "CosineSimilarity",
        },
        {"name": "Tags", "type": "listOfText"},
        {"name": "SourceId", "type": "text"},
        {"name": "IsTest", "type": "bool"},
        {"name": "Title", "type": "text"},
        {"name": "Reference", "type": "text"},
        {"name": "TimeStamp", "type": "int"},
        {"name": "Other", "type": "text"},
    ]

    # Pipeline steps
    pipeline.add_step(
        function="wikipedia/it",
        xin="""
            { 
                title: start.input.page 
            }
        """,
    )

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
                    Title:            $.start.input.page,
                    Reference:        $.start.input.url
                }, zip(chunking.out.chunks, vectors.out.embeddings))
            }
        """,
    )

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
    # print("\n== YAML ==")
    # print(pipeline.to_yaml())
    result = await client.run_pipeline(pipeline)
    print(json.dumps(result, indent=2))
