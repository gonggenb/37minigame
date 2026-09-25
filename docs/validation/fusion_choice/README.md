# 武学选择融合可视化

2026-09-22，Unity 6000.5.4f1。

## 本次行为

- 横屏三个武学并排，直接比较实际收益及各自两条秘传路线。
- 竖屏点选卡片展开路线，其余卡片保留武学、收益与可领悟/升重数量；底部按钮确认获得，支持列表滚动。
- 每条路线用两个流派代表图标、加号、箭头和秘传图标呈现。条件仍是流派总重数，不要求图中代表武学，也不消耗原武学。
- 两条进度分别显示选后总重数/门槛：原有重数填色，本次增加标注 +1 并以金色和纸色刻线区别，未满足格保留空心。
- 一重对应双方各 2 重，二重对应双方各 4 重；状态区分还差、可领悟、可升重、已圆满。显示目标重数的实际效果。
- 实际获得后显示秘传图标、名称及效果；多项同时解锁按队列逐项提示，每项 2.6 秒，边缘扫光仅播放一次。
- 复用 HUD 的 `reduceGrowthMotion` 关闭扫光，保留静态反馈。弹窗或升级期间暂缓获得提示，恢复操作后继续；重开清除旧反馈。
- 保留既有融合规则、武学数值、奖励流程和三条核心时间规则。反馈不增加暂停或确认步骤。

## 文件

修改：
- `Assets/Scripts/UI/PrototypeHUDController.cs`：横屏并排选择，接入反馈更新与绘制。
- `Assets/Scripts/UI/PortraitHudViews.cs`：竖屏展开选择与滚动。
- `Assets/Scripts/UI/MartialArtChoiceInsight.cs`：只读结构化融合预览。
- `docs/gameplay_systems.md`：本轮行为入口。

新增：
- `Assets/Scripts/UI/PrototypeHUDController.Fusion.cs` 及 `.meta`：共享卡片、路线、进度与获得反馈。
- `Assets/Scripts/Debug/FusionChoicePlayModeProbe.cs` 及 `.meta`：仅 Editor 的专项验证。
- 本目录说明、Play Mode JSON 及截图。

复用 `ContentIconCatalog`、`MartialArtIconRenderer`、既有秘传与流派图标、`WuxiaUiTheme`、`RuntimeChineseFont`；没有新增美术贴图或 PLACEHOLDER_UI。

## 已执行验证

- Unity 编译通过，最终 Console 无 error。
- 已执行 `37 MiniGame/Validate Chinese Fonts`，随包常规体与粗体、运行时文本检查通过，无需更新字体。
- 专项 Play Mode **55 项断言通过**，0 运行时错误：覆盖 5 种秘传的一重/二重门槛、只读预览、满重、缺少另一流派、一次获得两项、通知排队、重开清除、原武学保留与横竖屏布局。
- 真实计时验证通过：普通战斗继续扣主时间，洞穴战斗暂停主时间，最终 Boss 独立计时；获得提示没有额外暂停探索。
- 已查看横竖屏截图及第三张卡片展开、秘传圆满和获得反馈，未发现文字重叠；第三张卡片通过现有滚动区展开并完整显示。
- 选项确认走真实 `ChooseMartialArt`；第三卡展开截图使用受控 UI 状态，不作为手机真实触摸或鼠标点击验收。
- 完成后退出 Play Mode，恢复原 Game View 尺寸、后台运行和时间倍率，未保存场景。

## 运行与验证

无需新增 GameObject、挂载脚本、Prefab 或 Inspector 手动绑定。
打开 `Assets/Scenes/MainPrototype.unity`，进入第二关的起手/升级选择；横屏直接选择，竖屏先点卡片再按“领悟此诀”。

专项复验：退出 Play Mode 后执行 `37 MiniGame/Validate Fusion Choice Play Mode`。
测试使用受控构筑和战斗夹具，不保存场景；结束恢复 Game View 尺寸、后台运行与时间倍率。
`playmode_report.json` 保存断言与运行时错误；截图为 960×540 / 540×960 实际 Game View。

验收边界：Editor 编译、字体与受控 Play Mode 检查不等于真机触摸、安全区、性能或最终美术 Approved；这些仍需手机实测。
