# Copyright (c) Microsoft. All rights reserved.

from fastapi import APIRouter
from typing import List
from models import Item

router = APIRouter()

@router.get("/items", response_model=List[Item])
async def list_items():
    return [Item(id=1, name="Item1"), Item(id=2, name="Item2")]

@router.post("/items", response_model=Item, status_code=201)
async def create_item(item: Item):
    return item