# AI 美术智能体平台项目与风格包管理闭环 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [x]) syntax for tracking.

**Goal:** 完成方案 A，使用户能够在本地工作台中管理多个项目、完整维护武侠风格包、选择参考图创建静态资产任务，并从六类最近任务中恢复工作。

**Architecture:** 后端继续使用 FastAPI、Pydantic 和本地原子文件工作区，在现有项目、参考库、静态生产与序列生产之上增加只读活动聚合和参考元数据更新。前端使用 React Query 管理服务端状态、Zustand 持久化当前项目与一次性任务导航，并把现有大卡片拆成聚焦的项目、风格圣经、素材源、参考库和活动组件。

**Tech Stack:** Python 3.12、FastAPI、Pydantic、Pillow、pytest、React 19、TypeScript、TanStack Query、Zustand、Vitest、Testing Library、Playwright。

---

## 需求描述

本计划实现 plans/plan-ai-art-agent-platform-project-style-management.md 中已批准的方案 A。实施过程中只修改 Tools/AiArtAgentPlatform 下的平台源码、测试、共享 Schema、平台 README，以及对应 plans 文档；不调用 OpenAI，不调用 Unity MCP，不修改 Unity Scene、Prefab、GameObject、Component 或 Inspector，不进入 Play Mode。

## 实现方案

### 文件职责映射

后端：

- backend/app/schemas/activity.py：项目活动摘要的唯一 Pydantic 协议。
- backend/app/schemas/style_pack.py：参考标签更新请求与材质筛选字段。
- backend/app/style_pack/references.py：参考更新、缩略图读取、数量统计和筛选。
- backend/app/workspace/project_activity.py：聚合六类静态/序列最近任务。
- backend/app/production/sequence_service.py：提供跨动画和特效的项目级运行记录读取。
- backend/app/api/projects.py：暴露项目活动摘要。
- backend/app/api/style_pack.py：暴露参考更新和缩略图接口。
- backend/app/main.py：装配 ProjectActivityService。
- backend/app/schemas/export.py：导出第 10 份共享 Schema。

前端：

- frontend/src/api/client.ts：补充无响应体 DELETE 请求。
- frontend/src/api/projects.ts：项目 CRUD、活动摘要 Query 和 Mutation。
- frontend/src/api/stylePack.ts：风格圣经更新、素材源搜索、参考筛选/更新/删除和缩略图 URL。
- frontend/src/types/core.ts：完整 ProjectConfig 与项目活动类型。
- frontend/src/stores/projectWorkspace.ts：持久化当前项目。
- frontend/src/stores/taskNavigation.ts：一次性静态/序列任务打开请求。
- frontend/src/components/ProjectWorkspaceCard.tsx：创建、选择和编辑项目。
- frontend/src/components/ProjectActivityCard.tsx：六类最近任务入口。
- frontend/src/components/StyleGuideEditor.tsx：完整风格圣经编辑器。
- frontend/src/components/ReferenceSourceBrowser.tsx：只读素材源搜索与导入。
- frontend/src/components/ReferenceLibrary.tsx：缩略图、筛选、标签更新和移除。
- frontend/src/components/ReferencePicker.tsx：静态任务最多四张参考选择。
- frontend/src/components/StylePackCard.tsx：组合风格包子组件和提示词预览。
- frontend/src/components/ProductionCard.tsx：历史静态任务恢复和 reference_ids 写入。
- frontend/src/components/SequenceCard.tsx：消费序列任务导航目标。
- frontend/src/app/App.tsx：统一当前项目和各工作区组件。

---

### Task 1: 参考元数据领域能力

**Files:**

- Modify: Tools/AiArtAgentPlatform/backend/app/schemas/style_pack.py
- Modify: Tools/AiArtAgentPlatform/backend/app/style_pack/references.py
- Modify: Tools/AiArtAgentPlatform/backend/tests/test_reference_catalog.py

- [x] **Step 1: 写入失败测试**

在 test_reference_catalog.py 增加导入：

~~~python
from app.schemas.style_pack import (
    ReferenceFilters,
    ReferenceImportRequest,
    ReferenceUpdateRequest,
)
from app.style_pack.references import (
    ReferenceAlreadyExists,
    ReferenceCatalog,
    ReferenceNotFound,
)
~~~

增加测试：

~~~python
def test_reference_metadata_update_thumbnail_and_material_filter(
    tmp_path: Path,
) -> None:
    catalog, source_root, _ = _create_catalog(tmp_path)
    source = source_root / "hero.png"
    _write_image(source, (96, 128), (60, 80, 70, 255))
    source_hash = hashlib.sha256(source.read_bytes()).hexdigest()
    imported = catalog.import_reference(
        "wuxia-demo",
        ReferenceImportRequest(
            reference_id="hero-main",
            source_relative_path="hero.png",
            categories=[AssetCategory.CHARACTER],
            identities=["hero"],
            usages=["gameplay"],
            viewpoints=["topdown-45"],
            materials=["ink-cloth"],
        ),
    )

    updated = catalog.update_reference(
        "wuxia-demo",
        "hero-main",
        ReferenceUpdateRequest(
            categories=[AssetCategory.CHARACTER, AssetCategory.ANIMATION],
            identities=["hero", "young-swordsman"],
            usages=["gameplay", "animation-seed"],
            viewpoints=["topdown-45"],
            materials=["ink-cloth", "rice-paper"],
            notes="批准的角色身份参考",
        ),
    )

    assert updated.sha256 == imported.sha256
    assert updated.workspace_relative_path == imported.workspace_relative_path
    assert updated.thumbnail_relative_path == imported.thumbnail_relative_path
    assert updated.materials == ["ink-cloth", "rice-paper"]
    assert catalog.read_thumbnail("wuxia-demo", "hero-main").startswith(b"\x89PNG")
    assert [
        item.reference_id
        for item in catalog.list_references(
            "wuxia-demo",
            ReferenceFilters(material="rice-paper"),
        )
    ] == ["hero-main"]
    assert catalog.count_references("wuxia-demo") == 1
    assert hashlib.sha256(source.read_bytes()).hexdigest() == source_hash

    with pytest.raises(ReferenceNotFound):
        catalog.update_reference(
            "wuxia-demo",
            "missing",
            ReferenceUpdateRequest(categories=[AssetCategory.CHARACTER]),
        )
~~~

- [x] **Step 2: 运行测试并确认失败**

Run:

~~~powershell
python -m pytest backend/tests/test_reference_catalog.py::test_reference_metadata_update_thumbnail_and_material_filter -v
~~~

Expected: FAIL，提示 ReferenceUpdateRequest、update_reference、read_thumbnail 或 material 尚不存在。

- [x] **Step 3: 实现 Schema**

在 ReferenceImportRequest 后增加：

~~~python
class ReferenceUpdateRequest(StrictModel):
    categories: list[AssetCategory] = Field(min_length=1)
    identities: list[str] = Field(default_factory=list)
    usages: list[str] = Field(default_factory=list)
    viewpoints: list[str] = Field(default_factory=list)
    materials: list[str] = Field(default_factory=list)
    notes: str = Field(default="", max_length=2000)
~~~

把 ReferenceFilters 改为：

~~~python
class ReferenceFilters(StrictModel):
    category: AssetCategory | None = None
    identity: str | None = None
    usage: str | None = None
    viewpoint: str | None = None
    material: str | None = None
    limit: int = Field(default=100, ge=1, le=500)
~~~

- [x] **Step 4: 实现 ReferenceCatalog**

导入 ReferenceUpdateRequest，并在 delete_reference 前加入：

~~~python
    def update_reference(
        self,
        project_id: str,
        reference_id: str,
        request: ReferenceUpdateRequest,
    ) -> ReferenceAsset:
        index = self._read_index(project_id)
        position = next(
            (
                position
                for position, item in enumerate(index.references)
                if item.reference_id == reference_id
            ),
            None,
        )
        if position is None:
            raise ReferenceNotFound(reference_id)
        current = index.references[position]
        updated = current.model_copy(
            update={
                "categories": request.categories,
                "identities": request.identities,
                "usages": request.usages,
                "viewpoints": request.viewpoints,
                "materials": request.materials,
                "notes": request.notes,
            }
        )
        index.references[position] = updated
        self._write_index(project_id, index)
        return updated

    def read_thumbnail(self, project_id: str, reference_id: str) -> bytes:
        reference = next(
            (
                item
                for item in self._read_index(project_id).references
                if item.reference_id == reference_id
            ),
            None,
        )
        if reference is None:
            raise ReferenceNotFound(reference_id)
        project_root = self.projects.project_path(project_id)
        thumbnail = safe_child(
            project_root,
            *Path(reference.thumbnail_relative_path).parts,
        )
        if not thumbnail.is_file():
            raise ReferenceNotFound(reference_id)
        return thumbnail.read_bytes()

    def count_references(self, project_id: str) -> int:
        return len(self._read_index(project_id).references)
~~~

把 _matches 的 comparisons 改为：

~~~python
        comparisons = (
            (filters.identity, reference.identities),
            (filters.usage, reference.usages),
            (filters.viewpoint, reference.viewpoints),
            (filters.material, reference.materials),
        )
~~~

- [x] **Step 5: 运行参考目录测试**

Run:

~~~powershell
python -m pytest backend/tests/test_reference_catalog.py -v
~~~

Expected: PASS。

- [x] **Step 6: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/backend/app/schemas/style_pack.py Tools/AiArtAgentPlatform/backend/app/style_pack/references.py Tools/AiArtAgentPlatform/backend/tests/test_reference_catalog.py
git commit -m "feat: manage reference metadata"
~~~

---

### Task 2: 参考更新与缩略图 API

**Files:**

- Modify: Tools/AiArtAgentPlatform/backend/app/api/style_pack.py
- Modify: Tools/AiArtAgentPlatform/backend/tests/test_style_pack_api.py

- [x] **Step 1: 写入 API 失败测试**

在现有综合路由测试导入参考之后、身份保存之前加入：

~~~python
        source_hash = __import__("hashlib").sha256(
            (source_root / "hero.png").read_bytes()
        ).hexdigest()
        updated = await client.put(
            "/api/v1/projects/wuxia-demo/references/hero-main",
            json={
                "categories": ["character", "animation"],
                "identities": ["hero-main"],
                "usages": ["gameplay", "animation-seed"],
                "viewpoints": ["topdown-45"],
                "materials": ["rice-paper"],
                "notes": "批准参考",
            },
        )
        assert updated.status_code == httpx.codes.OK
        assert updated.json()["materials"] == ["rice-paper"]

        thumbnail = await client.get(
            "/api/v1/projects/wuxia-demo/references/hero-main/thumbnail"
        )
        assert thumbnail.status_code == httpx.codes.OK
        assert thumbnail.headers["content-type"] == "image/png"
        assert thumbnail.content.startswith(b"\x89PNG")

        filtered_by_material = await client.get(
            "/api/v1/projects/wuxia-demo/references",
            params={"material": "rice-paper"},
        )
        assert [
            item["reference_id"] for item in filtered_by_material.json()
        ] == ["hero-main"]
        assert __import__("hashlib").sha256(
            (source_root / "hero.png").read_bytes()
        ).hexdigest() == source_hash
~~~

在第二个测试增加：

~~~python
        missing_reference = await client.put(
            "/api/v1/projects/missing/references/hero-main",
            json={"categories": ["character"]},
        )
        missing_thumbnail = await client.get(
            "/api/v1/projects/missing/references/hero-main/thumbnail"
        )
~~~

并断言：

~~~python
    assert missing_reference.status_code == httpx.codes.NOT_FOUND
    assert missing_thumbnail.status_code == httpx.codes.NOT_FOUND
~~~

- [x] **Step 2: 运行测试并确认失败**

Run:

~~~powershell
python -m pytest backend/tests/test_style_pack_api.py -v
~~~

Expected: FAIL，PUT 和 thumbnail 路由返回 405 或 404。

- [x] **Step 3: 实现 API**

从 schemas.style_pack 导入 ReferenceUpdateRequest。给 list_references 增加参数：

~~~python
    material: str | None = Query(default=None, max_length=120),
~~~

构造 ReferenceFilters 时加入：

~~~python
                material=material,
~~~

在 DELETE 路由之前加入：

~~~python
@router.put("/references/{reference_id}", response_model=ReferenceAsset)
def update_reference(
    project_id: str,
    reference_id: str,
    request_data: ReferenceUpdateRequest,
    request: Request,
) -> ReferenceAsset:
    try:
        return get_references(request).update_reference(
            project_id,
            reference_id,
            request_data,
        )
    except (ProjectNotFound, ReferenceNotFound) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or reference not found",
        ) from error
    except (PathViolation, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_CONTENT,
            detail=str(error),
        ) from error


@router.get("/references/{reference_id}/thumbnail")
def read_reference_thumbnail(
    project_id: str,
    reference_id: str,
    request: Request,
) -> Response:
    try:
        content = get_references(request).read_thumbnail(project_id, reference_id)
    except (ProjectNotFound, ReferenceNotFound) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project or reference thumbnail not found",
        ) from error
    return Response(content=content, media_type="image/png")
~~~

- [x] **Step 4: 运行 API 测试**

Run:

~~~powershell
python -m pytest backend/tests/test_style_pack_api.py backend/tests/test_reference_catalog.py -v
~~~

Expected: PASS。

- [x] **Step 5: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/backend/app/api/style_pack.py Tools/AiArtAgentPlatform/backend/tests/test_style_pack_api.py
git commit -m "feat: expose reference library editing"
~~~

---

### Task 3: 项目活动领域聚合

**Files:**

- Create: Tools/AiArtAgentPlatform/backend/app/schemas/activity.py
- Create: Tools/AiArtAgentPlatform/backend/app/workspace/project_activity.py
- Modify: Tools/AiArtAgentPlatform/backend/app/production/sequence_service.py
- Create: Tools/AiArtAgentPlatform/backend/tests/test_project_activity.py

- [x] **Step 1: 写入项目活动失败测试**

创建 test_project_activity.py：

~~~python
from pathlib import Path

import yaml
from app.constraints.workspace import ConstraintWorkspace
from app.production.sequence_service import SequenceProductionService
from app.production.workspace import ProductionWorkspace
from app.schemas.core import AssetCategory, AssetTask, ProjectConfig
from app.schemas.production import ProductionRun, StaticAssetRecord
from app.schemas.sequence import SequenceTask
from app.style_pack.references import ReferenceCatalog
from app.style_pack.workspace import StylePackWorkspace
from app.workspace.project_activity import ProjectActivityService
from app.workspace.project_workspace import ProjectWorkspace


def _write_style_preset(preset_dir: Path, source_root: Path) -> None:
    path = preset_dir / "wuxia-ink-chibi-topdown-2_5d" / "style-guide.yaml"
    path.parent.mkdir(parents=True)
    path.write_text(
        yaml.safe_dump(
            {
                "schema_version": 1,
                "style_id": "wuxia-ink-chibi-topdown-2_5d",
                "display_name": "武侠",
                "reference_source": {
                    "path": str(source_root),
                    "mode": "read_only",
                },
                "camera": {
                    "projection": "orthographic_like",
                    "pitch_semantic_min": 35,
                    "pitch_semantic_max": 55,
                    "shared_view_required": True,
                    "default_facing": "right",
                },
                "palette": {"base": ["ink"], "accents": []},
                "rendering": {
                    "character_proportion": "chibi",
                    "character_outline": "ink",
                    "environment_detail": "restrained",
                    "surface_finish": "matte",
                    "shadow_direction": "lower_right",
                },
                "readability": {},
                "ui": {},
                "forbidden": [],
            },
            allow_unicode=True,
        ),
        encoding="utf-8",
    )


def test_project_activity_groups_static_and_sequence_work(
    tmp_path: Path,
) -> None:
    source_root = tmp_path / "source"
    source_root.mkdir()
    preset_dir = tmp_path / "presets"
    _write_style_preset(preset_dir, source_root)
    projects = ProjectWorkspace(tmp_path / "data")
    projects.create_project(
        ProjectConfig(project_id="wuxia-demo", display_name="武侠美术")
    )
    references = ReferenceCatalog(
        projects,
        StylePackWorkspace(projects, preset_dir),
    )
    production = ProductionWorkspace(projects)
    task = AssetTask(
        asset_id="green-sword",
        category=AssetCategory.ITEM,
        name="青锋剑",
        brief="水墨青锋剑",
        usage="world-sprite",
        style_pack="wuxia-ink-chibi-topdown-2_5d",
        constraint_profile="wuxia-item",
        output_mode="single-png",
    )
    production.create_asset("wuxia-demo", StaticAssetRecord(task=task))
    production.create_run(
        ProductionRun(
            run_id="run-item",
            project_id="wuxia-demo",
            task=task,
            status="planned",
        )
    )
    sequences = SequenceProductionService(
        projects,
        ConstraintWorkspace(projects, preset_dir),
        object(),
        id_factory=lambda: "run-effect",
    )
    sequences.create_reference(
        "wuxia-demo",
        SequenceTask(
            asset_id="sword-flash",
            category=AssetCategory.EFFECT,
            name="剑光",
            action="effect",
            frame_count=4,
            rows=2,
            columns=2,
            frame_width=256,
            frame_height=256,
            preview_fps=8,
        ),
    )
    service = ProjectActivityService(
        projects,
        references,
        production,
        sequences,
    )

    summary = service.summarize("wuxia-demo")

    assert [item.category for item in summary.categories] == [
        AssetCategory.CHARACTER,
        AssetCategory.SCENE,
        AssetCategory.ITEM,
        AssetCategory.ANIMATION,
        AssetCategory.EFFECT,
        AssetCategory.UI,
    ]
    item_activity = summary.categories[2]
    effect_activity = summary.categories[4]
    assert item_activity.task_count == 1
    assert item_activity.recent[0].run_id == "run-item"
    assert effect_activity.task_count == 1
    assert effect_activity.recent[0].run_id == "run-effect"
    assert sequences.list_project_runs("wuxia-demo")[0].run_id == "run-effect"
~~~

- [x] **Step 2: 运行测试并确认失败**

Run:

~~~powershell
python -m pytest backend/tests/test_project_activity.py -v
~~~

Expected: FAIL，activity 模块和 list_project_runs 不存在。

- [x] **Step 3: 创建活动 Schema**

创建 activity.py：

~~~python
from datetime import datetime
from typing import Literal

from pydantic import Field, model_validator

from .core import SLUG_PATTERN, AssetCategory, StrictModel


class ProjectActivityItem(StrictModel):
    workflow: Literal["static", "sequence"]
    category: AssetCategory
    asset_id: str = Field(pattern=SLUG_PATTERN)
    name: str = Field(min_length=1, max_length=120)
    status: str = Field(min_length=1, max_length=80)
    run_id: str | None = Field(default=None, pattern=SLUG_PATTERN)
    updated_at: datetime


class ProjectCategoryActivity(StrictModel):
    category: AssetCategory
    task_count: int = Field(ge=0)
    recent: list[ProjectActivityItem] = Field(default_factory=list, max_length=5)


class ProjectActivitySummary(StrictModel):
    schema_version: Literal[1] = 1
    project_id: str = Field(pattern=SLUG_PATTERN)
    reference_count: int = Field(ge=0)
    categories: list[ProjectCategoryActivity] = Field(min_length=6, max_length=6)

    @model_validator(mode="after")
    def require_all_categories_once(self) -> "ProjectActivitySummary":
        expected = list(AssetCategory)
        actual = [item.category for item in self.categories]
        if len(set(actual)) != 6 or set(actual) != set(expected):
            raise ValueError("project activity must contain every asset category once")
        return self
~~~

- [x] **Step 4: 增加序列项目级读取**

在 SequenceProductionService.list_runs 后加入：

~~~python
    def list_project_runs(self, project_id: str) -> list[SequenceRun]:
        project_root = self.workspace.project_path(project_id)
        self.workspace.get_project(project_id)
        runs: list[SequenceRun] = []
        for category in sorted(self.SEQUENCE_CATEGORIES, key=lambda item: item.value):
            category_path = safe_child(project_root, "assets", category.value)
            if not category_path.is_dir():
                continue
            for run_file in sorted(category_path.glob("*/runs/*/run.json")):
                asset_id = run_file.parents[2].name
                run_id = run_file.parent.name
                try:
                    runs.append(
                        self.get_run(project_id, category, asset_id, run_id)
                    )
                except (FileNotFoundError, PathViolation, ValueError):
                    continue
        return sorted(runs, key=lambda item: item.updated_at, reverse=True)
~~~

- [x] **Step 5: 创建聚合服务**

创建 project_activity.py：

~~~python
from __future__ import annotations

from collections import defaultdict

from app.production.sequence_service import SequenceProductionService
from app.production.workspace import ProductionWorkspace
from app.schemas.activity import (
    ProjectActivityItem,
    ProjectActivitySummary,
    ProjectCategoryActivity,
)
from app.schemas.core import AssetCategory
from app.style_pack.references import ReferenceCatalog
from app.workspace.project_workspace import ProjectWorkspace

CATEGORY_ORDER = (
    AssetCategory.CHARACTER,
    AssetCategory.SCENE,
    AssetCategory.ITEM,
    AssetCategory.ANIMATION,
    AssetCategory.EFFECT,
    AssetCategory.UI,
)


class ProjectActivityService:
    def __init__(
        self,
        projects: ProjectWorkspace,
        references: ReferenceCatalog,
        production: ProductionWorkspace,
        sequences: SequenceProductionService,
    ) -> None:
        self.projects = projects
        self.references = references
        self.production = production
        self.sequences = sequences

    def summarize(self, project_id: str) -> ProjectActivitySummary:
        self.projects.get_project(project_id)
        items: dict[AssetCategory, list[ProjectActivityItem]] = defaultdict(list)
        counts: dict[AssetCategory, set[str]] = defaultdict(set)

        for asset in self.production.list_assets(project_id):
            runs = self.production.list_runs(
                project_id,
                asset.task.category,
                asset.task.asset_id,
            )
            latest = runs[0] if runs else None
            counts[asset.task.category].add(asset.task.asset_id)
            items[asset.task.category].append(
                ProjectActivityItem(
                    workflow="static",
                    category=asset.task.category,
                    asset_id=asset.task.asset_id,
                    name=asset.task.name,
                    status=latest.status if latest else "draft",
                    run_id=latest.run_id if latest else None,
                    updated_at=latest.updated_at if latest else asset.updated_at,
                )
            )

        for run in self.sequences.list_project_runs(project_id):
            counts[run.task.category].add(run.task.asset_id)
            items[run.task.category].append(
                ProjectActivityItem(
                    workflow="sequence",
                    category=run.task.category,
                    asset_id=run.task.asset_id,
                    name=run.task.name,
                    status=run.status,
                    run_id=run.run_id,
                    updated_at=run.updated_at,
                )
            )

        categories = [
            ProjectCategoryActivity(
                category=category,
                task_count=len(counts[category]),
                recent=sorted(
                    items[category],
                    key=lambda item: item.updated_at,
                    reverse=True,
                )[:5],
            )
            for category in CATEGORY_ORDER
        ]
        return ProjectActivitySummary(
            project_id=project_id,
            reference_count=self.references.count_references(project_id),
            categories=categories,
        )
~~~

- [x] **Step 6: 运行领域测试**

Run:

~~~powershell
python -m pytest backend/tests/test_project_activity.py backend/tests/test_sequence_service.py -v
~~~

Expected: PASS。

- [x] **Step 7: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/backend/app/schemas/activity.py Tools/AiArtAgentPlatform/backend/app/workspace/project_activity.py Tools/AiArtAgentPlatform/backend/app/production/sequence_service.py Tools/AiArtAgentPlatform/backend/tests/test_project_activity.py
git commit -m "feat: aggregate project activity"
~~~

---

### Task 4: 项目活动 API 与共享 Schema

**Files:**

- Modify: Tools/AiArtAgentPlatform/backend/app/api/projects.py
- Modify: Tools/AiArtAgentPlatform/backend/app/main.py
- Modify: Tools/AiArtAgentPlatform/backend/app/schemas/export.py
- Modify: Tools/AiArtAgentPlatform/backend/tests/test_projects_api.py
- Create: Tools/AiArtAgentPlatform/shared/schemas/project-activity.schema.json

- [x] **Step 1: 写入活动路由失败测试**

在 test_projects_api.py 创建项目后加入：

~~~python
        asset = await client.post(
            "/api/v1/projects/wuxia-demo/assets",
            json={
                "asset_id": "green-sword",
                "category": "item",
                "name": "青锋剑",
                "brief": "水墨青锋剑",
                "usage": "world-sprite",
                "style_pack": "wuxia-ink-chibi-topdown-2_5d",
                "reference_ids": [],
                "constraint_profile": "wuxia-item",
                "constraint_overrides": {},
                "candidate_count": 4,
                "output_mode": "single-png",
            },
        )
        assert asset.status_code == httpx.codes.CREATED
        activity = await client.get("/api/v1/projects/wuxia-demo/activity")
        assert activity.status_code == httpx.codes.OK
        payload = activity.json()
        assert payload["project_id"] == "wuxia-demo"
        assert [item["category"] for item in payload["categories"]] == [
            "character",
            "scene",
            "item",
            "animation",
            "effect",
            "ui",
        ]
        assert payload["categories"][2]["recent"][0]["asset_id"] == "green-sword"
~~~

增加缺失项目断言：

~~~python
        missing_activity = await client.get("/api/v1/projects/missing/activity")
        assert missing_activity.status_code == httpx.codes.NOT_FOUND
~~~

- [x] **Step 2: 运行测试并确认失败**

Run:

~~~powershell
python -m pytest backend/tests/test_projects_api.py -v
~~~

Expected: FAIL，activity 路由不存在。

- [x] **Step 3: 装配服务与路由**

在 main.py 创建 sequence_production_service 局部变量，再同时写入 state：

~~~python
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
~~~

从 app.workspace.project_activity 导入 ProjectActivityService。

在 projects.py 增加：

~~~python
from app.schemas.activity import ProjectActivitySummary
from app.workspace.project_activity import ProjectActivityService


def get_activity_service(request: Request) -> ProjectActivityService:
    return cast(
        ProjectActivityService,
        request.app.state.project_activity_service,
    )


@router.get("/{project_id}/activity", response_model=ProjectActivitySummary)
def read_project_activity(
    project_id: str,
    request: Request,
) -> ProjectActivitySummary:
    try:
        return get_activity_service(request).summarize(project_id)
    except (ProjectNotFound, ValueError) as error:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="project not found",
        ) from error
~~~

- [x] **Step 4: 导出活动 Schema**

在 schemas/export.py 导入 ProjectActivitySummary，并在 SCHEMA_MODELS 首项加入：

~~~python
    "project-activity.schema.json": ProjectActivitySummary,
~~~

Run:

~~~powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-schemas.ps1
~~~

Expected: shared/schemas/project-activity.schema.json 生成，目录共有 10 个 Schema。

- [x] **Step 5: 运行后端回归**

Run:

~~~powershell
python -m pytest backend/tests/test_projects_api.py backend/tests/test_project_activity.py backend/tests/test_schemas.py -v
python -m ruff check backend
python -m mypy backend/app
~~~

Expected: 全部 PASS。

- [x] **Step 6: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/backend/app/api/projects.py Tools/AiArtAgentPlatform/backend/app/main.py Tools/AiArtAgentPlatform/backend/app/schemas/export.py Tools/AiArtAgentPlatform/backend/tests/test_projects_api.py Tools/AiArtAgentPlatform/shared/schemas/project-activity.schema.json
git commit -m "feat: expose project activity summary"
~~~

---

### Task 5: 前端项目与风格包 API 基础

**Files:**

- Modify: Tools/AiArtAgentPlatform/frontend/src/api/client.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/api/client.test.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/types/core.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/api/projects.ts
- Create: Tools/AiArtAgentPlatform/frontend/src/api/projects.test.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/api/stylePack.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/api/stylePack.test.ts

- [x] **Step 1: 写入 API 失败测试**

在 client.test.ts 增加：

~~~typescript
it("sends a DELETE request without parsing an empty response", async () => {
  const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
  vi.stubGlobal("fetch", fetchMock);

  await deleteRequest("/api/v1/projects/wuxia-demo/references/hero-main");

  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/projects/wuxia-demo/references/hero-main",
    expect.objectContaining({ method: "DELETE" }),
  );
});
~~~

创建 projects.test.ts，覆盖 fetch/create/update/activity：

~~~typescript
import { afterEach, expect, it, vi } from "vitest";

import {
  createProject,
  fetchProjectActivity,
  fetchProjects,
  updateProject,
} from "./projects";

afterEach(() => vi.unstubAllGlobals());

it("creates updates and reads project activity", async () => {
  const responses = [
    [{ schema_version: 1, project_id: "wuxia-demo", display_name: "武侠美术" }],
    { schema_version: 1, project_id: "wuxia-new", display_name: "新项目" },
    { schema_version: 1, project_id: "wuxia-new", display_name: "新名称" },
    {
      schema_version: 1,
      project_id: "wuxia-new",
      reference_count: 12,
      categories: [],
    },
  ];
  const fetchMock = vi.fn().mockImplementation(() =>
    Promise.resolve(
      new Response(JSON.stringify(responses.shift()), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    ),
  );
  vi.stubGlobal("fetch", fetchMock);

  await fetchProjects();
  await createProject({
    project_id: "wuxia-new",
    display_name: "新项目",
    language: "zh-CN",
  });
  await updateProject("wuxia-new", {
    schema_version: 1,
    project_id: "wuxia-new",
    display_name: "新名称",
    visual_type: "wuxia-ink-chibi-topdown-2_5d",
    language: "zh-CN",
    models: {
      planner_model: "gpt-5.6",
      review_model: "gpt-5.6",
      image_model: "gpt-image-2",
    },
    generation: {
      candidate_count: 4,
      automatic_retry_count: 2,
      image_quality: "high",
      transparency_mode: "postprocess",
    },
    review: {
      enabled: true,
      minimum_style_score: 75,
      hard_constraints_required: true,
    },
  });
  const activity = await fetchProjectActivity("wuxia-new");

  expect(activity.reference_count).toBe(12);
  expect(fetchMock.mock.calls[1][1]?.method).toBe("POST");
  expect(fetchMock.mock.calls[2][1]?.method).toBe("PUT");
});
~~~

在 stylePack.test.ts 增加 update、material filter、thumbnail 和 delete 断言。

- [x] **Step 2: 运行测试并确认失败**

Run:

~~~powershell
pnpm --dir frontend test -- src/api/client.test.ts src/api/projects.test.ts src/api/stylePack.test.ts
~~~

Expected: FAIL，缺少 deleteRequest 与新 API。

- [x] **Step 3: 实现 client 和完整类型**

在 client.ts 增加：

~~~typescript
export async function deleteRequest(
  path: string,
  { timeoutMs = 10_000 }: JsonRequestOptions = {},
): Promise<void> {
  const controller = new AbortController();
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(path, {
      method: "DELETE",
      headers: { Accept: "application/json" },
      signal: controller.signal,
    });
    if (!response.ok) {
      throw new ApiError(await responseErrorMessage(response), response.status);
    }
  } finally {
    window.clearTimeout(timeout);
  }
}
~~~

把 core.ts 的 ProjectConfig 扩展为完整后端协议，并增加：

~~~typescript
export interface ProjectActivityItem {
  workflow: "static" | "sequence";
  category: AssetCategory;
  asset_id: string;
  name: string;
  status: string;
  run_id: string | null;
  updated_at: string;
}

export interface ProjectCategoryActivity {
  category: AssetCategory;
  task_count: number;
  recent: ProjectActivityItem[];
}

export interface ProjectActivitySummary {
  schema_version: 1;
  project_id: string;
  reference_count: number;
  categories: ProjectCategoryActivity[];
}
~~~

- [x] **Step 4: 实现项目 API**

projects.ts 增加 createProject、updateProject、fetchProjectActivity、useCreateProjectMutation、useUpdateProjectMutation 和 useProjectActivityQuery。Mutation 成功后失效：

~~~typescript
await queryClient.invalidateQueries({ queryKey: ["projects"] });
await queryClient.invalidateQueries({
  queryKey: ["project-activity", projectId],
});
~~~

创建请求类型固定为：

~~~typescript
export interface ProjectCreateInput {
  project_id: string;
  display_name: string;
  language: "zh-CN" | "en-US";
}
~~~

- [x] **Step 5: 实现风格包 API**

stylePack.ts 增加：

~~~typescript
export interface SourceReferenceFile {
  relative_path: string;
  size_bytes: number;
}

export interface ReferenceFilters {
  category?: AssetCategory;
  identity?: string;
  usage?: string;
  viewpoint?: string;
  material?: string;
  limit?: number;
}

export type ReferenceUpdateInput = Pick<
  ReferenceAsset,
  "categories" | "identities" | "usages" | "viewpoints" | "materials" | "notes"
>;
~~~

实现 updateStyleGuide、fetchReferenceSource、fetchReferences(filters)、updateReference、deleteReference 和 referenceThumbnailUrl。Query Key 必须包含 projectId 与筛选对象；更新、导入和删除成功后同时失效 references 与 project-activity。

- [x] **Step 6: 运行 API 测试**

Run:

~~~powershell
pnpm --dir frontend test -- src/api/client.test.ts src/api/projects.test.ts src/api/stylePack.test.ts
pnpm --dir frontend typecheck
~~~

Expected: PASS。

- [x] **Step 7: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/src/api/client.ts Tools/AiArtAgentPlatform/frontend/src/api/client.test.ts Tools/AiArtAgentPlatform/frontend/src/types/core.ts Tools/AiArtAgentPlatform/frontend/src/api/projects.ts Tools/AiArtAgentPlatform/frontend/src/api/projects.test.ts Tools/AiArtAgentPlatform/frontend/src/api/stylePack.ts Tools/AiArtAgentPlatform/frontend/src/api/stylePack.test.ts
git commit -m "feat: add project and reference APIs"
~~~

---

### Task 6: 当前项目持久化与项目工作区面板

**Files:**

- Create: Tools/AiArtAgentPlatform/frontend/src/stores/projectWorkspace.ts
- Create: Tools/AiArtAgentPlatform/frontend/src/stores/projectWorkspace.test.ts
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ProjectWorkspaceCard.tsx
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ProjectWorkspaceCard.test.tsx

- [x] **Step 1: 写入 Store 失败测试**

创建 projectWorkspace.test.ts：

~~~typescript
import { beforeEach, expect, it } from "vitest";

import {
  resolveActiveProjectId,
  useProjectWorkspaceStore,
} from "./projectWorkspace";

beforeEach(() => {
  localStorage.clear();
  useProjectWorkspaceStore.setState({ activeProjectId: null });
});

it("keeps a valid project and falls back from a stale one", () => {
  const projects = [{ project_id: "alpha" }, { project_id: "beta" }];
  expect(resolveActiveProjectId("beta", projects)).toBe("beta");
  expect(resolveActiveProjectId("missing", projects)).toBe("alpha");
  expect(resolveActiveProjectId("alpha", [])).toBeNull();
});

it("persists the selected project id", () => {
  useProjectWorkspaceStore.getState().setActiveProjectId("beta");
  expect(
    JSON.parse(localStorage.getItem("ai-art-project-workspace") || "{}").state
      .activeProjectId,
  ).toBe("beta");
});
~~~

- [x] **Step 2: 写入组件失败测试**

创建 ProjectWorkspaceCard.test.tsx，mock 项目 API，验证：

~~~typescript
fireEvent.change(screen.getByLabelText("当前项目"), {
  target: { value: "project-b" },
});
expect(onSelect).toHaveBeenCalledWith("project-b");

fireEvent.click(screen.getByRole("button", { name: "新建项目" }));
fireEvent.change(screen.getByLabelText("新项目 ID"), {
  target: { value: "wuxia-new" },
});
fireEvent.change(screen.getByLabelText("新项目名称"), {
  target: { value: "新武侠项目" },
});
fireEvent.click(screen.getByRole("button", { name: "创建并切换" }));
await waitFor(() => expect(onSelect).toHaveBeenCalledWith("wuxia-new"));
~~~

再验证编辑表单发送完整 ProjectConfig，project_id 和 visual_type 输入为 disabled。

- [x] **Step 3: 运行测试并确认失败**

Run:

~~~powershell
pnpm --dir frontend test -- src/stores/projectWorkspace.test.ts src/components/ProjectWorkspaceCard.test.tsx
~~~

Expected: FAIL，文件不存在。

- [x] **Step 4: 实现 Store**

创建 projectWorkspace.ts：

~~~typescript
import { create } from "zustand";
import { persist } from "zustand/middleware";

interface ProjectLike {
  project_id: string;
}

interface ProjectWorkspaceState {
  activeProjectId: string | null;
  setActiveProjectId: (projectId: string | null) => void;
}

export function resolveActiveProjectId(
  current: string | null,
  projects: ProjectLike[],
): string | null {
  if (current && projects.some((project) => project.project_id === current)) {
    return current;
  }
  return projects[0]?.project_id ?? null;
}

export const useProjectWorkspaceStore = create<ProjectWorkspaceState>()(
  persist(
    (set) => ({
      activeProjectId: null,
      setActiveProjectId: (activeProjectId) => set({ activeProjectId }),
    }),
    {
      name: "ai-art-project-workspace",
      partialize: (state) => ({ activeProjectId: state.activeProjectId }),
    },
  ),
);
~~~

- [x] **Step 5: 实现项目工作区面板**

组件 Props 固定为：

~~~typescript
interface ProjectWorkspaceCardProps {
  projects: ProjectConfig[];
  activeProject: ProjectConfig | null;
  activity?: ProjectActivitySummary;
  onSelect: (projectId: string) => void;
}
~~~

创建表单发送：

~~~typescript
createProject.mutate(
  {
    project_id: createId.trim(),
    display_name: createName.trim(),
    language: createLanguage,
  },
  {
    onSuccess: (project) => {
      onSelect(project.project_id);
      setCreateId("");
      setCreateName("");
    },
  },
);
~~~

编辑表单从 activeProject 深拷贝为 draft，完整渲染 models、generation 和 review 字段；保存时调用 updateProject.mutate。页面明确显示 visual_type 为固定武侠预设，项目 ID 不可编辑。活动摘要显示 reference_count 和六类 task_count。

- [x] **Step 6: 运行组件测试**

Run:

~~~powershell
pnpm --dir frontend test -- src/stores/projectWorkspace.test.ts src/components/ProjectWorkspaceCard.test.tsx
pnpm --dir frontend typecheck
~~~

Expected: PASS。

- [x] **Step 7: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/src/stores/projectWorkspace.ts Tools/AiArtAgentPlatform/frontend/src/stores/projectWorkspace.test.ts Tools/AiArtAgentPlatform/frontend/src/components/ProjectWorkspaceCard.tsx Tools/AiArtAgentPlatform/frontend/src/components/ProjectWorkspaceCard.test.tsx
git commit -m "feat: manage active art projects"
~~~

---

### Task 7: 完整风格圣经编辑器

**Files:**

- Create: Tools/AiArtAgentPlatform/frontend/src/components/StyleGuideEditor.tsx
- Create: Tools/AiArtAgentPlatform/frontend/src/components/StyleGuideEditor.test.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/StylePackCard.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/StylePackCard.test.tsx

- [x] **Step 1: 写入失败测试**

创建 StyleGuideEditor.test.tsx，传入完整 guide，验证：

~~~typescript
fireEvent.change(screen.getByLabelText("风格名称"), {
  target: { value: "新版水墨武侠" },
});
fireEvent.change(screen.getByLabelText("基础色（每行一项）"), {
  target: { value: "rice_paper\nink_gray\nmoss_green" },
});
fireEvent.change(screen.getByLabelText("最小俯视角"), {
  target: { value: "60" },
});
fireEvent.change(screen.getByLabelText("最大俯视角"), {
  target: { value: "40" },
});
fireEvent.click(screen.getByRole("button", { name: "保存风格圣经" }));
expect(screen.getByText("最小俯视角不能大于最大俯视角")).toBeInTheDocument();
expect(onSave).not.toHaveBeenCalled();
~~~

修正角度后再次保存，断言 onSave 参数包含三项 palette.base、全部 rendering/readability/ui/forbidden 字段，并保持 style_id 与 mode 不变。

- [x] **Step 2: 运行测试并确认失败**

Run:

~~~powershell
pnpm --dir frontend test -- src/components/StyleGuideEditor.test.tsx
~~~

Expected: FAIL，组件不存在。

- [x] **Step 3: 实现编辑器**

在组件中使用以下纯函数：

~~~typescript
function joinLines(values: string[]): string {
  return values.join("\n");
}

function splitLines(value: string): string[] {
  return value
    .split(/\r?\n/)
    .map((item) => item.trim())
    .filter(Boolean);
}
~~~

组件接收：

~~~typescript
interface StyleGuideEditorProps {
  guide: StyleGuide;
  pending: boolean;
  errorMessage?: string;
  onSave: (guide: StyleGuide) => void;
}
~~~

表单必须完整渲染 display_name、reference_source.path、camera 五个字段、palette 两个列表、rendering 五个字段、readability 四个开关、ui 两个字段和 forbidden 列表。style_id 与 reference_source.mode 使用 disabled 输入。保存前执行角度范围校验，提交时对所有列表调用 splitLines。

- [x] **Step 4: 集成 StylePackCard**

StylePackCard 使用 useUpdateStyleGuideMutation，并在 guide.data 存在时渲染：

~~~tsx
<StyleGuideEditor
  guide={guide.data}
  pending={updateGuide.isPending}
  errorMessage={updateGuide.isError ? "风格圣经保存失败。" : undefined}
  onSave={(nextGuide) => updateGuide.mutate(nextGuide)}
/>
~~~

保留原有提示词编译预览，不在本任务移除。

- [x] **Step 5: 运行测试**

Run:

~~~powershell
pnpm --dir frontend test -- src/components/StyleGuideEditor.test.tsx src/components/StylePackCard.test.tsx
pnpm --dir frontend typecheck
~~~

Expected: PASS。

- [x] **Step 6: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/src/components/StyleGuideEditor.tsx Tools/AiArtAgentPlatform/frontend/src/components/StyleGuideEditor.test.tsx Tools/AiArtAgentPlatform/frontend/src/components/StylePackCard.tsx Tools/AiArtAgentPlatform/frontend/src/components/StylePackCard.test.tsx
git commit -m "feat: edit complete style guide"
~~~

---

### Task 8: 素材源浏览与参考库管理

**Files:**

- Create: Tools/AiArtAgentPlatform/frontend/src/components/ReferenceSourceBrowser.tsx
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ReferenceSourceBrowser.test.tsx
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ReferenceLibrary.tsx
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ReferenceLibrary.test.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/StylePackCard.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/app/styles.css

- [x] **Step 1: 写入素材源浏览器失败测试**

测试 mock /reference-source?query=hero&limit=100，验证只读提示、搜索结果和导入请求：

~~~typescript
fireEvent.change(screen.getByLabelText("搜索素材源"), {
  target: { value: "hero" },
});
fireEvent.click(screen.getByRole("button", { name: "搜索" }));
fireEvent.click(await screen.findByRole("button", { name: /角色\/hero.png/ }));
fireEvent.change(screen.getByLabelText("参考 ID"), {
  target: { value: "hero-main" },
});
fireEvent.change(screen.getByLabelText("身份标签"), {
  target: { value: "hero-main,young-swordsman" },
});
fireEvent.click(screen.getByRole("button", { name: "复制到项目参考库" }));
await waitFor(() =>
  expect(fetchMock).toHaveBeenCalledWith(
    "/api/v1/projects/wuxia-demo/references",
    expect.objectContaining({
      method: "POST",
      body: expect.stringContaining('"source_relative_path":"角色/hero.png"'),
    }),
  ),
);
expect(screen.getByText(/源目录只读/)).toBeInTheDocument();
~~~

- [x] **Step 2: 写入参考库失败测试**

测试 9、10、31 张参考分别显示三种数量提示；测试 category/identity/usage/viewpoint/material 筛选；编辑 hero-main 后 PUT；确认删除后 DELETE；图片 src 包含 /thumbnail?v=<sha256>。

- [x] **Step 3: 运行测试并确认失败**

Run:

~~~powershell
pnpm --dir frontend test -- src/components/ReferenceSourceBrowser.test.tsx src/components/ReferenceLibrary.test.tsx
~~~

Expected: FAIL，组件不存在。

- [x] **Step 4: 实现 ReferenceSourceBrowser**

组件 Props：

~~~typescript
interface ReferenceSourceBrowserProps {
  projectId: string;
}
~~~

使用 useReferenceSourceQuery(projectId, query, limit) 和 useImportReferenceMutation。输入标签使用逗号切分：

~~~typescript
function splitTags(value: string): string[] {
  return value
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}
~~~

搜索结果只提供“选择”操作；导入按钮文案固定为“复制到项目参考库”；不得渲染任何移动、重命名或删除源文件操作。

- [x] **Step 5: 实现 ReferenceLibrary**

组件使用本地 filter state 调用 useReferencesQuery。数量提示函数：

~~~typescript
function coverageMessage(count: number): string {
  if (count < 10) return "风格覆盖不足：建议至少导入 10 张参考图";
  if (count <= 30) return "参考数量处于推荐范围（10–30 张）";
  return "参考数量超过 30 张：建议精简重复参考";
}
~~~

每张卡片渲染缩略图、ID、尺寸、类别和五类标签。编辑表单使用 useUpdateReferenceMutation；删除前使用 window.confirm("只移除项目副本，不会删除只读源文件。")，确认后调用 useDeleteReferenceMutation。

- [x] **Step 6: 集成并添加样式**

StylePackCard 依次渲染 StyleGuideEditor、ReferenceSourceBrowser、ReferenceLibrary 和原有提示词预览。styles.css 增加 source-browser、reference-grid、reference-card、reference-filters 的响应式网格；缩略图使用 object-fit: contain，透明棋盘背景，最小可点击区域 44px。

- [x] **Step 7: 运行测试**

Run:

~~~powershell
pnpm --dir frontend test -- src/components/ReferenceSourceBrowser.test.tsx src/components/ReferenceLibrary.test.tsx src/components/StylePackCard.test.tsx
pnpm --dir frontend typecheck
~~~

Expected: PASS。

- [x] **Step 8: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/src/components/ReferenceSourceBrowser.tsx Tools/AiArtAgentPlatform/frontend/src/components/ReferenceSourceBrowser.test.tsx Tools/AiArtAgentPlatform/frontend/src/components/ReferenceLibrary.tsx Tools/AiArtAgentPlatform/frontend/src/components/ReferenceLibrary.test.tsx Tools/AiArtAgentPlatform/frontend/src/components/StylePackCard.tsx Tools/AiArtAgentPlatform/frontend/src/app/styles.css
git commit -m "feat: browse and manage art references"
~~~

---

### Task 9: 静态资产参考选择与历史恢复

**Files:**

- Create: Tools/AiArtAgentPlatform/frontend/src/components/ReferencePicker.tsx
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ReferencePicker.test.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/stores/production.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/ProductionCard.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/ProductionCard.test.tsx

- [x] **Step 1: 写入 ReferencePicker 失败测试**

创建四个已选和一个未选参考，断言第五个 disabled；取消一个后第五个可选：

~~~typescript
expect(screen.getByLabelText("选择 ref-5")).toBeDisabled();
fireEvent.click(screen.getByLabelText("选择 ref-1"));
expect(screen.getByLabelText("选择 ref-5")).not.toBeDisabled();
fireEvent.click(screen.getByLabelText("选择 ref-5"));
expect(onChange).toHaveBeenLastCalledWith(["ref-2", "ref-3", "ref-4", "ref-5"]);
~~~

- [x] **Step 2: 扩展 ProductionCard 测试**

参考 API 返回 item-ref 和 scene-ref。选择 item-ref 后创建资产，断言 POST body：

~~~typescript
expect(String(createCall?.[1]?.body)).toContain(
  '"reference_ids":["item-ref"]',
);
~~~

增加两个资产和两个 run fixture，使用“已有资产”和“运行记录”下拉框切换，断言当前资产与候选随选择变化。

- [x] **Step 3: 运行测试并确认失败**

Run:

~~~powershell
pnpm --dir frontend test -- src/components/ReferencePicker.test.tsx src/components/ProductionCard.test.tsx
~~~

Expected: FAIL。

- [x] **Step 4: 实现 ReferencePicker**

Props：

~~~typescript
interface ReferencePickerProps {
  references: ReferenceAsset[];
  selectedIds: string[];
  maxSelected?: number;
  onChange: (referenceIds: string[]) => void;
}
~~~

切换函数：

~~~typescript
const toggle = (referenceId: string) => {
  if (selectedIds.includes(referenceId)) {
    onChange(selectedIds.filter((item) => item !== referenceId));
    return;
  }
  if (selectedIds.length < maxSelected) {
    onChange([...selectedIds, referenceId]);
  }
};
~~~

- [x] **Step 5: 扩展生产草稿 Store**

增加 referenceIds、setReferenceIds 和 reset，并在类别变化时清空 referenceIds。默认值为 []。

- [x] **Step 6: 修改 ProductionCard**

调用 useReferencesQuery(projectId, { category: draft.category, limit: 100 })，渲染 ReferencePicker。创建任务时使用：

~~~typescript
reference_ids: draft.referenceIds,
style_pack: "wuxia-ink-chibi-topdown-2_5d",
~~~

增加“已有资产”与“运行记录”select。项目变化 effect 必须执行：

~~~typescript
useEffect(() => {
  setAsset(null);
  setRun(null);
  setPrompt("");
  setEditInstruction("");
  setAcceptStyleRisk(false);
  setExportVariant("default");
  draft.reset();
}, [projectId]);
~~~

选择历史资产后清空当前 run；runs 查询完成后选择目标 run 或第一项。任务尚无 run 时允许 PUT 更新 reference_ids；已有 run 时参考选择只读并提示“该任务已进入生产，参考上下文已锁定”。

- [x] **Step 7: 运行测试**

Run:

~~~powershell
pnpm --dir frontend test -- src/components/ReferencePicker.test.tsx src/components/ProductionCard.test.tsx
pnpm --dir frontend typecheck
~~~

Expected: PASS。

- [x] **Step 8: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/src/components/ReferencePicker.tsx Tools/AiArtAgentPlatform/frontend/src/components/ReferencePicker.test.tsx Tools/AiArtAgentPlatform/frontend/src/stores/production.ts Tools/AiArtAgentPlatform/frontend/src/components/ProductionCard.tsx Tools/AiArtAgentPlatform/frontend/src/components/ProductionCard.test.tsx
git commit -m "feat: select references for static assets"
~~~

---

### Task 10: 六类活动导航与序列任务恢复

**Files:**

- Create: Tools/AiArtAgentPlatform/frontend/src/stores/taskNavigation.ts
- Create: Tools/AiArtAgentPlatform/frontend/src/stores/taskNavigation.test.ts
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ProjectActivityCard.tsx
- Create: Tools/AiArtAgentPlatform/frontend/src/components/ProjectActivityCard.test.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/api/sequences.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/api/sequences.test.ts
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/ProductionCard.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/SequenceCard.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/components/SequenceCard.test.tsx

- [x] **Step 1: 写入导航 Store 与活动卡片失败测试**

目标类型：

~~~typescript
export interface TaskNavigationTarget {
  projectId: string;
  workflow: "static" | "sequence";
  category: AssetCategory;
  assetId: string;
  runId: string | null;
}
~~~

测试点击静态和序列项分别调用 requestOpen，并确保六个分类标题总是存在，即使 recent 为空。

- [x] **Step 2: 写入序列恢复失败测试**

mock GET /sequences/effect/sword-flash/runs/run-effect，先向 Store 写入 sequence 目标，渲染 SequenceCard，断言“剑光”、run-effect 预览和恢复后的 draft.assetId。

- [x] **Step 3: 运行测试并确认失败**

Run:

~~~powershell
pnpm --dir frontend test -- src/stores/taskNavigation.test.ts src/components/ProjectActivityCard.test.tsx src/api/sequences.test.ts src/components/SequenceCard.test.tsx
~~~

Expected: FAIL。

- [x] **Step 4: 实现导航 Store 与活动卡片**

Store：

~~~typescript
interface TaskNavigationState {
  target: TaskNavigationTarget | null;
  requestOpen: (target: TaskNavigationTarget) => void;
  clear: () => void;
}

export const useTaskNavigationStore = create<TaskNavigationState>((set) => ({
  target: null,
  requestOpen: (target) => set({ target }),
  clear: () => set({ target: null }),
}));
~~~

ProjectActivityCard 使用固定中文标签映射，点击后调用 requestOpen，再调用：

~~~typescript
document
  .getElementById(item.workflow === "static" ? "static-production" : "sequence-production")
  ?.scrollIntoView({ behavior: "smooth", block: "start" });
~~~

- [x] **Step 5: 增加单个序列运行 API**

在 sequences.ts 增加 fetchSequenceRun 与 useSequenceRunQuery，Query Key 为：

~~~typescript
["sequence-run", projectId, category, assetId, runId]
~~~

仅在四个标识都存在时 enabled。

- [x] **Step 6: 消费导航目标**

ProductionCard 监听 workflow=static 且 projectId 匹配的目标，从 assets.data 找到 asset；runs.data 到达后选 runId；完成后 clear。

SequenceCard 调用 useSequenceRunQuery 读取目标；数据到达时：

~~~typescript
setRun(openedRun.data);
setPrompt(openedRun.data.prompt);
draft.restoreTask(openedRun.data.task);
navigation.clear();
~~~

两个生产 section 分别设置 id="static-production" 和 id="sequence-production"。项目切换时清除不匹配的导航目标。

- [x] **Step 7: 运行测试**

Run:

~~~powershell
pnpm --dir frontend test -- src/stores/taskNavigation.test.ts src/components/ProjectActivityCard.test.tsx src/api/sequences.test.ts src/components/ProductionCard.test.tsx src/components/SequenceCard.test.tsx
pnpm --dir frontend typecheck
~~~

Expected: PASS。

- [x] **Step 8: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/src/stores/taskNavigation.ts Tools/AiArtAgentPlatform/frontend/src/stores/taskNavigation.test.ts Tools/AiArtAgentPlatform/frontend/src/components/ProjectActivityCard.tsx Tools/AiArtAgentPlatform/frontend/src/components/ProjectActivityCard.test.tsx Tools/AiArtAgentPlatform/frontend/src/api/sequences.ts Tools/AiArtAgentPlatform/frontend/src/api/sequences.test.ts Tools/AiArtAgentPlatform/frontend/src/components/ProductionCard.tsx Tools/AiArtAgentPlatform/frontend/src/components/SequenceCard.tsx Tools/AiArtAgentPlatform/frontend/src/components/SequenceCard.test.tsx
git commit -m "feat: reopen recent art tasks"
~~~

---

### Task 11: 应用级集成与项目隔离

**Files:**

- Modify: Tools/AiArtAgentPlatform/frontend/src/app/App.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/app/App.test.tsx
- Modify: Tools/AiArtAgentPlatform/frontend/src/app/styles.css

- [x] **Step 1: 写入 App 失败测试**

在 App.test.tsx mock 两个项目和各自 activity。设置 localStorage activeProjectId=project-b，渲染后断言 project-b 被选中；切换 project-a 后断言风格、参考、资产和 activity 请求均使用 project-a。再模拟 localStorage 保存 missing，断言回退 project-a。

关键断言：

~~~typescript
expect(await screen.findByLabelText("当前项目")).toHaveValue("project-b");
fireEvent.change(screen.getByLabelText("当前项目"), {
  target: { value: "project-a" },
});
await waitFor(() =>
  expect(fetchMock.mock.calls.some(([path]) =>
    String(path).includes("/projects/project-a/activity"),
  )).toBe(true),
);
~~~

- [x] **Step 2: 运行测试并确认失败**

Run:

~~~powershell
pnpm --dir frontend test -- src/app/App.test.tsx
~~~

Expected: FAIL，App 仍固定 projects.data[0]。

- [x] **Step 3: 集成当前项目**

App 读取 activeProjectId Store，并使用 effect：

~~~typescript
useEffect(() => {
  if (!projects.data) return;
  const resolved = resolveActiveProjectId(activeProjectId, projects.data);
  if (resolved !== activeProjectId) {
    setActiveProjectId(resolved);
  }
}, [activeProjectId, projects.data, setActiveProjectId]);
~~~

通过 project_id 查找 activeProject，调用 useProjectActivityQuery。替换旧的“项目与任务”卡片为：

~~~tsx
<ProjectWorkspaceCard
  projects={projects.data ?? []}
  activeProject={activeProject ?? null}
  activity={activity.data}
  onSelect={setActiveProjectId}
/>
<ProjectActivityCard
  projectId={activeProject?.project_id}
  activity={activity.data}
/>
~~~

所有其他卡片继续传入 activeProject?.project_id。

- [x] **Step 4: 更新布局样式**

为项目管理、活动六列、风格编辑、素材浏览和参考库增加桌面双列/移动端单列规则。生产区保持整行宽度，避免参考管理压缩候选画布。不得改变现有武侠宣纸配色变量。

- [x] **Step 5: 运行前端全量测试**

Run:

~~~powershell
pnpm --dir frontend test
pnpm --dir frontend typecheck
pnpm --dir frontend build
~~~

Expected: 全部 PASS。

- [x] **Step 6: 提交**

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/src/app/App.tsx Tools/AiArtAgentPlatform/frontend/src/app/App.test.tsx Tools/AiArtAgentPlatform/frontend/src/app/styles.css
git commit -m "feat: integrate project style workspace"
~~~

---

### Task 12: 离线端到端验收、文档与全量门禁

**Files:**

- Create: Tools/AiArtAgentPlatform/frontend/e2e/project-style-management.spec.ts
- Modify: Tools/AiArtAgentPlatform/frontend/e2e/production.spec.ts
- Modify: Tools/AiArtAgentPlatform/README.md
- Modify: README.md
- Modify: plans/plan-ai-art-agent-platform.md
- Modify: plans/plan-ai-art-agent-platform-project-style-management.md
- Modify: plans/plan-ai-art-agent-platform-project-style-management-implementation.md

- [x] **Step 1: 写入离线 Playwright 场景**

新用例拦截全部 /api/v1 请求，以内存对象保存 projects、styleGuide、references、assets 和 sequenceRuns。流程必须执行：

1. 创建 project-a 与 project-b。
2. 切换 project-b，刷新并确认仍是 project-b。
3. 编辑项目显示名称和风格圣经。
4. 搜索素材源并导入四张参考。
5. 编辑其中一张参考的材质标签。
6. 选择四张参考创建 item 任务，断言 request body reference_ids 长度为 4。
7. 从活动卡打开该 item。
8. 从活动卡打开 effect 序列。
9. 记录所有请求路径，断言不存在 /plan、/generate、/review 或 /edit 模型操作。

测试内的 PNG 缩略图使用已有 1×1 PNG Buffer，不访问磁盘外部素材。

- [x] **Step 2: 运行 Playwright 并修复 fixture**

Run:

~~~powershell
pnpm --dir frontend e2e -- project-style-management.spec.ts production.spec.ts
~~~

Expected: 2 passed。

- [x] **Step 3: 更新文档与计划轨迹**

Tools/AiArtAgentPlatform/README.md 和根 README.md 更新为：

- 首页可创建、选择、切换和编辑项目。
- 风格圣经可完整编辑。
- 参考源保持只读，项目参考支持缩略图和标签维护。
- 静态任务可选择 0–4 张参考。
- 六类最近任务可重新打开。
- 共享 Schema 数量从 9 改为 10。

在主计划和方案 A 规格的变更记录追加 2026-07-28 记录，列出实际文件、实现内容和最终验证结果。实施计划中把全部完成的 checkbox 改为 [x]，并追加变更记录。

- [x] **Step 4: 执行后端全量门禁**

Run:

~~~powershell
python -m pytest backend/tests -v
python -m ruff check backend
python -m mypy backend/app
~~~

Expected: 全部 PASS，测试数不少于实施前的 134 项。

- [x] **Step 5: 执行前端全量门禁**

Run:

~~~powershell
pnpm --dir frontend test
pnpm --dir frontend typecheck
pnpm --dir frontend build
pnpm --dir frontend e2e
~~~

Expected: 全部 PASS，Playwright 至少 2 项。

- [x] **Step 6: 验证 Schema 稳定**

Run:

~~~powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-schemas.ps1
Get-FileHash shared/schemas/*.json | Sort-Object Path
powershell -ExecutionPolicy Bypass -File scripts/generate-schemas.ps1
Get-FileHash shared/schemas/*.json | Sort-Object Path
~~~

Expected: 两次 10 个 Schema 的 SHA256 完全一致。

- [x] **Step 7: 执行无密钥启动冒烟**

Run:

~~~powershell
powershell -ExecutionPolicy Bypass -File scripts/check-environment.ps1
powershell -ExecutionPolicy Bypass -File scripts/start.ps1 -SmokeTest
~~~

Expected: 健康检查成功，127.0.0.1:5173 与 127.0.0.1:8765 在脚本退出后释放，不产生 OpenAI 请求。

- [x] **Step 8: 验证只读素材未变化**

对 shared/presets/wuxia-ink-chibi-topdown-2_5d/style-guide.yaml 中 reference_source.path 指向的源目录执行项目既有 Pilot 哈希检查，比较阶段 9 保存的 28 个源文件记录。

Run:

~~~powershell
python -m pytest backend/tests/test_offline_pilot.py -v
~~~

Expected: PASS；源素材哈希与阶段 9 记录一致。

- [x] **Step 9: 最终提交**

先确认暂存区只包含方案 A 文件：

~~~powershell
git diff --cached --name-status
~~~

再逐文件提交本任务尚未提交的验收与文档文件：

~~~powershell
git add Tools/AiArtAgentPlatform/frontend/e2e/project-style-management.spec.ts Tools/AiArtAgentPlatform/frontend/e2e/production.spec.ts Tools/AiArtAgentPlatform/README.md README.md plans/plan-ai-art-agent-platform.md plans/plan-ai-art-agent-platform-project-style-management.md plans/plan-ai-art-agent-platform-project-style-management-implementation.md
git commit -m "feat: complete project style management"
~~~

不得使用 git add Tools/AiArtAgentPlatform 这类目录级暂存；不得纳入 Pilot 产物、Unity 资源、场景、Prefab 或用户已有的其他工作区改动。

---

## 实施检查点

- Task 1–4 完成后：后端项目活动与参考管理 API 可独立验收。
- Task 5–8 完成后：项目与风格包界面可独立验收。
- Task 9–11 完成后：静态参考选择和最近任务恢复可独立验收。
- Task 12 完成后：方案 A 的全部离线验收证据齐备。

## 变更记录

### 2026-07-28 - 创建测试先行实施计划

- **修改文件**：plans/plan-ai-art-agent-platform-project-style-management-implementation.md
- **变更内容**：将方案 A 拆分为 12 个可独立验证和提交的 TDD 任务，覆盖后端参考管理、活动聚合、前端项目/风格包管理、任务恢复、离线端到端验收与全量门禁。
- **关联说明**：实施阶段必须使用 superpowers:subagent-driven-development 或 superpowers:executing-plans；本计划不授权 Unity MCP、Unity Editor 或真实 OpenAI 调用。

### 2026-07-28 - 完成 12 项测试先行实施任务

- **修改文件**：后端活动/参考 Schema、服务、API、测试和 `project-activity.schema.json`；前端项目、风格、参考、生产、活动导航、序列恢复、App 集成、Vitest 与 Playwright；`Tools/AiArtAgentPlatform/README.md`、根 `README.md` 和方案 A 计划文件。
- **变更内容**：完成 Task 1–12，并逐任务提交后端参考管理与活动聚合、前端项目/风格包闭环、静态参考选择、六类最近任务恢复、应用级项目隔离和离线端到端验收。
- **验证结果**：后端 `137 passed`、Ruff/Mypy 通过；前端 `70 passed / 28 files`、类型检查、Vite 构建和 Playwright `2 passed`；10 份 Schema 两次生成哈希一致；离线 Pilot `5 passed`；环境检查和无密钥启动冒烟通过，端口已释放。
- **关联说明**：未调用真实 OpenAI、Unity MCP、Unity Editor 或 Play Mode，未修改只读素材、Unity 资源和三条核心时间规则。方案 A 已完成，但方案 B、C 尚未实施。
