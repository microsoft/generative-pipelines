# Copyright (c) Microsoft. All rights reserved.

from fastapi import APIRouter
from typing import List
from models import User

router = APIRouter()

@router.get("/users", response_model=List[User])
async def list_users():
    return [User(id=1, username="user1"), User(id=2, username="user2")]