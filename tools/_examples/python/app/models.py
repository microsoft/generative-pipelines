# Copyright (c) Microsoft. All rights reserved.

from pydantic import BaseModel

class Item(BaseModel):
    id: int
    name: str

class User(BaseModel):
    id: int
    username: str

class Status(BaseModel):
    status: str