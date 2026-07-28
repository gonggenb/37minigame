from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.api.router import api_router
from app.config.settings import Settings
from app.constraints.exporter import ImageExporter
from app.constraints.workspace import ConstraintWorkspace
from app.jobs.queue import JobQueue
from app.jobs.recovery import recover_interrupted
from app.production.context import ProductionContextBuilder
from app.production.sequence_service import SequenceProductionService
from app.production.service import ProviderRegistry, StaticProductionService
from app.production.workspace import ProductionWorkspace
from app.providers.costs import CostAggregator
from app.providers.registry import OpenAIProviderRegistry
from app.style_pack.identity import CharacterIdentityStore
from app.style_pack.references import ReferenceCatalog
from app.style_pack.workspace import StylePackWorkspace
from app.workspace.project_activity import ProjectActivityService
from app.workspace.project_workspace import ProjectWorkspace

LOCAL_FRONTEND_ORIGINS = (
    "http://127.0.0.1:5173",
    "http://localhost:5173",
)


def create_app(
    settings: Settings | None = None,
    *,
    provider_registry: ProviderRegistry | None = None,
) -> FastAPI:
    app = FastAPI(
        title="2D 小游戏 AI 美术生产工作台",
        version="0.1.0",
    )
    resolved_settings = settings or Settings()
    workspace = ProjectWorkspace(resolved_settings.data_dir)
    recover_interrupted(workspace)
    app.state.settings = resolved_settings
    app.state.workspace = workspace
    style_pack_workspace = StylePackWorkspace(
        workspace,
        resolved_settings.preset_dir,
    )
    app.state.style_pack_workspace = style_pack_workspace
    reference_catalog = ReferenceCatalog(workspace, style_pack_workspace)
    identity_store = CharacterIdentityStore(workspace)
    constraint_workspace = ConstraintWorkspace(
        workspace,
        resolved_settings.preset_dir,
    )
    image_exporter = ImageExporter(workspace)
    app.state.reference_catalog = reference_catalog
    app.state.identity_store = identity_store
    app.state.constraint_workspace = constraint_workspace
    app.state.image_exporter = image_exporter
    app.state.job_queue = JobQueue(workspace)
    resolved_registry = provider_registry or OpenAIProviderRegistry(
        resolved_settings,
        workspace,
    )
    app.state.provider_registry = resolved_registry
    app.state.cost_aggregator = CostAggregator(workspace)
    production_workspace = ProductionWorkspace(workspace)
    app.state.production_workspace = production_workspace
    app.state.static_production_service = StaticProductionService(
        production_workspace,
        constraint_workspace,
        image_exporter,
        resolved_registry,
        ProductionContextBuilder(
            workspace,
            style_pack_workspace,
            reference_catalog,
            identity_store,
        ),
    )
    sequence_production_service = SequenceProductionService(
        workspace,
        constraint_workspace,
        resolved_registry,
    )
    app.state.sequence_production_service = sequence_production_service
    app.state.project_activity_service = ProjectActivityService(
        workspace,
        reference_catalog,
        production_workspace,
        sequence_production_service,
    )
    app.add_middleware(
        CORSMiddleware,
        allow_origins=list(LOCAL_FRONTEND_ORIGINS),
        allow_credentials=False,
        allow_methods=["GET", "POST", "PUT", "DELETE", "OPTIONS"],
        allow_headers=["Content-Type"],
    )
    app.include_router(api_router, prefix="/api/v1")
    return app


app = create_app()
