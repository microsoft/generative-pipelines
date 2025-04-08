# Copyright (c) Microsoft. All rights reserved.

import os
import redis
import json
import logging
from enum import Enum
from typing import Optional, Dict, Any

# Configure logging
logging.basicConfig(level=logging.INFO)
log = logging.getLogger(__name__)


# Enum for ContentType
class ContentType(str, Enum):
    JSON = "Json"
    MULTIPART = "Multipart"


# FunctionDescription model
class FunctionDescription:
    def __init__(self, id: str, tool: str, url: str, method: str, input_type: ContentType, output_type: ContentType, description: str):
        self.id = id
        self.tool = tool
        self.url = url
        self.method = method
        self.input_type = input_type
        self.output_type = output_type
        self.description = description

    def to_dict(self) -> Dict[str, Any]:
        """Convert to a dictionary."""
        return {
            "Id": self.id,
            "Tool": self.tool,
            "Url": self.url,
            "Method": self.method,
            "InputType": self.input_type.value,
            "OutputType": self.output_type.value,
            "Description": self.description,
        }


# Redis client (singleton)
_redis_client: Optional[redis.Redis] = None


def get_redis_client() -> Optional[redis.Redis]:
    """Returns a cached Redis client instance. If not already created, it initializes one."""
    if os.getenv("GenerativePipelines__ToolsRegistryEnabled", "").lower() != "true":
        log.info("GenerativePipelines__ToolsRegistryEnabled is set to false, skipping Redis client creation.")
        return None

    global _redis_client
    if _redis_client is None:
        conn_str = os.getenv("ConnectionStrings__redisstorage", "")
        if not conn_str:
            log.warning("Redis connection string not found in environment variables.")
            return None

        host, port = conn_str.split(":")
        _redis_client = redis.Redis(host=host, port=int(port), decode_responses=True)
        log.info("Redis client ready")

    return _redis_client


def register_function(url: str, method: str, is_json: bool, description: str):
    """Registers a function in Redis."""
    redis_client = get_redis_client()
    if not redis_client:
        return

    # The tool name should be set using an env var, e.g. injected by the hosting environment
    tool_name = os.getenv("TOOL_NAME", "unknown-python-app")

    data = FunctionDescription(
        id=f"{tool_name}{url}",
        tool=tool_name,
        url=url,
        method=method,
        input_type=ContentType.JSON if is_json else ContentType.MULTIPART,
        output_type=ContentType.JSON,
        description=description
    )

    # Store a unique ID into a "functions" Redis set, used to index key-values.
    # The ID points to a Redis key where the entire function description is stored.
    # This approach allows to modify function details without causing
    # duplicate entries in the Redis set.

    # Data stored in Redis KV
    redis_data_key = f"FunctionDetails:{tool_name}:{url}"
    redis_data_value = json.dumps(to_camel_case(data.to_dict()))
    redis_client.set(redis_data_key, redis_data_value)

    # Pointer stored in Redis Set
    redis_client.sadd("functions", redis_data_key)


def to_camel_case(obj: Any) -> Any:
    """Recursively converts dictionary keys to camelCase."""
    if not isinstance(obj, dict):
        return obj
    return {
        key[0].lower() + key[1:]: to_camel_case(value) for key, value in obj.items()
    }
