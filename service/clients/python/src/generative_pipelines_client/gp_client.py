# Copyright (c) Microsoft. All rights reserved.

import aiohttp
import json
from generative_pipelines_client.definition import PipelineDefinition
from generative_pipelines_client.encoder import PipelineEncoder


class GPClient:
    """
    HTTP client for interacting with a generative pipeline backend.

    Args:
        base_url (str): Full base URL (must start with http:// or https://).
        api_key (str, optional): API key for Authorization header.

    Methods:
        new_pipeline() -> PipelineDefinition:
            Creates a new empty pipeline definition.

        run_pipeline(pipeline: PipelineDefinition) -> dict:
            Sends a pipeline definition to the server for execution.
    """

    def __init__(self, base_url: str, api_key: str = None):
        """
        Initializes the client with the given base URL.

        Args:
            base_url (str): Full base URL (must start with http:// or https://).
            api_key (str, optional): API key for Authorization header.
        """
        if not base_url.startswith("http://") and not base_url.startswith("https://"):
            raise ValueError("base_url must start with http:// or https://")
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key

    @staticmethod
    def new_pipeline() -> PipelineDefinition:
        """
        Creates and returns a new empty PipelineDefinition.
        """
        return PipelineDefinition()

    async def run_pipeline(self, pipeline: PipelineDefinition) -> dict:
        """
        Executes the given pipeline by posting it to the backend.

        Args:
            pipeline (PipelineDefinition): The pipeline to execute.

        Returns:
            dict: The parsed JSON response from the server.
        """
        return await self._post("/api/jobs", pipeline, encoder=PipelineEncoder)

    async def _post(self, path: str, data: object, encoder=None) -> dict:
        """
        Internal helper to send a POST request with optional JSON encoder.

        Args:
            path (str): Endpoint path.
            data (object): Data to serialize and send.
            encoder (json.JSONEncoder, optional): Custom encoder.

        Returns:
            dict: Parsed JSON response.
        """
        url = f"{self.base_url}{path}"
        body = json.dumps(data, cls=encoder or json.JSONEncoder).encode("utf-8")

        headers = {"Content-Type": "application/json"}
        if self.api_key:
            headers["Authorization"] = f"Bearer {self.api_key}"

        async with aiohttp.ClientSession() as session:
            async with session.post(url, data=body, headers=headers) as resp:
                resp.raise_for_status()
                return await resp.json()
