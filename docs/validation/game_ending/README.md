# 第三关全部通关与制作人员名单

2026-09-10：实现并完成受控 Unity Editor Play Mode 验证。

同日按最新署名更新：组名哈基米组；组长宋明远、策划龚宇洋、程序林智源、美术李大海；
物料与宣发吕梓昭、刘熙彦、陆可晴、李智美、谢青松。
本次仅修改 `GameTextCatalog.cs`、`PrototypeHUDController.Ending.cs`、两份玩法说明及本记录，更新既有截图和报告；没有新增文件。
角色署名采用两列对齐，物料与宣发分两行；横屏面板 620 × 500，竖屏面板 492 × 660，均受 Safe Area 约束。

## 玩家流程

第三关最终 Boss 胜利 → “全部通关 / 恭喜你，游戏已全部通关！” → 3 个真实秒后自动显示制作人员名单。
也可点击“查看制作人员”提前显示。名单内容为制作小组“哈基米组”；组长宋明远、策划龚宇洋、程序林智源、美术李大海，物料与宣发吕梓昭、刘熙彦、陆可晴、李智美、谢青松。
名单保持显示，直到玩家选择“查看通关战果”或“返回主页”。战果页可重新打开名单或“再试本关”。
第一、二关和第三关失败不触发；每次重玩第三关获胜均重新显示通关提示。

复用现有暗金九宫格、主题按钮、Safe Area 和 `RuntimeChineseFont`；未新增美术或占位资源。
沿用 `Result` 冻结玩法，不新增战斗阶段或改变时间规则。无需新建 GameObject、挂脚本或绑定 Inspector。

## 本次文件

- 修改 `Assets/Scripts/GameFlow/GameFlowController.cs`：本次通关判定、提示计时、关闭与重看状态。
- 修改 `Assets/Scripts/Runtime/GameTextCatalog.cs`：通关文案及制作人员专名。
- 修改 `Assets/Scripts/UI/PrototypeHUDController.cs`：结局与战果显示分支。
- 修改 `Assets/Scripts/UI/PrototypeHUDController.RunReview.cs`：全部通关标题、名单重看入口。
- 新增 `Assets/Scripts/UI/PrototypeHUDController.Ending.cs` 及 `.meta`：横竖屏共用结局界面。
- 新增 `Assets/Scripts/Debug/GameEndingPlayModeProbe.cs` 及 `.meta`：仅 Editor 可用的集成验证。
- 更新 `docs/project_core.md`、`docs/gameplay_systems.md`、`docs/unity_tech.md`。
- 新增本目录的说明、JSON 报告和五张截图。

上述已有文件包含本任务开始前的其他工作，本次仅追加结局相关改动，未回退其他修改。

## 运行与复验

正常运行：进入第三关 `Assets/Scenes/BambooValleyLevel.unity` 的 Play Mode，开始本关并击败最终 Boss。

自动复验：退出 Play Mode 后执行 `37 MiniGame/Validate Game Ending Play Mode`。
验证只在 Play Mode 中加载测试场景，不保存场景、不改变关卡解锁存档；结束后恢复原编辑场景与 Game View 尺寸。
可在任意编辑场景运行。本次原 `MainPrototype` 的未保存状态已保留。

## 验证证据

- Unity 脚本导入与编译完成；执行 `37 MiniGame/Validate Chinese Fonts` 及相同校验方法通过，常规体与粗体无需重建。
- [playmode_report.json](playmode_report.json)：16 项检查通过，无运行时 Error / Exception / Assert。
- 普通战斗继续主倒计时、洞穴战斗暂停主倒计时、Boss 独立计时均在实际 Play Mode 战斗中检查。
- 第三关胜负经实际战斗结束回调进入结算；夹具将 Boss 或玩家气血设为零以加速结果，非自然完整跑局。
- 第一、二关的排除条件通过受控结果入口检查，未重复跑其完整关卡。
- `Time.timeScale = 0` 时名单仍按真实时间自动显示；结局期间主时间与 Boss 时间均保持不变。
- 检查了重玩状态复位、再次通关、名单关闭/重开、返回主页与方向切换。按钮回调通过代码调用验证，未做真机触摸验收。
- 已由 Codex 目视检查以下真实 Game View 截图，标题和姓名无缺字、截断、重叠；不等于玩家的最终视觉验收。

| 页面 | 横屏 960 × 540 | 竖屏 540 × 960 |
| --- | --- | --- |
| 全部通关提示 | [notice_landscape.png](notice_landscape.png) | [notice_portrait.png](notice_portrait.png) |
| 制作人员名单 | [credits_landscape.png](credits_landscape.png) | [credits_portrait.png](credits_portrait.png) |
| 通关战果 | 沿用现有布局 | [review_portrait.png](review_portrait.png) |

仍需目标手机的触摸、刘海安全区及发布构建验收。本次为 Editor Play Mode 验证，不代表真机或最终美术批准。
