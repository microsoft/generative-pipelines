# Copyright (c) Microsoft. All rights reserved.

import json
from generative_pipelines_client.definition import PipelineDefinition, PipelineStep
from types import SimpleNamespace


class PipelineEncoder(json.JSONEncoder):
    """
    Custom JSON encoder for serializing PipelineDefinition and PipelineStep
    to a specific format expected by the Orchestrator service.

    - Fields with empty or None values are excluded.
    - Steps are serialized under "_workflow.steps".
    """

    def default(self, obj):
        if isinstance(obj, PipelineDefinition):
            return {
                "input": obj.input,
                "_workflow": {"steps": [self.default(step) for step in obj.steps]},
            }
        if isinstance(obj, PipelineStep):
            step = {}
            if obj.id:
                step["id"] = obj.id
            if obj.function:
                step["function"] = obj.function
            if obj.xin:
                step["xin"] = obj.xin
            if obj.xout:
                step["xout"] = obj.xout
            return step

        if isinstance(obj, SimpleNamespace):
            return vars(obj)  # convert to dict

        return super().default(obj)
