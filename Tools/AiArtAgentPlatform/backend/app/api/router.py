from fastapi import APIRouter

from app.api.constraints import router as constraints_router
from app.api.costs import router as costs_router
from app.api.health import router as health_router
from app.api.jobs import router as jobs_router
from app.api.models import router as models_router
from app.api.production import router as production_router
from app.api.projects import router as projects_router
from app.api.sequences import router as sequences_router
from app.api.style_pack import router as style_pack_router

api_router = APIRouter()
api_router.include_router(health_router)
api_router.include_router(projects_router)
api_router.include_router(jobs_router)
api_router.include_router(models_router)
api_router.include_router(style_pack_router)
api_router.include_router(constraints_router)
api_router.include_router(costs_router)
api_router.include_router(production_router)
api_router.include_router(sequences_router)
