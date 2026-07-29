from fastapi import APIRouter

router = APIRouter(tags=["health"])


@router.get("/health")
def health() -> dict[str, str | int]:
    return {
        "status": "ok",
        "service": "ai-art-agent-platform",
        "schema_version": 1,
    }
