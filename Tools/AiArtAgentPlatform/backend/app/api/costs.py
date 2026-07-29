from typing import cast

from fastapi import APIRouter, HTTPException, Request, status

from app.providers.costs import CostAggregator
from app.schemas.providers import ProjectCostSummary
from app.workspace.project_workspace import ProjectNotFound

router = APIRouter(prefix="/projects/{project_id}/costs", tags=["costs"])


def get_aggregator(request: Request) -> CostAggregator:
    return cast(CostAggregator, request.app.state.cost_aggregator)


@router.get("", response_model=ProjectCostSummary)
def project_costs(project_id: str, request: Request) -> ProjectCostSummary:
    try:
        return get_aggregator(request).summarize(project_id)
    except ProjectNotFound as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=str(error),
        ) from error
