from fastapi import APIRouter
from models import Status

router = APIRouter()

@router.get("/status", response_model=Status)
async def get_status():
    return Status(status="OK")