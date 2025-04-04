# Copyright (c) Microsoft. All rights reserved.

from fastapi import FastAPI
from items_endpoint import router as items_router
from users_endpoint import router as users_router
from status_endpoint import router as status_router

app = FastAPI()

app.include_router(items_router)
app.include_router(users_router)
app.include_router(status_router)
