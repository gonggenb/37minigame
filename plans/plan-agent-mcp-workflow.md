# Agent MCP 分阶段工作流约束

## 需求描述

更新项目开发文档，约束 Agent 在执行 Unity 项目任务时先区分脚本开发、Editor 工作和测试三种任务类型。限制 MCP 工具调用，要求调用前审批、调用时告知，并禁止在同一任务中混合脚本修改、编辑器配置和运行测试。

## 实现方案

- 在根目录 `AGENTS.md` 中增加强制任务分类、MCP 审批、逐次调用告知、分阶段停止和电脑控制限制，作为 Agent 的首要执行约束。
- 在 `docs/codex_workflow.md` 中补充可直接执行的阶段流程和完成回复字段。
- 在 `docs/task_prompts.md` 中增加三类独立任务模板，并要求跨阶段需求拆成多个提示词。
- 在 `README.md` 中增加简短入口说明，避免开发模式变化只存在于内部规则文件。
- 本需求本身属于脚本开发任务中的文档修改，不调用 Unity MCP，不执行 Editor 操作或游戏测试。

## 变更记录

### 2026-07-27 - 建立 Agent MCP 分阶段工作流

- **修改文件**：`AGENTS.md`、`README.md`、`docs/codex_workflow.md`、`docs/task_prompts.md`
- **新增文件**：`plans/plan-agent-mcp-workflow.md`
- **变更内容**：新增脚本开发、Editor 工作、测试三类互斥任务；规定跨阶段需求按顺序拆分；所有 Unity MCP 调用必须事先说明范围并取得明确审批，实际调用前逐次告知，测试发现问题后不得在同一任务直接修复。
- **关联说明**：本轮仅修改开发文档，未调用 Unity MCP，未修改游戏脚本、Scene、Prefab、Inspector 或运行状态。
