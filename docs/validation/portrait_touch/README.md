# 竖屏按钮触控尺寸优化

本轮属于窗口、装备、武学、洞穴与结算 UI 布局调整，沿用现有黑铁、暗金九宫格及随包中文字体，不新增美术或文案。

## 实现

- `PortraitUiLayout` 集中 64 逻辑单位按钮高度、12 单位间隔及 24 单位底部内边距。可见框体与实际 `GUI.Button` 点击区域同步扩大。
- 武学确认/重选、商店购买/刷新/结束、结算再试/下一关/主页、情报确认与制作人员页采用加高按钮；滚动正文同步让出空间。
- 设置开关高 64；方向选项独占一行，窄屏设置内容可滚动，顶部继续与底部主页按钮固定。
- 角色窗口关闭按钮 64×64；两枚页签加高并分配整行宽度。装备卸下按钮 88×64，已装备和背包共用滚动区，底部详情与装备按钮固定。
- 选关卡片和返回按钮、教学确认、洞穴出口同步加高。洞穴出口仍位于已有移动输入排除区域内。
- 横屏保留原有按钮尺寸和排列。长操作文案允许换行；按钮事件、存档与计时逻辑保持原实现。

## 文件

修改：`Assets/Scripts/UI/PortraitUiLayout.cs`、`PortraitHudViews.cs`、`PrototypeHUDController.cs`、`PrototypeHUDController.MainMenu.cs`、`PrototypeHUDController.RunReview.cs`、`PrototypeHUDController.Challenges.cs`、`PrototypeHUDController.Ending.cs`，以及 `Assets/Scripts/Cave/CaveRoomController.cs`。

新增：本说明和本目录内验证截图（如有）。没有新增运行时脚本、Prefab 或资源。

## 运行与验收

不需要创建 GameObject、挂载脚本或绑定 Inspector/Prefab。打开现有 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode。

1. Game View 设为 `540×960`，从首页检查选关、设置，然后进入单局检查情报、武学选择、角色/装备、云游商店与结算。
2. 改为 `390×844`、`375×667`，检查按钮文字、滚动区与固定底部操作没有重叠，滚到底仍能操作。
3. 改为 `960×540`，确认横屏原布局与点击事件正常。
4. 设置开关点击一次只切换一次；装备/卸下后数据正确；商店铜钱不足/售罄/刷新耗尽时禁用；关闭弹窗后可恢复滑动移动。
5. 真机补验刘海/底部手势安全区、单手拇指点击与滚动误触。

本轮仅改绘制尺寸和布局；普通战斗继续消耗主时间、洞穴暂停主时间、最终 Boss 独立计时的代码未修改。编辑器截图不代表 iOS/Android 真机触控批准。

## 本轮验证记录（2026-09-21）

- 本轮修改脚本已由 Unity 导入编译；Console 查询为 0 error。
- 本轮涉及文件 `git diff --check` 通过。工作区其他既有场景/项目设置存在空白警告，未修改这些文件。
- 已查看真实 Play Mode 设置页截图：`settings_390x844.png`、`settings_375x667.png`。加高按钮无重叠；375×667 内容区可滚动且底部操作固定。
- 装备、商店、结算、540×960 与横屏本轮完整交互回归尚未完成：共享 Editor 的其他玩法验证重新进入/改变了 Play Mode，未将其运行结果归入本轮证据。无真机触摸验收结论。
- 本轮没有新增或修改中文文案，也没有新增 `PLACEHOLDER_UI` 资源；沿用既有主题资源状态。
