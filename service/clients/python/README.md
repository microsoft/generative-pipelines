# Generative Pipelines in Python

Example usage:

```python
from generative_pipelines_client import Client
from types import SimpleNamespace
import json


client = GPClient("http://localhost:60000")  # Update as need
pipeline = client.new_pipeline()

# Dynamic input
pipeline.input = SimpleNamespace()
pipeline.input.sourceId = "itwikidolomiti"
pipeline.input.page = "Dolomiti"

# Steps
pipeline.add_step(function="wikipedia/it",
    xin="""
        {
            title: start.input.page
        }
    """)

pipeline.add_step(function="chunker/chunk",
    id="chunking",
    xin="""
        {
            text:              state.content,
            maxTokensPerChunk: `400`,
            overlap:           `0`,
            tokenizer:         'cl100k_base'
        }
    """)

# Run
result = await client.run_pipeline(pipeline)
print(json.dumps(result, indent=2))
```