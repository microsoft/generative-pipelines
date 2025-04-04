# Copyright (c) Microsoft. All rights reserved.

from dataclasses import dataclass, field
from typing import List, Any
from typing import Optional
from types import SimpleNamespace
import json
import yaml
import textwrap


@dataclass
class PipelineStep:
    """
    Represents a single step in a pipeline.

    Attributes:
        id (str): Unique identifier for the step. Optional. Useful to reference the step input/output in other steps.
        function (str): Function to execute in this step. Optional. When empty, only xin/xout transformations are done.
        xin (str): Input transformation in JMESPath format. Optional. If not provided, the input is the output of the previous step.
        xout (str): Output transformation in JMESPath format. Optional.
    """
    id: Optional[str] = None
    function: Optional[str] = None
    xin: Optional[str] = None
    xout: Optional[str] = None


@dataclass
class PipelineDefinition:
    """
    Represents a pipeline definition consisting of one or more steps.

    Attributes:
        input (Any): Optional input data for the pipeline.
        steps (List[PipelineStep]): Ordered list of steps in the pipeline.
    """
    input: Any = None  # user runtime data
    steps: List[PipelineStep] = field(default_factory=list)

    def add_step(
        self,
        id: str = None,
        function: str = None,
        xin: str = None,
        xout: str = None
    ) -> "PipelineDefinition":
        """
        Adds a step to the pipeline with optional fields.

        Args:
            id (str): Step ID (optional).
            function (str): Step function (optional).
            xin (str): JMESPath input expression (optional, multiline allowed).
            xout (str): JMESPath output expression (optional).

        Returns:
            self (PipelineDefinition): Enables chaining.
        """

        if isinstance(xin, str):
            xin = textwrap.dedent(xin).strip()

        if isinstance(xout, str):
            xout = textwrap.dedent(xout).strip()

        step = PipelineStep(id=id, function=function, xin=xin, xout=xout)
        self.steps.append(step)
        return self
    
    def to_json(self) -> str:
        """
        Serialize the pipeline definition to a JSON string.
        """
        from generative_pipelines_client.encoder import PipelineEncoder
        return json.dumps(self, cls=PipelineEncoder, indent=2)

    def to_yaml(self) -> str:
        """
        Serialize the pipeline definition to YAML:
        - All string values are quoted ("...") or folded (">") if multiline
        - Booleans, ints, and floats are not quoted
        - Keys are never quoted
        - None values are omitted
        - Steps are serialized as a nested structure under "_workflow"
        """
        import dataclasses
        import yaml

        class Dumper(yaml.SafeDumper):
            def represent_data(self, data):
                # Quote all strings; use folded style for multiline
                if isinstance(data, str):
                    style = '>' if '\n' in data else '"'
                    return self.represent_scalar('tag:yaml.org,2002:str', data, style=style)
                return super().represent_data(data)

            def represent_mapping(self, tag, mapping, flow_style=None):
                node = super().represent_mapping(tag, mapping, flow_style)
                for key_node, _ in node.value:
                    if key_node.tag == 'tag:yaml.org,2002:str':
                        key_node.style = None  # never quote keys
                return node

        def clean(obj):
            if dataclasses.is_dataclass(obj):
                data = {
                    k: clean(v)
                    for k, v in dataclasses.asdict(obj).items()
                    if v is not None and k != "steps"
                }
                steps = getattr(obj, "steps", None)
                if steps:
                    data["_workflow"] = {"steps": clean(steps)}
                return data
            elif isinstance(obj, SimpleNamespace):
                return clean(vars(obj))  # convert to dict
            elif isinstance(obj, dict):
                return {k: clean(v) for k, v in obj.items() if v is not None}
            elif isinstance(obj, list):
                return [clean(i) for i in obj if i is not None]
            return obj

        return yaml.dump(
            clean(self),
            Dumper=Dumper,
            sort_keys=False,
            allow_unicode=True,
            width=120
        )