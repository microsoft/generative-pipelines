# Semantic Kernel Memory Connectors

Over the past few years, while working on the **Semantic Kernel** project we have evolved
several memory-related patterns, from **Semantic Memory** — a foundational set of classes simplifying
embeddings usage across various vector databases — to **Kernel Memory**, a dedicated service for
handling multi-modal data processing. Kernel Memory handles multiple document formats, generates and
securely stores embeddings while offering advanced features like **tagging**, **filtering**,
**customizable database schemas**, **long-running jobs**, and **Retrieval-Augmented Generation(RAG)**
patterns.

Many capabilities pioneered in Kernel Memory have been integrated into Semantic Kernel through
the **Microsoft Extensions Vector Data/Store** library. This library streamlines the creation
of applications similar to Kernel Memory.

In Generative Pipelines, we used the library to create a Development Tool, a standardized web API
supporting multiple storage backends, including Postgres, Azure AI Search, and Qdrant, so that
we can easily **create pipelines over multiple storage types**, defining table schemas on the fly,
storing and searching data, leveraging semantic search.

## About Microsoft Extensions Vector Data

Resources:

- [What are Semantic Kernel Vector Store connectors?](https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors)
  - The Vector Store Abstraction
  - Define your data model
  - Connect to your database and select a collection
  - Create the collection and add records
  - Do a vector search
- [Working with data model classes](https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors/defining-your-data-model)
- Working with record definitions
  - https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/schema-with-record-definition
  - https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors/generic-data-model
  - VectorStoreRecordDefinition
  - VectorStoreRecordKeyProperty
  - VectorStoreRecordDataProperty
  - VectorStoreRecordVectorProperty
- [Serialization/Mappers\(https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors/serialization)
- [Vector Search](https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors/vector-search)
- [Hybrid Search](https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors/hybrid-search)
- [Code samples](https://learn.microsoft.com/semantic-kernel/concepts/vector-store-connectors/code-samples)
