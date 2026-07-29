# 2D 小游戏 AI 美术生产智能体平台 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在当前 Unity 仓库中建设一套隔离运行的个人本地 2D 游戏美术生产工作台，稳定产出统一 Q 版水墨武侠、2.5D 俯视角风格的规范图片、逐帧 PNG 和 Sprite Sheet。

**Architecture:** 平台位于 `Tools/AiArtAgentPlatform/`，由 React/Vite 前端、FastAPI 本地服务和文件工作区组成，不修改 Unity 场景、Prefab 或玩法代码。智能体采用可追踪的阶段式管线，通过供应商适配器调用 OpenAI，通过确定性图像处理与硬约束验证控制输出质量。

**Tech Stack:** React、Vite、TypeScript、Zustand、TanStack Query、Konva.js、Python 3.12、FastAPI、Pydantic、OpenAI Python SDK、Pillow、OpenCV、NumPy、scikit-image、YAML、JSON/JSONL、pytest、Vitest、Playwright、PowerShell。

---

## 需求描述

构建一套个人使用、本地运行的 2D 小游戏 AI 美术生产智能体平台，覆盖：

- 角色原画与游戏内角色基准帧。
- 2.5D 俯视角场景与分层场景素材。
- 世界内物品 Sprite 与 UI 图标。
- 纯逐帧角色动画。
- 透明逐帧特效。
- UI 图标、按钮、面板和装饰件。

完整生产闭环：

```text
项目风格包
→ 资产需求
→ LLM 结构化规划
→ GPT Image 生成或编辑
→ 确定性图像后处理
→ 硬约束与风格检查
→ 人工选择或定向修复
→ 规范 PNG、逐帧 PNG、Sprite Sheet 导出
```

V1 使用 GPT Image 作为唯一图像生成服务，但所有供应商调用必须通过 `ImageProvider` 与 `ReviewProvider` 接口隔离。平台不建设数据库、多用户、正式审批流、云部署、远程访问、Unity 插件或 Unity 自动导入。

## 已确认决策

### 1. 仓库位置与隔离边界

平台根目录固定为：

```text
Tools/AiArtAgentPlatform/
```

不得把平台的 `frontend/`、`backend/`、`shared/` 和 `data/` 直接放到 Unity 仓库根目录。平台运行时不得写入：

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- Unity Scene、Prefab、GameObject、Component 或 Inspector 绑定

本平台只导出图片。导出结果如需进入 Unity，必须在独立 Editor 工作任务中处理。

### 2. 真实参考素材

只读风格参考路径：

```text
D:\MiniGame\素材\Q版中国风水墨武侠割草游戏仙侠武器音效动画UI图标界面美术素材\Q版中国风水墨武侠割草游戏仙侠武器音效动画UI图标界面美术素材\Q版水墨国风（行侠仗义五千年）
```

审计结果：

| 分类 | 数量 |
| --- | ---: |
| UI 图片 | 862 |
| 动画序列 | 1,057 |
| 场景背景 | 107 |
| 特效序列 | 592 |
| 角色切图 | 99 |
| 角色立绘 | 54 |
| 道具图标 | 445 |
| 音效 | 122 |
| 字体 | 4 |

合计 3,216 张 PNG。参考目录只读，不修改、不重命名、不提交到 Git。平台风格包只保存经用户选择的参考索引、缩略图缓存、标签和必要的工作副本。

### 3. 项目专用美术方向

首个内置预设固定为：

```yaml
visual_type: wuxia-ink-chibi-topdown-2_5d
display_name: Q版水墨武侠俯视角
language: zh-CN
```

核心约束：

- 角色采用 Q 版武侠比例、清晰墨线、简化服饰层次和稳定身份特征。
- 场景采用近似正交的 2.5D 俯视构图，默认俯视角语义为 35°–55°，同组资产保持一致。
- 主色以宣纸米白、墨灰、青灰、苔藓绿、暖土色为主，朱红与暗金作为强调色。
- 场景降低高频细节和对比度，为角色、敌人、掉落物与技能反馈保留可读空间。
- 世界物品、建筑、角色和阴影必须共享观察角度与主光方向。
- UI 使用水墨边缘、宣纸肌理、朱红印章感和暗金点缀，正式文字不得烘焙进图片。
- 不采用像素角色、写实照片地表、低多边形 3D 模型、高反射 PBR、霓虹色或复杂写实光照。

该平台风格预设不直接修改现有 Unity 项目的 `docs/art_style_guide.md`。只有在平台导出资产正式替换 Unity 运行时美术时，才单独调整游戏项目美术规范。

### 4. OpenAI 接口现实约束

根据 2026-07-28 的 OpenAI 官方文档：

- `gpt-image-2` 可通过 Image API 生成和编辑图片。
- Responses API 适合结构化规划、视觉评审和多轮图像编辑。
- `gpt-image-2` 对参考图采用高保真输入处理。
- `gpt-image-2` 当前不支持 `background: "transparent"`。
- 图像尺寸必须符合模型边长、16 像素倍数、宽高比和总像素约束。

因此 `transparent_background: true` 不能表示模型原生透明输出，必须表示平台最终交付要求。透明资产管线固定为：

```text
在纯色或可分离背景上生成
→ 背景分割或移除
→ Alpha 边缘去色与清理
→ RGBA 检查
→ 透明残留检查
→ 规范 PNG 导出
```

官方资料：

- https://developers.openai.com/api/docs/guides/image-generation
- https://developers.openai.com/api/docs/guides/structured-outputs

## 实现方案

### 1. 目录结构

```text
Tools/AiArtAgentPlatform/
  README.md
  .env.example
  pyproject.toml
  package.json

  frontend/
    src/
      api/
      app/
      components/
      features/
        projects/
        style-pack/
        production/
        constraints/
        editor/
        animation-preview/
        review/
        export/
      stores/
      types/
    tests/

  backend/
    app/
      api/
      agent/
      providers/
      pipelines/
        character/
        scene/
        item/
        animation/
        effect/
        ui/
      constraints/
      image_processing/
      quality/
      jobs/
      workspace/
      schemas/
      config/
    tests/

  shared/
    schemas/
    presets/
      wuxia-ink-chibi-topdown-2_5d/

  scripts/
    start.ps1
    check-environment.ps1
    generate-schemas.ps1

  data/
    workspaces/
```

`Tools/AiArtAgentPlatform/data/`、前端依赖、Python 虚拟环境、缓存、测试报告和本地 `.env` 必须排除在 Git 之外。

### 2. 本地工作区协议

```text
data/workspaces/<project-slug>/
  project.yaml
  style-pack/
    style-guide.yaml
    references/
    thumbnails/
    reference-index.json
  constraints/
    common.yaml
    character.yaml
    scene.yaml
    item.yaml
    animation.yaml
    effect.yaml
    ui.yaml
  assets/
    <category>/
      <asset-id>/
        task.yaml
        identity.json
        runs/
          <run-id>/
            plan.json
            prompt.json
            provider-request.json
            provider-response.json
            raw/
            processed/
            review.json
            cost.json
        selected/
        exports/
  jobs/
    <job-id>.json
  logs/
    api-costs.jsonl
    errors.jsonl
```

YAML 和 JSON 使用同目录临时文件写入、刷新到磁盘后原子替换。启动时将残留 `running` 状态转换为 `interrupted`，允许重试。所有用户路径经过规范化和工作区边界检查，禁止使用 `..`、符号链接或绝对路径越过当前项目工作区。

### 3. 核心数据接口

#### ProjectConfig

```yaml
schema_version: 1
project_id: wuxia-minigame
display_name: 一刀江湖美术生产
visual_type: wuxia-ink-chibi-topdown-2_5d
language: zh-CN

models:
  planner_model: ${OPENAI_REVIEW_MODEL}
  review_model: ${OPENAI_REVIEW_MODEL}
  image_model: gpt-image-2

generation:
  candidate_count: 4
  automatic_retry_count: 2
  image_quality: high
  transparency_mode: postprocess

review:
  enabled: true
  minimum_style_score: 75
  hard_constraints_required: true
```

模型名称只从配置读取。服务启动时只做配置校验；真实模型最小测试请求由用户在界面主动触发，避免每次启动产生费用。

#### AssetTask

固定类别：

```text
character
scene
item
animation
effect
ui
```

统一字段：`asset_id`、`category`、`name`、`brief`、`usage`、`style_pack`、`reference_ids`、`constraint_profile`、`constraint_overrides`、`candidate_count`、`output_mode`。

#### ConstraintProfile

公共字段包括母版尺寸、最终尺寸、PNG/RGBA、透明要求、裁切、留白、占框、缩放算法、Pivot、命名、最大文件体积和 Sprite Sheet 输出要求。

动画与特效增加帧数、网格、单帧尺寸、FPS、循环、对齐基线、共享缩放、首帧锁定、中心漂移阈值和尺寸漂移阈值。

#### GenerationPlan

规划模型必须使用结构化输出返回：资产类型、用途、参考图、构图、视角、光照、身份约束、提示词、负面约束、输出规格、后处理步骤、检查项和修复策略。

#### QualityReport

硬约束与软风格分开。硬约束不通过时禁止导出；风格评分低于阈值时允许用户手动接受，但必须保存风险原因与接受记录。

### 4. 本地 API

统一前缀 `/api/v1`。

项目与风格包：

- `POST /projects`
- `GET /projects`
- `GET /projects/{projectId}`
- `PUT /projects/{projectId}`
- `PUT /projects/{projectId}/style-guide`
- `POST /projects/{projectId}/references`
- `DELETE /projects/{projectId}/references/{referenceId}`

约束配置：

- `GET /projects/{projectId}/constraints`
- `PUT /projects/{projectId}/constraints/{category}`
- `POST /projects/{projectId}/constraints/{category}/validate`

资产生产：

- `POST /projects/{projectId}/assets`
- `POST /assets/{assetId}/plan`
- `POST /assets/{assetId}/generate`
- `POST /assets/{assetId}/edit`
- `POST /assets/{assetId}/review`
- `POST /assets/{assetId}/select`
- `POST /assets/{assetId}/export`
- `GET /assets/{assetId}/runs`

本地任务：

- `GET /jobs/{jobId}`
- `POST /jobs/{jobId}/retry`
- `POST /jobs/{jobId}/cancel`
- `GET /jobs/{jobId}/events`

任务状态：

```text
draft
planning
planned
generating
processing
reviewing
ready
needs_input
exporting
exported
failed
cancelled
interrupted
```

### 5. 阶段式智能体

不建设多个自由运行且互相对话的 Agent。单个任务按以下可追踪阶段执行：

1. `RequirementAnalyzer`
2. `ReferenceSelector`
3. `PromptCompiler`
4. `ImageGenerator`
5. `PostProcessor`
6. `ConstraintValidator`
7. `VisualReviewer`
8. `RepairPlanner`
9. `Exporter`

每阶段必须有明确输入、输出、状态和错误类型，并写入运行目录。领域管线不得直接调用 OpenAI SDK。

```python
class ImageProvider(Protocol):
    async def generate(self, request: GenerateRequest) -> list[GeneratedImage]: ...
    async def edit(self, request: EditRequest) -> list[GeneratedImage]: ...
    def capabilities(self) -> ProviderCapabilities: ...

class ReviewProvider(Protocol):
    async def plan(self, request: PlanningRequest) -> GenerationPlan: ...
    async def review(self, request: ReviewRequest) -> QualityReport: ...
```

V1 只实现 `OpenAIImageProvider` 和 `OpenAIReviewProvider`。

### 6. 六类资产管线

#### 角色

- 从风格包选择角色和总体风格参考。
- 生成角色主设候选并由用户选定基准。
- 提取主体、规范画布、校验比例、透明边缘与主色。
- 保存角色身份摘要，供头像、变体和动画使用。

#### 场景

- 支持完整背景、可平铺背景、前景/中景/背景分层。
- 强制记录俯视角、视平线语义、安全区、光源和角色可读区域。
- 不生成碰撞、导航、关卡布局或 Unity Scene。

#### 物品

- 区分世界 Sprite 与 UI 图标。
- 同组资产共享生成计划、参考图、观察角度、描边、光照和占框比例。

#### 动画

- 必须先批准游戏内基准帧。
- 不得逐帧独立生成。
- 在固定网格中一次请求生成完整动作条带。
- 统一缩放、底部中心对齐、切分帧并输出逐帧 PNG、Sprite Sheet 和预览。
- 默认预设：待机 4 帧、移动 8 帧、攻击 6 帧、受击 4 帧、死亡 8 帧。

#### 特效

- 支持透明序列和 Sprite Sheet。
- 检查锚点、边界溢出、透明残留、亮度突变与首尾连续性。
- 混合模式只作为建议记录，不生成材质。

#### UI

- 支持图标、面板、按钮状态、标签底板、弹窗装饰和数值容器。
- 正式文字不得烘焙。
- 记录九宫格安全区、边框厚度、透明边缘和状态组一致性。

### 7. 图像约束器

固定顺序：

```text
解码
→ RGBA 标准化
→ 背景处理
→ Alpha 边界框
→ 裁切
→ 共享缩放
→ 锚点对齐
→ 透明留白
→ 最终尺寸
→ PNG 编码
→ 硬约束验证
```

硬约束包括：可解码、PNG、尺寸准确、Alpha 存在、主体不触边、命名正确、动画帧数正确、网格可整除、无静默覆盖和写入哈希一致。

### 8. 前端功能

- 项目首页。
- 风格包编辑器。
- 资产生产工作台。
- 轻量裁切、缩放、留白与蒙版编辑器。
- 动画预览器。
- 约束配置器。
- 候选对比、质量报告、费用信息与任务恢复。

## 实施阶段

完整 V1 不缩减，但按可独立验证的纵向闭环推进。

### 阶段 1：项目规则与骨架

- [x] 创建独立前后端工程。
- [x] 建立共享 Schema 与类型生成。
- [x] 建立本地配置、健康检查和前后端通信。
- [x] 建立环境检查与启动脚本。
- [x] 建立测试、格式化与基础 CI 命令。

详细步骤见 `plans/plan-ai-art-agent-platform-stage-1.md`。

### 阶段 2：文件工作区与任务系统

- [x] 项目创建、读取和更新。
- [x] YAML/JSON 原子写入。
- [x] 运行目录和任务目录。
- [x] 后台队列、取消、重试、恢复和 SSE。
- [x] 路径规范化、符号链接检查和工作区边界保护。

### 阶段 3：OpenAI 适配层

- [x] 服务端读取 API Key，前端永不接收密钥。
- [x] 用户主动触发模型可用性测试。
- [x] Responses API 结构化规划与评审。
- [x] Image API 的 `gpt-image-2` 生成与蒙版编辑。
- [x] 保存请求、响应、图片和费用信息。
- [x] 区分超时、限额、拒绝、格式异常和可重试瞬时错误。

### 阶段 4：武侠风格包与提示词编译

- [x] 风格圣经 Schema。
- [x] 参考图只读导入、缩略图与标签。
- [x] 按类别、身份、用途和视角筛选参考图。
- [x] 角色身份摘要。
- [x] 固定顺序提示词编译器。
- [x] 计划预览和人工修改。

### 阶段 5：资产约束器

- [x] 公共图像操作。
- [x] 背景移除与 Alpha 清理。
- [x] 裁切、留白、缩放和对齐。
- [x] 类别约束与预览。
- [x] 规范 PNG 导出。
- [x] 固定输入黄金测试。

### 阶段 6：静态资产纵向闭环

按以下顺序，每类必须完成生成、比较、编辑、约束、评审和导出：

1. [x] 物品与武学图标。
2. [x] UI 图标、按钮和面板。
3. [x] 角色原画与游戏内基准帧。
4. [x] 2.5D 俯视角场景。

### 阶段 7：动画与特效

- [x] 动画基准帧和网格参考画布。
- [x] 整条动画一次生成。
- [x] 网格切分、共享缩放和脚底基线。
- [x] Sprite Sheet、GIF/WebP 预览和漂移曲线。
- [x] 复用基础设施完成特效管线。
- [x] 分离模型生成单格与最终导出帧，并在模型调用前校验 `gpt-image-2` 画布。

### 阶段 8：视觉评审与定向修复

- [x] 候选与参考图对比图。
- [x] 固定 Schema 风格评分。
- [x] 硬约束与风格问题分离。
- [x] 失败维度到局部修改提示词的转换。
- [x] 最多两次自动定向重试。
- [x] 展示评分依据和风险说明。

### 阶段 9：真实武侠项目试点

- [x] 从只读素材库选择 18 张获批参考图。
- [x] 六类资产各生产至少一套完整离线输出。
- [x] 同一角色完成五种基础动画离线输出。
- [ ] 手动放入 Unity 6 项目进行尺寸、透明度、动画与整体风格验收。
- [x] 根据第一轮真实误判记录增加动作级漂移阈值覆盖；无真实模型输出，因此不伪造提示词质量修订结论。

## 测试与验收

### 自动测试

- YAML/JSON 正确读写并可从中断恢复。
- 非法路径无法访问工作区外文件。
- API Key 不出现在前端响应、日志或构建产物中。
- 任务失败、取消、重试和恢复状态正确。
- 每条硬约束有独立测试。
- 透明边缘、裁切、留白和缩放匹配黄金图。
- 动画切分帧数、尺寸、共享缩放和 Sprite Sheet 排列准确。
- 前端刷新后可恢复本地任务状态。
- 导出不覆盖已有文件。
- 非预期结构化输出可报告错误并允许重试。

### V1 验收标准

- 六类资产全部具备端到端闭环。
- 所有导出文件硬约束通过率 100%。
- 每个任务最多生成 4 个候选。
- 至少 70% 的试点任务可从 4 个候选中选出无需外部绘图修改的结果。
- 同一角色动画无明显尺寸跳变、锚点漂移或身份变化。
- 同一项目角色、场景、物品、特效和 UI 可识别为同一 Q 版水墨武侠体系。
- 中断后不丢失已保存原图、处理图和配置。
- 用户可通过界面调整尺寸、帧数、留白、对齐、命名和导出规格。

## 排除范围

V1 不实现数据库、登录、多用户、正式审批、云部署、计费、Unity 插件、自动导入、其他引擎适配、LoRA、微调、自训练、私有 GPU、骨骼动画、3D 资产、Photoshop 级编辑、原生 SVG 或自动修改游戏代码。

## 变更记录

### 2026-07-28 - 建立平台主计划与武侠风格基线

- **新增文件**：`plans/plan-ai-art-agent-platform.md`、`plans/plan-ai-art-agent-platform-stage-1.md`
- **变更内容**：将用户提供的开发计划落到当前 Unity 仓库，确定平台放置于 `Tools/AiArtAgentPlatform/`，补充真实参考素材审计、Q 版水墨武侠 2.5D 俯视角风格规范、OpenAI 接口现实约束和九阶段实施路线。
- **关联说明**：`gpt-image-2` 当前不支持原生透明背景，因此透明资产改为后处理目标；本计划不修改 Unity 场景、Prefab、玩法代码或三条核心时间规则。

### 2026-07-28 - 完成阶段 1 本地平台骨架

- **修改文件**：`.gitignore`、`README.md`
- **新增目录**：`Tools/AiArtAgentPlatform/`，包含 FastAPI 后端、React/Vite 前端、共享 Schema、风格预设、启动脚本和测试。
- **变更内容**：完成本地回环绑定、密钥隔离、Q 版水墨武侠风格预设、`ProjectConfig`/`AssetTask`/`ConstraintProfile`/`GenerationPlan`/`QualityReport` 协议、健康检查、前端连接状态页、请求超时、Schema 原子导出、环境检查和进程树清理。
- **验证结果**：后端 `18 passed`，Ruff 和 Mypy 通过；前端 `7 passed`、TypeScript 检查通过、Vite 生产构建通过；Schema 重复生成哈希一致；环境检查和 `-SmokeTest` 通过，5173/8765 端口均释放；敏感信息与外部绑定扫描通过。
- **关联说明**：pnpm 11 的 `esbuild` 构建脚本仅通过 `allowBuilds.esbuild: true` 白名单启用；PowerShell 脚本运行时文本使用 ASCII 以兼容 Windows PowerShell 5.1。未调用 Unity MCP，未修改 Unity 资源和玩法规则。

### 2026-07-28 - 完成阶段 2 本地工作区与任务系统

- **修改文件**：`Tools/AiArtAgentPlatform/backend/app/workspace/**`、`backend/app/jobs/**`、项目/任务 API、前端项目与任务状态组件、阶段 2 计划与平台 README。
- **变更内容**：建立项目 CRUD、YAML/JSON 原子存储、路径边界守卫、单进程异步任务队列、取消/重试、启动恢复和 SSE 事件流；前端增加项目与任务状态卡片。
- **关联说明**：本阶段不调用 OpenAI 或 Unity MCP，不修改 Unity Scene、Prefab、组件、Inspector 或三条核心时间规则。

### 2026-07-28 - 完成阶段 3 OpenAI 适配层

- **修改文件**：`Tools/AiArtAgentPlatform/backend/app/providers/**`、`backend/app/workspace/**`、模型状态 API、前端模型状态接口与卡片、`.env.example`、平台与仓库 README、阶段 3 计划。
- **变更内容**：完成服务端密钥隔离、Responses API 结构化规划/评审、`gpt-image-2` 生成/蒙版编辑、脱敏运行记录、API 用量保存、稳定错误分类和用户主动模型可用性测试；前端默认只测试规划模型，图像模型必须显式勾选并提示费用。
- **验证结果**：后端 `40 passed`，Ruff、Mypy 通过；前端 `12 passed`，TypeScript 检查和 Vite 生产构建通过；未配置 `.env`，未执行真实 OpenAI 请求，未产生费用。
- **关联说明**：`gpt-image-2` 不发送原生透明背景或 `input_fidelity`；透明交付继续由后续图像约束器完成。未调用 Unity MCP，未修改 Unity Scene、Prefab、玩法代码或三条核心时间规则。

### 2026-07-28 - 完成阶段 4 武侠风格包与提示词编译

- **修改文件**：风格包 Schema、`backend/app/style_pack/**`、`backend/app/agent/reference_selector.py`、`prompt_compiler.py`、风格包 API、前端风格包接口与卡片、`pyproject.toml`、README 和阶段 4 计划。
- **变更内容**：完成项目风格圣经、只读素材根目录安全解析、PNG/JPEG/WebP 工作副本和缩略图、SHA-256 与标签索引、类别/身份/用途/视角筛选、角色身份摘要、最多 4 张稳定参考选择、固定顺序提示词编译和人工覆盖。
- **验证结果**：后端 `52 passed`，Ruff、Mypy 通过；前端 `17 passed`，TypeScript 与 Vite 构建通过；使用真实素材首张图片完成临时导入，源文件哈希与修改时间保持不变。
- **关联说明**：本阶段所有能力均可离线使用，不调用 OpenAI，不修改参考素材源、Unity Scene、Prefab、玩法代码或三条核心时间规则。

### 2026-07-28 - 完成阶段 5 资产约束器

- **修改文件**：`backend/app/constraints/**`、`backend/app/image_processing/**`、约束 API、共享 Schema、六类约束预设、前端约束接口与卡片、README 和阶段 5 计划。
- **变更内容**：完成项目级六类约束、边缘连通背景移除、Alpha 清理、确定性裁切/缩放/留白/锚点、独立硬约束报告、无覆盖 PNG 导出，以及前端配置、处理预览、检查与导出闭环。
- **验证结果**：后端 `67 passed`，Ruff、Mypy 通过；前端 `21 passed`，TypeScript 与 Vite 构建通过；共享 Schema 两次生成哈希一致；环境检查与无密钥启动冒烟通过。
- **关联说明**：本阶段未配置 `.env`、未调用 OpenAI、未产生费用；未调用 Unity MCP，未修改参考素材源、Unity Scene、Prefab、玩法代码或三条核心时间规则。

### 2026-07-28 - 完成阶段 6 静态资产纵向闭环

- **修改文件**：`backend/app/production/**`、生产 Schema 与共享 JSON Schema、静态生产 API、前端生产接口/Zustand Store/生产卡片、README 和阶段 6 计划。
- **变更内容**：完成物品、UI、角色和场景的任务、规划、人工提示词覆盖、最多四候选、参考图编辑式生成、确定性处理、比较选择、定向编辑、视觉评审、风险确认、刷新恢复和规范导出闭环。
- **验证结果**：后端 `81 passed`，Ruff、Mypy 通过；前端 `26 passed`，TypeScript 与 Vite 构建通过；四类 API 均使用假供应商完成闭环，共享 Schema 重复生成稳定，环境检查和无密钥启动冒烟通过。
- **关联说明**：测试未访问网络、未产生费用；未调用 Unity MCP，未修改参考素材源、Unity Scene、Prefab、玩法代码或三条核心时间规则。

### 2026-07-28 - 完成阶段 7 动画与特效序列闭环

- **修改文件**：`backend/app/schemas/sequence.py`、`backend/app/sequence_processing/**`、`backend/app/production/sequence_service.py`、序列 API、共享 JSON Schema、前端序列 API/Zustand Store/预览卡片、README 和阶段 7 计划。
- **变更内容**：完成角色基准帧参考网格、整条序列单次模型调用、逐格背景移除与 Alpha 清理、固定切分、共享缩放、锚点/基线、逐帧 PNG、Sprite Sheet、GIF/WebP、漂移度量、候选选择、重新离线处理、刷新恢复和无覆盖批量导出；特效复用同一管线。
- **验证结果**：后端与前端全量门禁、共享 Schema 重复生成、环境检查、无密钥启动冒烟和敏感信息扫描均按阶段 7 计划执行；五种角色动作和一个特效模板使用假供应商验证每条序列只有一次图像模型调用。
- **关联说明**：未配置 `.env`，未执行真实 OpenAI 请求、未产生费用；未调用 Unity MCP，未修改只读参考素材源、Unity Scene、Prefab、玩法代码或三条核心时间规则。阶段 8–9 仍待继续。

### 2026-07-28 - 完成阶段 8 视觉评审、定向修复与费用/编辑闭环

- **修改文件**：阶段 8 后端评审、修复、费用、编辑与 API 模块；前端生产卡、Konva 编辑器、费用卡、Playwright 离线用例；共享质量/生产 Schema；阶段 8 计划与 README。
- **变更内容**：候选与项目参考对比板和固定 Schema 评审可见证据；硬约束不被模型覆盖；RepairPlanner 将身份/配色/线条/构图失败映射为局部提示词；显式自动修复最多两次并保存父子运行、评审历史与停止原因；脱敏成本历史和项目汇总；候选裁切、缩放、透明留白、背景透明化、画笔/矩形蒙版和局部重绘入口；前端显示评审依据、风险、重试历史和费用未知记录。
- **验证结果**：后端 `115 passed`，Ruff/Mypy 通过；前端 Vitest `33 passed`，TypeScript/Vite 通过；Playwright 离线 `1 passed`。未配置 `.env`、未访问 OpenAI。
- **关联说明**：未调用 Unity MCP，未修改只读参考素材、Unity Scene、Prefab、玩法代码或三条核心时间规则。阶段 9 仍需真实素材试点和 Unity 人工验收。

### 2026-07-28 - 完成阶段 9 真实武侠素材离线试点

- **修改文件**：Pilot Manifest/Schema/Runner、序列任务与漂移指标、离线脚本、单元测试、共享 Schema、Unity 人工验收文档、平台与仓库 README、阶段 9 计划。
- **新增目录**：`Tools/AiArtAgentPlatform/pilot-output/wuxia-stage-9/**`、`pilot-output/wuxia-stage-9-r2/**`
- **变更内容**：选择 18 张真实只读参考，完成六类确定性离线输出和同角色五动作；第一轮保留死亡动作被全局阈值误判的证据，第二轮增加动作级中心/尺寸/基线漂移覆盖，仅放宽死亡动作，待机与移动继续使用严格全局规则。
- **验证结果**：第二轮 121 个文件全部通过，源文件运行前后哈希一致且第一轮目录聚合哈希未变化；后端 `122 passed`、Ruff/Mypy 通过，前端 Vitest `33 passed`、TypeScript/Vite/Playwright 通过，共享 Schema 稳定，环境、无密钥启动、端口和安全扫描通过。
- **关联说明**：未配置 `.env`、未调用 OpenAI 或产生费用；未调用 Unity MCP，未修改 Scene、Prefab、玩法代码或三条核心时间规则。Unity 人工导入、Play Mode 动画节奏/整体风格和真实模型 70% 采用率仍未验收。

### 2026-07-28 - 完成方案 A：分离模型序列画布与最终导出帧

- **修改文件**：`Tools/AiArtAgentPlatform/backend/app/providers/openai_image.py`、`backend/app/schemas/sequence.py`、`backend/app/production/sequence_service.py`、`backend/app/sequence_processing/grid.py`、`backend/app/sequence_processing/pipeline.py`、对应后端测试；`frontend/src/stores/sequencePreview.ts`、`frontend/src/api/client.ts`、`frontend/src/api/sequences.ts`、`frontend/src/components/SequenceCard.tsx`、`frontend/src/app/styles.css`、对应前端测试；`shared/schemas/sequence-task.schema.json`、`shared/schemas/sequence-run.schema.json`、两份 README 与相关计划。
- **新增文件**：`frontend/src/features/animation-preview/modelCanvas.ts`、`modelCanvas.test.ts`、`frontend/src/stores/sequencePreview.test.ts`、`frontend/src/api/client.test.ts`、`plans/plan-ai-art-agent-platform-openai-canvas.md`、`plans/plan-ai-art-agent-platform-openai-canvas-implementation.md`。
- **变更内容**：`SequenceTask` 增加可选模型生成单格尺寸，默认模型单格为 `512 × 512`、最终帧为 `256 × 256`；五种动作改用合法二维网格；参考画布与模型请求使用生成尺寸，逐帧 PNG、Sprite Sheet、GIF/WebP 继续使用最终尺寸；`gpt-image-2` 在 SDK 调用前校验 16 px 倍数、3:1 比例、655,360–8,294,400 总像素和 3840 px 最大边长；前端显示两套画布、具体错误并只阻止付费模型生成。旧任务缺少新字段时保持回退，历史原始拼图继续按实际网格推断，阶段 9 Pilot 无需迁移。
- **验证结果**：后端 `134 passed`，Ruff 通过，Mypy 检查 78 个源码文件通过；前端 Vitest `46 passed / 18 files`，TypeScript 与 Vite 构建通过；Playwright Chromium `1 passed`；9 份共享 Schema 连续生成稳定；环境检查与无密钥启动冒烟通过，5173/8765 端口已释放。两轮 Pilot 各 121 个文件，28 个源素材当前哈希与记录一致。
- **关联说明**：未配置 `.env`，未调用真实 OpenAI、Unity MCP 或 Play Mode，未修改只读参考素材、Pilot 输出、Unity Scene/Prefab/玩法代码或三条核心时间规则。完整 V1 仍缺真实模型输出、Unity 人工验收和真实四候选任务 70% 采用率证据。

### 2026-07-28 - 完成方案 A：项目与风格包管理闭环

- **修改文件**：`Tools/AiArtAgentPlatform/backend/app/schemas/{activity,style_pack}.py`、`backend/app/style_pack/references.py`、`backend/app/workspace/project_activity.py`、`backend/app/production/sequence_service.py`、项目/风格 API 与对应后端测试；`frontend/src/api/{projects,stylePack,production,sequences}.ts`、`frontend/src/app/{App.tsx,styles.css}`、项目/风格/参考/生产/活动/序列组件、Zustand Store 与对应 Vitest；`frontend/e2e/{project-style-management,production}.spec.ts`、`shared/schemas/project-activity.schema.json`、两份 README 与三份方案 A 计划。
- **变更内容**：完成项目创建、持久化切换与安全编辑，完整风格圣经编辑，只读素材源搜索与复制导入，参考缩略图、五类标签筛选和项目副本维护，静态任务 0–4 张显式参考选择，以及六类最近任务的静态/序列恢复；共享 Schema 数量增至 10。
- **验证结果**：后端 `137 passed`，Ruff 通过，Mypy 检查 80 个源码文件通过；前端 Vitest `70 passed / 28 files`，TypeScript 与 Vite 构建通过；Playwright Chromium `2 passed`；10 份 Schema 连续生成哈希一致；离线 Pilot `5 passed` 且只读源素材哈希保持一致；环境检查和无密钥启动冒烟通过，5173/8765 端口已释放。
- **关联说明**：未配置 `.env`，未调用真实 OpenAI、Unity MCP、Unity Editor 或 Play Mode，未修改只读参考素材、Unity Scene/Prefab/玩法代码或三条核心时间规则。方案 B、C 仍待实施，当前不宣称完整 V1 已完成。

### 2026-07-29 - 批准方案 B：动画与特效统一智能体闭环

- **新增文件**：`plans/plan-ai-art-agent-platform-sequence-agent.md`
- **前置提交**：`4e98315 chore: track ai art platform foundation`
- **变更内容**：采用“基线先行并增量扩展”方案，固化动画/特效的结构化规划、项目参考与角色身份上下文、确定性硬约束、可解释视觉评审、最多两次完整条带定向修复、父子运行、风格风险导出门禁和前端工作流设计。
- **关联说明**：方案 B 将复用现有 `ReviewProvider`、`GenerationPlan`、`QualityReport` 和 `RepairPlanner`，所有生成与修复继续遵守“整条序列一次模型调用”，禁止逐帧调用。当前仅完成设计，尚未实施；方案 C、真实模型采用率和 Unity 人工验收仍待后续处理。

### 2026-07-29 - 批准 React + Tauri 无后端目标架构

- **新增文件**：`docs/superpowers/specs/2026-07-29-ai-art-agent-tauri-design.md`、`plans/plan-ai-art-agent-platform-tauri-redesign.md`
- **变更内容**：将目标产品重新定位为个人 Windows 单机 2D 游戏资产生产工作台，确认资产任务主流程、六类资产范围、静态优先实施顺序、Tauri 本地核心、Windows 凭据库、`gpt-image-2` Image API、文件工作区兼容和无 HTTP 服务的目标架构。
- **关联说明**：本主计划此前记录的 React + FastAPI 内容继续作为当前实现和建设历史；新的目标架构由 Tauri 重构设计取代。方案 B 的动画/特效功能目标可在 Tauri 序列阶段继续采用，但其 FastAPI API 和 SSE 假设不再作为实现目标。
