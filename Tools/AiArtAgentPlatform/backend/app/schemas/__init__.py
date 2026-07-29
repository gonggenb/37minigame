"""Shared data contracts for the AI art production pipeline."""

from app.schemas.core import (
    AssetCategory,
    AssetTask,
    ConstraintProfile,
    GenerationPlan,
    JobStatus,
    ProjectConfig,
    QualityReport,
)

__all__ = [
    "AssetCategory",
    "AssetTask",
    "ConstraintProfile",
    "GenerationPlan",
    "JobStatus",
    "ProjectConfig",
    "QualityReport",
]
from .production import (
    CandidateEditRequest,
    CandidateReviewRequest,
    CandidateSelection,
    ProductionCandidate,
    ProductionExportRequest,
    ProductionExportResult,
    ProductionGenerateRequest,
    ProductionRun,
    StaticAssetRecord,
)
from .sequence import (
    SequenceCandidate,
    SequenceDriftReport,
    SequenceExportResult,
    SequenceFrameRecord,
    SequenceGenerationRequest,
    SequenceOutput,
    SequenceRun,
    SequenceSelection,
    SequenceTask,
)

__all__ = [
    "CandidateEditRequest",
    "CandidateReviewRequest",
    "CandidateSelection",
    "ProductionCandidate",
    "ProductionExportRequest",
    "ProductionExportResult",
    "ProductionGenerateRequest",
    "ProductionRun",
    "StaticAssetRecord",
    "SequenceCandidate",
    "SequenceDriftReport",
    "SequenceExportResult",
    "SequenceFrameRecord",
    "SequenceGenerationRequest",
    "SequenceOutput",
    "SequenceRun",
    "SequenceSelection",
    "SequenceTask",
]
