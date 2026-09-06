# 开场剧情与对话交互验证

## 已验证

- Unity 6000.5.4f1 实际编译通过，新的剧情目录可在 Editor 中访问，教学 10 句、正式开场 11 句。
- 执行 `37 MiniGame/Validate Chinese Fonts`：893 个非 ASCII 字形通过，常规体与粗体覆盖完整；本轮无需重新生成字体。
- 执行既有 `37 MiniGame/Validate UI Safe Areas`：四组横竖屏与异形屏的共享安全区检查通过。该检查不是新界面视觉验收。
- 实际 MainPrototype Play Mode：序章停在第二句时主时间保持 60。
- 调用实际展示层推进方法：未显示完时补全 28/28 字，索引保持 1；同一帧再次调用仍保持 1；后续帧再次调用进入索引 2、妖狐发言。
- 正式开场完整推进后进入 LevelUpPaused、三项起手武学选择，主时间仍为 60。
- 受控普通战斗：主时间从 60 下降到 43.72371。
- 受控洞穴敌人战斗：CaveRunning 且 battle active，主时间持续保持 57。
- 受控最终 Boss 战：主时间为 0，Boss 独立用时从 0 增至 15.309。
- 教学分支完整推进 10 句后实际载入 TutorialLevel，处于 Ready，30 秒提示打开，主时间为 30。
- 测试后退出 Play Mode，恢复原 Game View 选择与后台运行设置，返回原 MainPrototype 场景；未保存测试中的场景状态。
- `git diff --check` 通过。

## 未验证与边界

- 初轮输入检查调用实际展示层方法；随后已补测原生 Unity 窗口鼠标与空格键翻页，详见下方。手机触摸尚未验收。
- 高清立绘母版两次生成均为 RGB 棋盘格图；随后经用户明确许可用本地 Python 抠图，现已交付 RGBA 并完成 Editor 横竖屏截图检查。
- 当前高清立绘与背景均已实际加载；原像素角色只用于资源缺失时回退。
- 未做新 WebGL 构建、外部发布或手机真机验收。
- 剧情本轮仅覆盖开场；失踪者后续、Boss 战后剧情、配音与表情差分尚未制作。

## 修改范围

修改 `GameFlowController.cs`、`GameTextCatalog.cs`、`BattleScreenController.cs` 和 `AutomatedRunStatisticsRunner.cs`。
新增 `OpeningDialogueCatalog.cs`、`BattleScreenController.Opening.cs`、对应 .meta、美术原图/引用/提示词/背景、剧情说明与本记录。
统计工具取消原先写死的 8 句上限，改为读取实际对话数，避免剧情增长影响后续自动实玩验证。
无需手动 GameObject、脚本挂载或 Inspector 绑定。
工作区原有 PortraitUiLayout、WebGL 产物及共享发布设置改动不属于本任务，未回退或覆盖。

## 透明立绘接入续验

- 新增 `Tools/process_opening_portraits.py`，保留两份 RGB 原图，交付两张 1024 × 1536 RGBA 立绘与相同的归一化母版。
- alpha 检查：主角透明像素 49.14%、半透明边缘 0.69%；妖狐透明像素 24.84%、半透明边缘 0.38%。结果在 `ArtSource/Normalized/OpeningDialogue/matte_report.json`。
- 深色与旧纸色两种底色检查；针对发束封闭空隙、胳膊与身体间空隙、尾巴缝隙补做清理，保留衣服亮部。脚本包含针对这两张固定母版的区域规则，不是通用抠图模型。
- Unity 实际加载格式为 1024 × 1536 RGBA32；背景 1672 × 941 RGB24。三张贴图显式关闭 NPOT 重采样，避免默认二次幂缩放改变人物比例。
- Play Mode 截图检查：540 × 960 与 960 × 540 两种实际渲染尺寸，主角与妖狐轮流发言、姓名牌、底部文字、压暗听话者均正常，未见棋盘格底图。
- 原生 Unity 窗口鼠标点击对话框：索引 3 → 4；空格键：索引 4 → 5。说话者与对应文本同步切换，主时间维持 60。
- 截图目录：`docs/validation/opening_dialogue_images/`。并排截图位于 `ArtSource/Previews/UI/OpeningDialogue/opening_ingame_v01.png`；这是游戏内截图组合，不是生成概念图。
- 此续验只加工贴图与导入设置，未改计时或战斗逻辑；沿用初轮已完成的三条计时规则实测结果。
- 资源状态为 InEngineQA，未做真机触摸、WebGL 新构建或发布，未标记 Approved。

### 最终收尾

- 末句旁白截图 `landscape_departure.png` 确认妖狐离场，仅保留主角与古刹背景。
- 完成末句后再次验证实际载入 TutorialLevel：Ready、30 秒提示打开、计时保持 30。
- 最终 Console error 为 0；中文字体菜单再次通过 893 字形检查。
- 已退出 Play Mode，恢复本次进入前的 Game View 选择和后台运行设置；MainPrototype 场景未变脏，未保存测试状态。
- 两张运行时 PNG 与归一化母版 SHA-256 一致；RGBA、1024 × 1536、alpha 同时包含 0 与 255 的检查通过。
