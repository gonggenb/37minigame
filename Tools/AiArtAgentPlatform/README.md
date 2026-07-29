# 2D 小游戏 AI 美术生产工作台

这是一个面向个人使用、仅在本机运行的 2D 游戏美术生产工具。平台与 Unity 游戏工程隔离，V1 只导出 PNG、逐帧 PNG、Sprite Sheet、GIF/WebP 预览和质量报告，不创建 Unity Scene、Prefab、材质或导入配置。

## 安全边界

- 本地 API 只允许绑定 `127.0.0.1` 或 `localhost`。
- OpenAI API Key 只由 Python 后端从本地 `.env` 读取，不进入前端代码、浏览器响应或日志。
- `.env` 和 `data/` 已排除在 Git 之外。
- `data/` 保存项目配置、参考图工作副本、生成过程和导出结果。
- 外部参考素材按只读方式索引，平台不得重命名或修改源文件。
- 启动、健康检查和模型状态读取不会调用 OpenAI API；模型可用性测试、生成和评审仅由用户主动触发。

## 首个风格预设

首个内置预设为“Q 版水墨武侠俯视角”，目标是生成统一的轻量化 2.5D 俯视角角色、场景、物品、逐帧动画、特效和 UI 素材。

只读参考素材路径：

```text
D:\MiniGame\素材\Q版中国风水墨武侠割草游戏仙侠武器音效动画UI图标界面美术素材\Q版中国风水墨武侠割草游戏仙侠武器音效动画UI图标界面美术素材\Q版水墨国风（行侠仗义五千年）
```

## 当前阶段

阶段 1–9 已建立 React/Vite 前端、FastAPI 后端、共享 Schema、武侠风格预设、本地项目与任务系统、供应商隔离的 OpenAI 适配层、只读风格包/提示词编译能力、确定性资产约束器、四类静态资产生产闭环，以及角色动画/透明特效序列闭环。当前可完成物品、UI、角色与场景的任务创建、结构化规划、候选生成与评审，也可从角色基准帧创建固定网格，通过一次图像模型调用生成整条动画，离线完成逐格背景移除、Alpha 清理、共享缩放、锚点对齐、Sprite Sheet、GIF/WebP 和漂移报告。序列任务已分离模型生成单格与最终导出帧，新任务默认使用 `512 × 512` 模型单格和 `256 × 256` 最终帧，前后端会显示实际模型画布并在付费调用前阻止非法 `gpt-image-2` 尺寸。阶段 8 还提供候选/参考对比板、固定 Schema 评分解释、硬约束与风格问题分离、RepairPlanner 定向提示词、最多两次显式自动修复、脱敏费用汇总，以及 Konva 裁切/缩放/透明留白/画笔与矩形蒙版编辑器。阶段 9 使用 18 张真实只读武侠素材完成六类离线试点，并支持动作级中心、尺寸和脚底基线漂移阈值覆盖。平台仍不修改 Unity 项目运行时内容。

方案 A“项目与风格包管理闭环”已完成：首页可创建、选择、切换和编辑项目，并在刷新后恢复最后一个有效项目；完整风格圣经可编辑保存；只读素材源支持搜索和复制导入，项目参考库支持缩略图、五类标签筛选、标签维护与项目副本移除；静态任务可显式选择 0–4 张参考图；角色、场景、物品、动画、特效和 UI 六类最近任务均可重新打开。共享 JSON Schema 现为 10 份。方案 B 的动画/特效统一智能体能力和方案 C 的人工采用率、Unity 验收统计仍未完成，因此当前不宣称完整 V1 已验收。

## 安装

在本目录运行：

```powershell
python -m pip install -e ".[dev]"
pnpm install
Copy-Item .env.example .env
```

离线使用不要求填写 `OPENAI_API_KEY`。需要主动测试模型、规划、评审或生成图片时，在本地 `.env` 中配置密钥；不要把密钥发送到前端或提交到 Git。默认模型配置为：

```dotenv
OPENAI_REVIEW_MODEL=gpt-5.6
OPENAI_IMAGE_MODEL=gpt-image-2
OPENAI_TIMEOUT_SECONDS=120
OPENAI_MAX_RETRIES=2
```

## 环境检查

```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-environment.ps1
```

检查 Python 3.12、Node.js、pnpm、本地数据目录、端口和密钥配置状态。脚本只报告密钥是否配置，不输出密钥值。

## 启动

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start.ps1
```

前端地址为 `http://127.0.0.1:5173`，后端地址为 `http://127.0.0.1:8765`。启动脚本不会自动打开浏览器；停止脚本时会清理前端和后端子进程。

自动冒烟检查：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/start.ps1 -SmokeTest
```

## 测试

```powershell
pnpm test -- --run
pnpm typecheck
pnpm build
pnpm e2e
```

Playwright 用例只拦截本地 API fixture，不访问 OpenAI；首次运行 `pnpm exec playwright install chromium` 准备浏览器。

```powershell
python -m pytest backend/tests -v
python -m ruff check backend
python -m mypy backend/app
pnpm --dir frontend test --run
pnpm --dir frontend typecheck
pnpm --dir frontend build
```

重新生成共享 JSON Schema：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/generate-schemas.ps1
```

脚本当前稳定生成 10 份 Schema，其中包含 `project-activity.schema.json`。

完整计划见：

- `../../plans/plan-ai-art-agent-platform.md`
- `../../plans/plan-ai-art-agent-platform-stage-1.md`
- `../../plans/plan-ai-art-agent-platform-stage-2.md`
- `../../plans/plan-ai-art-agent-platform-stage-3.md`
- `../../plans/plan-ai-art-agent-platform-stage-4.md`
- `../../plans/plan-ai-art-agent-platform-stage-5.md`
- `../../plans/plan-ai-art-agent-platform-stage-6.md`
- `../../plans/plan-ai-art-agent-platform-stage-7.md`

## 阶段 2：本地工作区与任务系统

阶段 2 已提供以下本地能力：

- `POST/GET/PUT /api/v1/projects`：创建、读取、列出和更新项目配置。
- `GET /api/v1/projects/{project_id}/jobs`：读取项目最近任务。
- `POST /api/v1/projects/{project_id}/jobs`：加入单机后台任务队列。
- `GET /api/v1/jobs/{job_id}`：读取任务状态；`POST .../cancel` 和 `POST .../retry` 支持取消与重试。
- `GET /api/v1/jobs/{job_id}/events`：通过 SSE 回放并订阅任务进度事件。

每个项目保存到 `data/workspaces/<project-id>/`。项目配置和任务记录采用同目录临时文件加原子替换；服务启动时会把残留的活动任务标记为 `interrupted`，并保留事件记录。路径守卫会拒绝非法 slug、绝对路径、`..` 和指向工作区外部的符号链接。

阶段 2 仍不会调用 OpenAI，不会生成图片，也不会修改 Unity Scene、Prefab、组件或 Inspector 配置。

## 阶段 3：OpenAI 适配层

阶段 3 已提供：

- `GET /api/v1/models/status`：读取密钥是否配置、规划模型、图像模型和本地超时/重试设置，不产生模型用量。
- `POST /api/v1/models/availability`：由界面主动触发；默认只测试规划模型，显式勾选后才测试图像模型并产生对应 API 用量。
- `OpenAIReviewProvider`：通过 Responses API 结构化解析 `GenerationPlan` 与 `QualityReport`。
- `OpenAIImageProvider`：通过 Image API 封装 `gpt-image-2` 图片生成与蒙版编辑，每次最多 4 个候选。
- 稳定错误码：区分超时、连接、限额、认证、权限、请求、服务端错误、内容拒绝、结构化输出和不支持能力，并标记是否可重试。
- 运行记录：保存到 `assets/<category>/<asset-id>/runs/<run-id>/`，包含脱敏请求/响应、`raw/` 原图、`processed/` 目录和 `cost.json` 用量信息。

`gpt-image-2` 当前不使用原生透明背景参数。透明资产仍按“实体背景生成 → 后续抠图和 Alpha 清理 → RGBA/硬约束验证”处理。`cost.json` 只保存 API 返回的用量，不硬编码价格或估算金额。

## 阶段 4：武侠风格包与提示词编译

阶段 4 已提供：

- 项目级 `style-pack/style-guide.yaml`，首次读取时从对应内置预设复制，后续在工作区原子更新。
- 只读参考源枚举与导入，只接受风格圣经声明根目录内的 PNG/JPEG/WebP，拒绝 `..`、绝对路径和符号链接越界。
- 原图工作副本、SHA-256、尺寸、最长边 256 像素 PNG 缩略图和 `reference-index.json` 标签索引。
- 按资产类别、角色身份、用途和视角筛选参考图；提示词编译时稳定选择最多 4 张相关参考。
- 角色身份摘要保存到 `assets/character/<asset-id>/identity.json`。
- 固定顺序提示词段落：项目风格、资产任务、身份、参考图、构图视角、光照材质、输出规格、禁止项、后处理目标。
- 首页风格包卡片，可导入只读参考、填写资产需求、预览提示词并提交人工覆盖。

主要 API：

```text
GET/PUT /api/v1/projects/{project_id}/style-guide
GET     /api/v1/projects/{project_id}/reference-source
POST/GET/DELETE /api/v1/projects/{project_id}/references
GET/PUT /api/v1/projects/{project_id}/identities/{asset_id}
POST    /api/v1/projects/{project_id}/prompt-preview
```

提示词预览和参考管理都是离线操作，不会调用 OpenAI。真实素材源不会被修改；平台只在项目工作区保存副本、缩略图和元数据。

## 阶段 5：资产约束器

阶段 5 已提供：

- 六类项目级约束配置，首次读取时从武侠预设复制并支持原子更新。
- PNG/JPEG/WebP 解码与 RGBA 标准化，只移除与画布边缘连通的角落背景色。
- Alpha 高低阈值清理、完全透明输入检测和主体 Alpha 边界裁切。
- Lanczos/Nearest 缩放、占框比例、透明留白、中心/底部中心锚点与场景填满模式。
- 独立硬检查：解码、PNG、尺寸、RGBA、Alpha、主体边界、命名、文件体积、Sprite Sheet 网格和内容哈希。
- 首页资产约束卡片，可编辑输出尺寸、留白、占框和透明要求，使用工作区图片离线预览，并展示每条硬检查。
- 规范 PNG 导出到 `assets/<category>/<asset-id>/exports/`；硬检查失败时禁止写入，同名文件默认拒绝覆盖，写入后重新校验哈希。

主要 API：

```text
GET  /api/v1/projects/{project_id}/constraints
PUT  /api/v1/projects/{project_id}/constraints/{category}
POST /api/v1/projects/{project_id}/constraints/{category}/process-preview
POST /api/v1/projects/{project_id}/constraints/{category}/validate
POST /api/v1/projects/{project_id}/constraints/{category}/export
```

所有阶段 5 能力均可离线使用，不会自动调用 OpenAI。输入路径必须位于当前项目工作区，参考素材源仍保持只读。

## 阶段 6：静态资产生产闭环

阶段 6 已提供：

- `item`、`ui`、`character`、`scene` 四类静态资产任务与独立运行历史。
- 结构化规划结果、人工提示词覆盖、原始候选、处理候选、质量报告和选择结果持久化。
- 无参考图时调用生成接口；存在已选工作区参考图时通过编辑接口携带最多四张参考。
- 每个任务最多四个候选，逐张执行背景/Alpha、裁切、缩放、留白、锚点和硬约束处理。
- 候选选择、定向编辑新运行、视觉评审、本地硬约束覆盖模型判断和低分风格风险确认。
- 生产卡片使用 Zustand 保存表单状态、TanStack Query 读取资产与运行历史，页面刷新后恢复最近运行。
- 物品、UI、角色和场景均使用假供应商完成 API 端到端测试，不访问网络、不产生费用。

主要 API：

```text
POST /api/v1/projects/{project_id}/assets
GET  /api/v1/projects/{project_id}/assets
GET/PUT /api/v1/projects/{project_id}/assets/{category}/{asset_id}
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/plan
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/generate
GET  /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs
GET  /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/image
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/select
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/edit
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/review
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/review-and-repair
GET  /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/comparison
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/transform
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/mask
POST /api/v1/projects/{project_id}/assets/{category}/{asset_id}/runs/{run_id}/export
GET  /api/v1/projects/{project_id}/costs
```

规划、生成、编辑和评审只在用户点击带“调用模型”标记的按钮时执行。没有 `.env` 时这些操作返回稳定错误，已有资产、候选、约束与导出仍可离线读取和处理。

阶段 8 的自动修复按钮会先展示评审依据和 RepairPlanner 生成的局部提示词，并明确最多两次和可能产生 API 用量；没有可操作失败证据时系统停止，不会无限重新生成。费用汇总只报告供应商返回的已知金额，缺失计费信息保留为“费用未知”。

## 阶段 7：动画与特效序列

阶段 7 已提供：

- `animation` 与 `effect` 两类序列任务；角色动画必须提供当前项目工作区内的已批准基准帧。
- 待机 4 帧 `2×2`、移动 8 帧 `2×4`、攻击 6 帧 `2×3`、受击 4 帧 `2×2`、死亡 8 帧 `2×4` 五种初始模板，并允许调整 1–32 帧、1–8 行列、模型单格、最终单帧尺寸、FPS 和循环。
- 模型网格默认每格 `512 × 512`，最终逐帧 PNG 默认 `256 × 256`；历史任务缺少生成尺寸时继续兼容，旧原始拼图由实际图片尺寸切分，不覆盖阶段 9 Pilot。
- 角色基准帧只进入参考网格第一槽；每个候选整条序列只调用一次 `ImageProvider.edit`，特效只调用一次 `generate` 或带参考图的 `edit`，不逐帧调用模型。
- 模型按纯色、均匀、可分离背景生成；固定切分后逐格执行边缘连通背景移除和 Alpha 清理，再进行全序列共享缩放与底部中心/中心锚点对齐，不要求 `gpt-image-2` 原生透明。
- 后端在调用 `gpt-image-2` 前验证 16 px 倍数、3:1 比例、655,360–8,294,400 总像素和 3840 px 最大边长；前端显示具体失败原因，只禁用模型生成，不影响已有候选的离线重处理。
- 逐帧 PNG、Sprite Sheet、GIF、WebP、帧级 Alpha 边界/中心/尺寸/基线/面积/颜色/亮度记录，以及中心、尺寸、基线、面积、颜色、亮度和首尾差异报告。
- 前端逐帧播放、FPS/循环调节、棋盘/宣纸米白/墨灰背景、Alpha 边界框、锚点、脚底基线、轻量 SVG 漂移曲线、候选选择、刷新恢复和无覆盖导出。

主要 API：

```text
POST /api/v1/projects/{project_id}/sequences
GET  /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs
GET  /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}
POST /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/generate
POST /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/reprocess
POST /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/select
GET  /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/frames/{frame_index}
GET  /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/sprite-sheet
GET  /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/preview.gif
GET  /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/preview.webp
GET  /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/candidates/{candidate_id}/drift-report
POST /api/v1/projects/{project_id}/sequences/{category}/{asset_id}/runs/{run_id}/export
```

测试使用假供应商，不访问网络、不产生费用。当前工作区没有 `.env` 时不会执行真实 OpenAI 序列生成。

## 阶段 9：真实只读素材离线试点

阶段 9 的 Manifest 位于 `shared/pilot/wuxia-stage-9.yaml`，固定读取用户提供的只读素材根目录。Runner 会在运行前后计算全部输入文件 SHA-256，并拒绝覆盖已存在的输出目录。

本轮已完成：

- 选择 18 张真实参考，覆盖角色、场景、物品、动画、特效和 UI。
- 生成角色、场景、物品、UI 四类静态 PNG 与硬约束报告。
- 为同一 `yuanchengcike` 角色生成待机、移动、攻击、受击代理和死亡五套逐帧 PNG、Sprite Sheet、GIF/WebP 与漂移报告。
- 将 `fire_026_4x4_Tex.png` 处理为 16 帧特效序列并输出预览与漂移报告。
- 第一轮 `pilot-output/wuxia-stage-9/` 保留了死亡动作被全局 4 px / 8% 阈值误判的证据；第二轮 `pilot-output/wuxia-stage-9-r2/` 仅为死亡动作设置 16 px 中心漂移、20% 尺寸漂移和 2 px 基线漂移覆盖，待机与移动仍使用严格全局阈值。
- 两轮输出均为独立目录；第一轮目录未被覆盖，第二轮 18 张参考的源文件哈希前后一致。
- 第二轮共生成 121 个文件；后端 122 项测试、Ruff、Mypy、前端 33 项测试、TypeScript、Vite 和 Playwright Chromium 均通过，共享 Schema 连续生成稳定。

运行新的离线试点时必须提供尚不存在的输出目录：

```powershell
powershell -ExecutionPolicy Bypass -File scripts/run-offline-pilot.ps1 -OutputDirectory "D:\path\to\new-pilot-output"
```

Unity 手动导入与验收步骤见 `docs/unity-pilot-acceptance.md`。本轮没有调用 Unity MCP、没有进入 Unity Editor 或 Play Mode，也没有 `.env` 或真实 OpenAI 输出，因此动画节奏、引擎内 Alpha/尺寸、整体风格和“70% 无需外部修改”仍属于外部验收缺口。
