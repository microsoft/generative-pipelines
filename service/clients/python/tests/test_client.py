# Copyright (c) Microsoft. All rights reserved.

from generative_pipelines_client import GPClient
from types import SimpleNamespace
import pytest
import json


def test_pipeline_serialization():
    pipeline = GPClient.new_pipeline()

    pipeline.input = {
        "sourceId": "itwikidolomiti",
        "page": "Dolomiti",
        "url": "https://it.wikipedia.org/wiki/Dolomiti",
        "tags": [
            "user:devis",
            "type:wiki",
            "lang:it",
        ],
        "isTest": True,
        "chunkSize": 500,
        "chunkOverlap": 0,
        "tokenizer": "cl100k_base",
    }

    pipeline.add_step(
        id="create_collection",
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
        function="wikipedia/it",
        xin="""
            {
                title: start.input.page
            }
        """,
    )

    pipeline.add_step(
        function="chunker/chunk",
        xin="""
            {
                text:              state.content,
                maxTokensPerChunk: start.input.chunkSize,
                overlap:           start.input.chunkOverlap,
                tokenizer:         start.input.tokenizer
            }
        """,
    )

    print("== JSON ==")
    print(pipeline.to_json())

    print("\n== YAML ==")
    print(pipeline.to_yaml())


def test_pipeline_with_dynamic_input():
    pipeline = GPClient.new_pipeline()

    pipeline.input = SimpleNamespace()
    pipeline.input.sourceId = "itwikidolomiti"
    pipeline.input.page = "Dolomiti"
    pipeline.input.url = "https://it.wikipedia.org/wiki/Dolomiti"
    pipeline.input.tags = ["user:devis", "type:wiki", "lang:it"]
    pipeline.input.isTest = True
    pipeline.input.chunkSize = 500
    pipeline.input.chunkOverlap = 0
    pipeline.input.tokenizer = "cl100k_base"

    pipeline.add_step(
        function="wikipedia/it",
        xin="""
            {
                title: start.input.page
            }
        """,
    )

    print("== JSON ==")
    print(pipeline.to_json())

    print("\n== YAML ==")
    print(pipeline.to_yaml())


@pytest.mark.asyncio
async def test_pipeline_execution_async():
    client = GPClient("http://localhost:60000")

    pipeline = GPClient.new_pipeline()

    pipeline.input = {
        "sourceId": "itwikidolomiti",
        "page": "Dolomiti",
        "url": "https://it.wikipedia.org/wiki/Dolomiti",
        "tags": [
            "user:devis",
            "type:wiki",
            "lang:it",
        ],
        "isTest": True,
        "chunkSize": 500,
        "chunkOverlap": 0,
        "tokenizer": "cl100k_base",
    }

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
        xin="""
            {
                text:              state.content,
                maxTokensPerChunk: start.input.chunkSize,
                overlap:           start.input.chunkOverlap,
                tokenizer:         start.input.tokenizer
            }
        """,
    )

    result = await client.run_pipeline(pipeline)
    print(json.dumps(result, indent=2))
