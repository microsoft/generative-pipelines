poetry new --src generative-pipelines-client
mv generative-pipelines-client python
cd python

poetry add --group dev pytest
poetry add --group dev pytest-asyncio
poetry add --group dev pyyaml
poetry add aiohttp

pyproject.toml:

```ini
[project]
name = "generative-pipelines-client"
version = "0.1.0"
description = "Client for Generative Pipelines"
authors = [
    {name = "Devis Lucato",email = "dluc@users.noreply.github.com"}
]
readme = "README.md"
requires-python = ">=3.11"
dependencies = [
    "aiohttp (>=3.11.14,<4.0.0)"
]

[tool.poetry]
packages = [{include = "generative_pipelines_client", from = "src"}]

[tool.poetry.group.dev.dependencies]
pyyaml = "^6.0.2"
pytest = "^8.3.5"
pytest-asyncio = "^0.26.0"

[build-system]
requires = ["poetry-core>=2.0.0,<3.0.0"]
build-backend = "poetry.core.masonry.api"
```


