# Copyright (c) Microsoft. All rights reserved.

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import httpx
from app.libs.tool_registry import register_function

app = FastAPI()


class WikipediaGenericRequest(BaseModel):
    lang: str
    title: str


class WikipediaRequest(BaseModel):
    title: str


class WikipediaResponse(BaseModel):
    title: str
    content: str


async def fetch_wikipedia_content(title: str, lang: str = "en") -> WikipediaResponse:
    """Fetches Wikipedia content in the specified language."""
    api_url = f"https://{lang}.wikipedia.org/w/api.php"
    params = {
        "action": "query",
        "format": "json",
        "titles": title,
        "prop": "extracts",
        "explaintext": "1"
    }

    async with httpx.AsyncClient() as client:
        response = await client.get(api_url, params=params)

    if response.status_code != 200:
        raise HTTPException(status_code=500, detail="Failed to fetch data from Wikipedia API")

    data = response.json()
    pages = data.get("query", {}).get("pages", {})

    for page_content in pages.values():
        return WikipediaResponse(
            title=page_content.get("title", ""),
            content=page_content.get("extract", "")
        )

    raise HTTPException(status_code=500, detail="Could not parse Wikipedia API response")


@app.post("/", response_model=WikipediaResponse)
async def get_wikipedia_content_en(request: WikipediaGenericRequest):
    return await fetch_wikipedia_content(request.title, lang=request.lang)


@app.post("/cn", response_model=WikipediaResponse)
async def get_wikipedia_content_fr(request: WikipediaRequest):
    return await fetch_wikipedia_content(request.title, lang="cn")


@app.post("/en", response_model=WikipediaResponse)
async def get_wikipedia_content_fr(request: WikipediaRequest):
    return await fetch_wikipedia_content(request.title, lang="en")


@app.post("/es", response_model=WikipediaResponse)
async def get_wikipedia_content_fr(request: WikipediaRequest):
    return await fetch_wikipedia_content(request.title, lang="es")


@app.post("/it", response_model=WikipediaResponse)
async def get_wikipedia_content_it(request: WikipediaRequest):
    return await fetch_wikipedia_content(request.title, lang="it")


# Register functions in Orchestrator's registry
register_function(url="/", method="POST", is_json=True, description="Fetch Wikipedia content by language and title")
register_function(url="/cn", method="POST", is_json=True, description="Fetch Wikipedia Chinese content by title")
register_function(url="/en", method="POST", is_json=True, description="Fetch Wikipedia English content by title")
register_function(url="/es", method="POST", is_json=True, description="Fetch Wikipedia Spanish content by title")
register_function(url="/it", method="POST", is_json=True, description="Fetch Wikipedia Italian content by title")
