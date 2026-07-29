# AI 美术智能体平台 Tauri 无后端重构

## 需求描述

将现有个人本地 2D 游戏美术工作台重新定位为 Windows 单机资产生产应用。保留角色、场景、道具、UI、动画和特效六类资产能力，取消 FastAPI、本地 HTTP 端口和独立后端服务，通过 React + Tauri 接入 `gpt-image-2`，并保证 API Key、本地素材和项目数据的安全边界。

## 实现方案

正式设计规格见：

- `docs/superpowers/specs/2026-07-29-ai-art-agent-tauri-design.md`

已确认决策：

- 产品形态采用 React + Tauri Windows 单机应用。
- 前端以资产任务工作流为主，轻量画布只用于候选编辑，对话输入只用于整理需求。
- 六类资产入口全部保留，实施顺序为四类静态资产优先，动画与特效随后迁移。
- 图片生成和编辑明确使用 `gpt-image-2` Image API。
- API Key 保存到 Windows 系统凭据库，不进入 React、项目文件或日志。
- 现有文件工作区和历史运行记录通过版本兼容方式保留。
- 正式工作区默认位于用户文档目录，并支持复制导入现有 `data/workspaces`。
- 图像处理和任务系统迁移为 Rust 本地模块，发行包不携带 Python Sidecar。
- 迁移完成后删除 FastAPI、HTTP API、SSE、端口和 Python 运行依赖。
- 平台继续与 Unity 运行时隔离，只导出图片和质量报告。

旧 `plans/plan-ai-art-agent-platform.md` 记录现有 FastAPI 平台的建设历史；旧 `plans/plan-ai-art-agent-platform-sequence-agent.md` 记录动画/特效智能体草案。两者保留，但其中关于目标运行架构的 FastAPI 假设由本计划取代。

## 变更记录

### 2026-07-29 - 批准 React + Tauri 无后端重构设计

- **修改文件**：`plans/plan-ai-art-agent-platform.md`
- **新增文件**：`docs/superpowers/specs/2026-07-29-ai-art-agent-tauri-design.md`、`plans/plan-ai-art-agent-platform-tauri-redesign.md`
- **变更内容**：重新评定产品目标、主流平台取舍、前端信息架构、Tauri 本地核心、`gpt-image-2` 接入、安全边界、工作区兼容、迁移顺序、测试策略和 V1 验收标准。
- **关联说明**：当前代码仍为 React/Vite + FastAPI，尚未开始 Tauri 实现。本次只建立设计，不调用 OpenAI、Unity MCP、Unity Editor 或 Play Mode，不修改 Unity 资源、玩法代码或三条核心时间规则。
