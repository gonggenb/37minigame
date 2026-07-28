# AI 美术智能体平台：项目与风格包管理闭环

## 需求描述

在现有 `Tools/AiArtAgentPlatform/` 本地工作台基础上，完成方案 A“项目与风格包管理闭环”。用户需要能够在界面中创建、选择、切换和编辑本地项目，完整维护武侠风格圣经，搜索只读参考素材源，管理已导入参考图的缩略图与标签，并在创建静态资产任务时明确选择不超过 4 张参考图。同时，首页需要按角色、场景、物品、动画、特效和 UI 六类资产显示最近任务，并允许重新打开已有任务。

本阶段只完善本地项目管理、风格包管理和任务导航，不调用 OpenAI 模型，不调用 Unity MCP，不修改 Unity Scene、Prefab、GameObject、Component 或 Inspector，不进入 Play Mode。

## 实现方案

### 1. 范围与边界

本阶段包含：

- 项目创建、选择、切换和安全字段编辑。
- 当前项目持久化，刷新页面后恢复上次选择。
- 项目活动摘要和六类资产最近任务入口。
- 完整风格圣经编辑与校验。
- 只读素材源的路径搜索和文件选择。
- 已导入参考图的缩略图、标签、筛选、数量反馈、更新和移除。
- 静态资产任务的参考图选择，并写入现有 `AssetTask.reference_ids`。
- 已有静态资产和动画/特效任务的重新打开。
- 后端、前端和离线端到端测试。

本阶段不包含：

- 动画/特效的结构化 LLM 规划、视觉评审和自动修复；该能力属于后续方案 B。
- 四候选人工采用记录、70% 采用率和 Unity 人工验收统计；该能力属于后续方案 C。
- 删除项目、复制项目、多人协作、数据库、云端同步或权限系统。
- 真实 OpenAI 生成、评审或费用消耗。
- Unity 自动导入、Editor 配置或运行态测试。

### 2. 核心设计决策

#### 2.1 项目 ID 与视觉预设保持稳定

- `project_id` 创建后不可修改，继续使用小写英文、数字和短横线格式。
- V1 创建项目时固定使用 `wuxia-ink-chibi-topdown-2_5d` 视觉预设，避免切换 `visual_type` 后与已经落盘的 `style-guide.yaml` 不一致。
- 项目编辑允许修改显示名称、语言、模型名称、候选数量、自动修复次数、图片质量、透明策略和评审阈值等安全字段。
- 后端继续使用 `ProjectConfig` 和原子 YAML 写入，不引入数据库。

#### 2.2 只读素材源与项目参考库严格分离

- `StyleGuide.reference_source.path` 指向用户提供的绝对素材目录，`mode` 永远为 `read_only`。
- 搜索素材源只读取文件名、相对路径和文件大小，不移动、不重命名、不写入源目录。
- 导入参考图时继续把源文件复制到项目工作区，并在项目工作区生成缩略图。
- 编辑标签只更新项目内 `reference-index.json`；删除参考只删除项目工作区副本与缩略图，不删除源文件。
- 缩略图接口只读取已登记参考的项目内缩略图，不接受任意文件路径。

#### 2.3 最近任务使用统一活动摘要，不改变生产状态机

- 新增只读活动聚合服务，组合现有静态资产记录、静态生产运行记录和动画/特效序列运行记录。
- 活动摘要按六个 `AssetCategory` 固定返回，每类包含任务总数和最近更新项。
- 静态资产以资产记录为入口，附带最近一次 `ProductionRun`；没有运行记录时状态为 `draft`。
- 动画和特效以 `SequenceRun` 为入口，按 `asset_id` 与 `run_id` 打开。
- 本阶段不把同步模型操作迁移到后台任务队列，也不改变现有运行文件格式。

#### 2.4 参考选择显式写入任务

- 静态资产表单按当前类别展示已导入参考库。
- 用户可选择 0–4 张参考图；超出 4 张时前端禁止继续选择，后端继续由 `AssetTask.reference_ids` 的长度约束兜底。
- 创建任务时将选择结果原样写入 `reference_ids`，不再固定发送空数组。
- 结构化规划和提示词编译继续使用现有 `ReferenceSelector` 与 `PromptCompiler`，并以任务中的显式选择作为优先上下文。

### 3. 后端设计

#### 3.1 项目活动 Schema

新增 `backend/app/schemas/activity.py`：

```python
class ProjectActivityItem(StrictModel):
    workflow: Literal["static", "sequence"]
    category: AssetCategory
    asset_id: str
    name: str
    status: str
    run_id: str | None = None
    updated_at: datetime


class ProjectCategoryActivity(StrictModel):
    category: AssetCategory
    task_count: int = Field(ge=0)
    recent: list[ProjectActivityItem] = Field(default_factory=list, max_length=5)


class ProjectActivitySummary(StrictModel):
    schema_version: Literal[1] = 1
    project_id: str
    reference_count: int = Field(ge=0)
    categories: list[ProjectCategoryActivity] = Field(min_length=6, max_length=6)
```

`categories` 固定按 `character`、`scene`、`item`、`animation`、`effect`、`ui` 返回，前端不依赖字典遍历顺序。

#### 3.2 项目活动服务

新增 `backend/app/workspace/project_activity.py`，职责仅限读取和聚合：

- 验证项目存在。
- 读取 `ReferenceCatalog` 的已导入参考数量。
- 读取 `ProductionWorkspace.list_assets(project_id)`，并为每个静态资产取最近运行记录。
- 为 `SequenceProductionService` 增加 `list_project_runs(project_id)`，安全扫描动画和特效目录下的 `run.json`，跳过损坏或身份不匹配的记录。
- 按更新时间倒序截取每类最近 5 项。
- 不修改任务、运行记录或素材文件。

新增接口：

```text
GET /api/v1/projects/{projectId}/activity
```

项目不存在返回 `404`；局部损坏的历史记录被跳过，避免一个坏文件阻断整个首页。

#### 3.3 参考标签更新

在 `backend/app/schemas/style_pack.py` 新增：

```python
class ReferenceUpdateRequest(StrictModel):
    categories: list[AssetCategory] = Field(min_length=1)
    identities: list[str] = Field(default_factory=list)
    usages: list[str] = Field(default_factory=list)
    viewpoints: list[str] = Field(default_factory=list)
    materials: list[str] = Field(default_factory=list)
    notes: str = Field(default="", max_length=2000)
```

在 `ReferenceCatalog` 增加：

- `update_reference(project_id, reference_id, request)`：只替换可编辑标签和备注，保留源相对路径、工作区路径、缩略图路径、哈希和尺寸。
- `read_thumbnail(project_id, reference_id)`：通过索引定位缩略图，并使用工作区路径保护读取 PNG 字节。
- `ReferenceFilters.material`：补齐材质筛选。

新增接口：

```text
PUT /api/v1/projects/{projectId}/references/{referenceId}
GET /api/v1/projects/{projectId}/references/{referenceId}/thumbnail
```

更新不存在的参考返回 `404`；空类别、非法路径或数据不符合 Schema 返回 `422`；缩略图接口返回 `image/png`。

#### 3.4 现有接口复用

以下接口已经存在，本阶段只补齐前端调用和测试：

```text
POST /api/v1/projects
GET  /api/v1/projects
GET  /api/v1/projects/{projectId}
PUT  /api/v1/projects/{projectId}
GET  /api/v1/projects/{projectId}/style-guide
PUT  /api/v1/projects/{projectId}/style-guide
GET  /api/v1/projects/{projectId}/reference-source?query=...&limit=...
POST /api/v1/projects/{projectId}/references
GET  /api/v1/projects/{projectId}/references
DELETE /api/v1/projects/{projectId}/references/{referenceId}
```

`GET /references` 在保留现有 `category`、`identity`、`usage`、`viewpoint` 参数的基础上增加 `material` 参数。

### 4. 前端设计

#### 4.1 当前项目状态

新增 `frontend/src/stores/projectWorkspace.ts`，使用 Zustand `persist` 中间件只保存：

```ts
interface ProjectWorkspaceState {
  activeProjectId: string | null;
  setActiveProjectId(projectId: string | null): void;
}
```

恢复规则：

1. 如果本地保存的项目 ID 仍存在，继续使用。
2. 如果不存在，选择项目列表第一项并覆盖失效 ID。
3. 如果没有项目，设为 `null`。
4. 新建项目成功后立即切换到新项目。

项目切换时，生产卡片和序列卡片以 `projectId` 为边界重置当前局部状态，禁止把前一个项目的资产或运行记录显示到新项目。

#### 4.2 项目工作区面板

新增 `ProjectWorkspaceCard.tsx`：

- 项目下拉选择器。
- “新建项目”和“编辑当前项目”表单。
- 创建字段：项目 ID、显示名称和语言；视觉预设固定显示为武侠水墨预设。
- 编辑字段：显示名称、语言、模型、生成和评审设置；项目 ID 与视觉预设只读。
- 显示字段级校验、重复项目 `409`、保存失败和保存成功状态。
- 显示参考数量与六类任务数量摘要。

项目 API 在 `frontend/src/api/projects.ts` 中补齐创建、读取、更新和活动摘要查询/Mutation，并在成功后精确失效相关 React Query 缓存。

#### 4.3 完整风格圣经编辑器

将现有只读摘要扩展为 `StyleGuideEditor.tsx`，覆盖 `StyleGuide` 的全部可编辑字段：

- 显示名称和只读素材源绝对路径。
- 相机投影、俯视语义最小/最大角度、共享视角和默认朝向。
- 基础色与强调色。
- 角色比例、轮廓、环境细节、表面质感和阴影方向。
- 四项可读性开关。
- UI 正式文字烘焙开关与边框语言。
- 禁用项列表。

`style_id` 和 `reference_source.mode` 只读。列表字段使用逐行输入并在保存时去除空行和首尾空格。相机最小角度不得大于最大角度。保存成功后刷新风格圣经、素材源和提示词相关缓存。

#### 4.4 只读素材源浏览

新增 `ReferenceSourceBrowser.tsx`：

- 调用现有 `/reference-source` 接口按相对路径搜索。
- 默认最多显示 100 项，可主动加载到接口上限 500 项。
- 选中文件后自动填入导入表单的 `source_relative_path`。
- 导入前填写参考 ID、类别、身份、用途、视角、材质和备注。
- 明确显示“源目录只读，导入会复制到项目工作区”的提示。
- 不提供移动、重命名、覆盖或删除源文件的按钮。

#### 4.5 参考库缩略图与标签管理

新增 `ReferenceLibrary.tsx` 和可复用的 `ReferencePicker.tsx`：

- 参考库使用缩略图网格显示图片、ID、尺寸、类别和标签摘要。
- 缩略图 URL 使用新接口，并附加参考哈希作为缓存版本。
- 支持类别、身份、用途、视角和材质筛选。
- 允许编辑身份、用途、视角、材质、类别和备注。
- 允许移除项目参考；确认文案明确说明不会删除源文件。
- 数量少于 10 张时提示“风格覆盖不足”，10–30 张显示“推荐范围”，超过 30 张提示精简重复参考。
- `ReferencePicker` 以受控组件形式接收类别、候选参考和已选 ID，统一执行最多 4 张的限制。

#### 4.6 静态资产参考选择与历史任务打开

修改 `ProductionCard.tsx`：

- 增加已有静态资产和运行记录选择器，不再只自动打开列表第一项。
- 创建表单中嵌入 `ReferencePicker`，按静态资产类别筛选参考。
- 创建任务时将所选 ID 写入 `reference_ids`。
- 打开历史资产时显示其已保存参考，并允许在资产尚未规划前通过现有更新接口调整任务。
- 项目切换后清空当前资产、运行记录、提示词、编辑指令和导出选项。

修改 `SequenceCard.tsx`：

- 接收统一任务导航目标。
- 从活动摘要点击动画或特效任务时，根据 `category`、`asset_id` 和 `run_id` 恢复任务与预览状态。
- 本阶段不改变动画/特效生成、处理、漂移检查和导出逻辑。

#### 4.7 六类最近任务导航

新增 `frontend/src/stores/taskNavigation.ts` 和 `ProjectActivityCard.tsx`：

- 活动卡片固定显示六类资产。
- 每类显示总数、最近 5 项、状态和更新时间。
- 点击静态任务时设置 `workflow="static"` 导航目标并滚动到静态生产区。
- 点击动画/特效时设置 `workflow="sequence"` 导航目标并滚动到序列生产区。
- `ProductionCard` 和 `SequenceCard` 消费目标后恢复对应任务，再清除一次性导航请求。

### 5. 数据流

#### 5.1 项目切换

```text
读取项目列表
→ 校验 localStorage.activeProjectId
→ 得到当前项目
→ 并行读取项目活动、风格圣经、参考库、资产、序列和约束
→ 所有 Query Key 均包含 projectId
```

#### 5.2 参考导入与编辑

```text
搜索只读素材源
→ 选择相对路径并填写标签
→ 后端校验源路径位于只读根目录
→ 复制原图到项目工作区
→ 生成 256×256 以内缩略图
→ 原子更新 reference-index.json
→ 前端刷新参考库与项目活动
```

标签编辑只执行最后一步，不重新复制图片，也不改变原图哈希。

#### 5.3 静态任务创建

```text
选择资产类别
→ 参考库按类别筛选
→ 用户选择 0–4 张参考图
→ 创建 AssetTask(reference_ids=[...])
→ 规划阶段读取显式参考
→ 后续生成、评审和导出沿用现有静态生产闭环
```

### 6. 错误处理

- 项目 ID 重复：前端显示“项目 ID 已存在”，不清空用户输入。
- 本地保存的项目 ID 失效：自动回退到第一个有效项目。
- 风格圣经校验失败：保留编辑草稿并显示后端 `422` 详情。
- 素材源目录不存在：素材源浏览器显示路径错误，但风格圣经其他字段仍可编辑。
- 参考源文件不存在或格式不支持：导入失败且不写入索引。
- 缩略图缺失：单张卡片显示占位图，不阻断其他参考加载。
- 标签更新冲突或参考已删除：刷新参考库并提示记录已变化。
- 活动摘要遇到损坏历史文件：跳过损坏项，返回其他有效任务。
- 项目切换期间：旧项目请求结果通过含 `projectId` 的 Query Key 隔离，不覆盖新项目界面。

### 7. 测试与验收

#### 7.1 后端测试

- 项目创建、更新、重复 ID 和路径/正文 ID 不一致。
- 活动摘要固定返回六类，静态与序列最近任务排序正确。
- 损坏运行记录被跳过，项目不存在返回 `404`。
- 参考标签更新只改变标签字段，原图哈希、路径和尺寸保持不变。
- 缩略图读取返回 PNG，任意路径和不存在 ID 无法读取。
- 材质筛选与现有类别、身份、用途、视角筛选可组合。
- 导入、更新和删除参考前后，只读素材源文件哈希不变。

#### 7.2 前端测试

- 当前项目 Store 的保存、恢复、失效回退和清空。
- 创建项目后自动切换；编辑后刷新缓存。
- 完整风格圣经表单的序列化、校验和错误保留。
- 素材源搜索、导入表单和只读提示。
- 参考缩略图、标签编辑、五类筛选和数量反馈。
- `ReferencePicker` 最多选择 4 张，取消选择后可继续添加。
- `ProductionCard` 创建请求包含准确的 `reference_ids`。
- 六类活动卡片能够打开对应静态或序列任务。
- 切换项目后旧任务状态不残留。

#### 7.3 离线端到端验收

Playwright 覆盖以下无模型流程：

1. 创建两个本地项目。
2. 在两个项目之间切换并刷新页面，确认恢复上次项目。
3. 编辑当前项目和完整风格圣经。
4. 搜索只读素材源，导入参考并编辑标签。
5. 从参考库选择 4 张图片创建静态资产任务。
6. 在六类活动摘要中重新打开静态任务和预置序列任务。
7. 确认测试没有点击任何模型生成或评审按钮。
8. 对测试前后的只读源素材计算哈希，结果必须一致。

#### 7.4 全量回归

实现完成后执行：

- 后端 Pytest 全量测试。
- Ruff 和 Mypy。
- 前端 Vitest、TypeScript 类型检查和 Vite 构建。
- Playwright 离线端到端测试。
- 共享 JSON Schema 重复生成稳定性检查。
- 无 API Key 启动与健康检查。

### 8. 预期修改范围

后端预计新增或修改：

- `backend/app/schemas/activity.py`
- `backend/app/schemas/style_pack.py`
- `backend/app/workspace/project_activity.py`
- `backend/app/production/sequence_service.py`
- `backend/app/style_pack/references.py`
- `backend/app/api/projects.py`
- `backend/app/api/style_pack.py`
- `backend/app/main.py`
- 对应后端测试与共享 Schema。

前端预计新增或修改：

- `frontend/src/api/projects.ts`
- `frontend/src/api/stylePack.ts`
- `frontend/src/types/core.ts`
- `frontend/src/stores/projectWorkspace.ts`
- `frontend/src/stores/taskNavigation.ts`
- `frontend/src/components/ProjectWorkspaceCard.tsx`
- `frontend/src/components/ProjectActivityCard.tsx`
- `frontend/src/components/StyleGuideEditor.tsx`
- `frontend/src/components/ReferenceSourceBrowser.tsx`
- `frontend/src/components/ReferenceLibrary.tsx`
- `frontend/src/components/ReferencePicker.tsx`
- `frontend/src/components/StylePackCard.tsx`
- `frontend/src/components/ProductionCard.tsx`
- `frontend/src/components/SequenceCard.tsx`
- `frontend/src/app/App.tsx`
- `frontend/src/app/styles.css`
- 对应 Vitest 与 Playwright 测试。

### 9. 完成标准

- 用户可完全通过界面创建、选择、切换和编辑本地项目。
- 刷新页面后恢复最后一次有效项目选择。
- 用户可完整编辑并保存武侠风格圣经。
- 用户可搜索只读素材源，导入 10–30 张参考图，并在缩略图网格中维护标签。
- 只读源素材在导入、编辑和删除项目参考后均未被修改。
- 静态资产任务能够保存 0–4 个明确参考 ID，且不再固定为空数组。
- 首页按六类显示最近任务，并能重新打开静态和序列任务。
- 全部离线自动测试通过。
- 本阶段不产生 OpenAI API 用量，不调用 Unity MCP，不修改 Unity 项目运行内容。

## 变更记录

### 2026-07-28 - 固化方案 A 书面设计

- **修改文件**：`plans/plan-ai-art-agent-platform-project-style-management.md`
- **变更内容**：记录项目管理、风格圣经、只读素材源、参考库、任务引用选择和六类最近任务导航的完整设计、接口、数据流、错误处理、测试与完成标准。
- **关联说明**：本文件是完整 V1 缺口整改的第一个子项目；后续动画/特效统一智能体和人工采用率/Unity 验收统计分别另立方案 B、方案 C 计划。

### 2026-07-28 - 完成方案 A 详细实施计划

- **修改文件**：`plans/plan-ai-art-agent-platform-project-style-management-implementation.md`、`plans/plan-ai-art-agent-platform-project-style-management.md`
- **变更内容**：把书面设计拆分为 12 个测试先行任务，明确后端参考管理与活动聚合、前端项目/风格包管理、静态参考选择、最近任务恢复、离线端到端验收和全量门禁。
- **关联说明**：实施计划要求逐文件暂存，保护当前仓库中大量用户既有改动；不授权 Unity MCP、Unity Editor 或真实 OpenAI 调用。
