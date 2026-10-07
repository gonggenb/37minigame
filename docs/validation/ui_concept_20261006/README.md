# UI / UX 风格替换

参考：用户确认的 `ArtSource/Previews/UI/StyleConcept_20261006/`。本轮沿用已批准的暗木、黑铁、旧纸、黄铜方向，将概念拆成真实运行组件。未替换地图、角色或战斗美术，也未改动玩法数值。

## 已实现

- 共享主题接入 v03 暗木黑铁面板、Boss 框体和六态铜边按钮，覆盖首页、选关、角色/装备、设置、挑战/无尽、洞穴、武学、战斗与结算中原有共享组件。
- 探索计时器横竖屏共用表盘、数值和六十段实时进度；角色 HUD 在窄屏为表盘与右侧快捷栏预留空间，气血数值显示在条内，修为收为细线。
- 设置、角色、装备和情报入口显示图标和短文字，移动端无需悬停识别。
- 探索状态说明显示四秒后收起；区域收益等已有路线信息保留。
- 武学选择统一“点选预览 → 确认获得”，横竖屏共用选中武学；竖屏收紧未选卡片，保留真实收益和两条秘传进度，低高度窗口可滚动。
- 商店两列竖屏 / 三列横屏，先选择库存，再在纸张详情中比较并确认购买。横竖屏共用选择、库存、价格和售罄状态。刷新后清理旧滚动位置，结束交易沿用已有重进规则。
- 最终 Boss 用 `mm:ss` 累计用时；竖屏调整顶部时间、Boss 气血与玩家构筑的位置，避免窄屏重叠。普通战斗仍倒计时，洞穴仍暂停主时间。

## 修改文件

- `Assets/Scripts/UI/WuxiaUiTheme.cs`：共享材质、按钮与语义颜色。
- `Assets/Scripts/UI/PortraitUiLayout.cs`：共享组件扩展与 Boss 布局间距。
- `Assets/Scripts/UI/PortraitHudViews.cs`：探索、短提示与竖屏武学选择。
- `Assets/Scripts/UI/PrototypeHUDController.cs`：横屏 HUD、统一确认选择、血条和快捷入口。
- `Assets/Scripts/UI/PrototypeHUDController.Fusion.cs`：收紧卡片与融合详情间距。
- `Assets/Scripts/UI/PrototypeHUDController.Challenges.cs`：情报入口。
- `Assets/Scripts/UI/BattleScreenController.cs`：Boss 独立用时、配色与竖屏血条排列。
- `Assets/Scripts/Cave/CaveRoomController.cs`：横竖屏商店、纸张详情、选择与购买分离。
- `docs/UI_STYLE_GUIDE.md`：追加本轮迁移状态。

## 新增文件与资源

- `Assets/Scripts/UI/WuxiaUiComponents.Concept.cs` 及 meta：图标操作、纸张详情、选中/售罄、状态徽条、独立计时组件。
- `Assets/Scripts/Debug/ConceptUiPlayModeProbe.cs` 及 meta：可重复执行的 Editor Play Mode 回归与截图。
- `Assets/Resources/UI/Theme/*_v03.png` 及 meta：两种面板、六种按钮状态，共八张运行资源。
- `Tools/ArtPipeline/prepare_ui_concept.py`：由生成母版归一化，并派生一致的状态贴图。运行时不生成这些贴图。
- `ArtSource/Raw/UI/Concept_20261006/`：image_gen 原始母版与完整提示词。
- `ArtSource/Normalized/UI/Concept_20261006/`：归一化母版。
- 本目录：回归报告、原生鼠标操作记录和运行截图。

## 运行与 Editor 绑定

无需新建 GameObject、挂载脚本或手动绑定 Inspector；主题通过 Resources 加载，现有场景与 Controller 继续使用。

正常运行：打开 `Assets/Scenes/BootMenu.unity` 后进入 Play Mode，选关开始游戏。
开发回归：打开 `Assets/Scenes/MainPrototype.unity`，执行 `37 MiniGame/Validate Concept UI Play Mode`。
探针临时修改当前 Play 会话，不保存场景、Prefab 或测试库存；结束恢复 Game View 尺寸、时间倍率与后台运行设置，并退出 Play Mode。
更新中文文案后仍须执行 `37 MiniGame/Validate Chinese Fonts`；本轮已执行菜单并通过直接调用同一校验器的验证。

## 验证与证据

`playmode_report.json` 记录实际回归结论与错误列表。包含资源加载、武学选择与 2+2 秘传、横竖屏状态保留、提示收起、设置暂停、商店购买与防重复购买，以及：

1. 普通战斗主倒计时继续。
2. 真实洞穴战斗主倒计时暂停。
3. 最终 Boss 主倒计时保持、独立用时增长，显示支持超过六十秒。

截图覆盖 960×540、540×960 与 375×667 的关键界面；安全区布局另检查 540×960、390×844、375×667 的顶部/底部保留区。
原生鼠标点选“百毒心经”后查询仍为 0 重，点击“确认武学”后查询为 1 重并回到探索，见 `native_interaction.json` 与 `18_native_selected.png`。

资源状态为 `InEngineQA`；已完成 Editor 编译、实际 Play Mode 与静态截图检查。尚未完成本轮 WebGL 发布构建、手机真机触摸/刘海/浏览器栏以及人类最终视觉验收，不能标记最终 `Approved`。
