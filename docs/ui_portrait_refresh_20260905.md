# 竖屏 UI / HUD v02：实施与验收

日期：2026-09-05。状态：`Imported / Editor InEngineQA`，不是最终 `Approved`。

## 本次实际接入

- 主地图：顶部气血、圆形实时计时盘、关卡/铜钱、修为进度、只读自动武学栏、磨砺摘要；底部滑动移动提示，无主动技能按钮。
- 普通战斗/洞穴战斗：相同计时器分别表达主时间继续与暂停。洞穴探索也使用暂停计时盘。
- 中期强敌：主时间暂停 + 独立交锋时间 + 胜利后继续探索说明。
- 最终 Boss：独立正向计时、全宽血条、70%/35% 阈值线、妖甲和技能状态；玩家 HUD 不与 Boss 血条重叠。
- 修为突破：纵向三选一、先选后确认、真实重数与效果、有限重选；短画布可滚动，不把确认按钮挤到屏外。
- 装备：三槽装备、背包滚动选择、真实属性/效果、攻击与防御有符号差值、装备与卸下。不是完整的所有属性差值比较器。
- 商店：竖屏双列、选中商品完整说明可滚动、购买/已售罄/铜钱不足、单洞一次刷新；保留原有库存和交易逻辑。
- 暂停：继续、音乐、方向偏好、回主页。结算：实际胜负、统计、武学、下一关可用性和回主页。

素材遵循 UI_STYLE_GUIDE 与 game-art 流程：暗木/黑铁/黄铜、低饱和状态色、无字组件和文字分层。没有替换地图、角色、武学/装备图标，没有使用整屏概念图作为游戏背景。

## 技术边界与复用

当前工程仍使用现有 IMGUI。`WuxiaUiTheme` 统一材质/状态，`WuxiaUiComponents` 提供缓存字体样式、触摸按钮、计时盘和报告行；`PortraitUiLayout` 统一模态边界与战斗 HUD 高度。`PortraitHudViews` 只负责视图；武学、装备、商店和战斗仍调用原业务接口，没有复制战斗规则。

这是一版实际接入的渐进式替换，不是完成了原始长期需求中的 UGUI Prefab/Canvas/MVC/EventBus 全套重构。没有新增 UGUI 包，没有修改场景或 Prefab 的序列化绑定。原 v01 材质仍保留。

## 文件清单

修改：

- `Assets/Scripts/UI/WuxiaUiTheme.cs`：v02 资源加载、面板/按钮切边及统一表面。
- `Assets/Scripts/UI/PrototypeHUDController.cs`：竖屏视图分派、共享战斗高度、装备页/页签适配。
- `Assets/Scripts/UI/BattleScreenController.cs`：分阶段竖屏标题、Boss 全宽血条与阈值。
- `Assets/Scripts/Cave/CaveRoomController.cs`：竖屏洞穴/商店；横屏刷新按钮避让设置入口。
- `Assets/Editor/WuxiaUiThemeAssetImporter.cs`：自动 Sprite、尺寸和九宫格导入。
- `Tools/update_chinese_font_subset.py`：同步 CFF 内部字体身份，避免只改 OpenType name 表。
- `Assets/Resources/Fonts/NotoSansCJKsc-{Regular,Bold}-Subset.ttf` 及 `.meta`：通过上述工具从锁定的静态源字体再生成，指纹同步。
- `Assets/Resources/Fonts/README.md`、本批概念图 README：生产与运行检查说明。

新增：

- `Assets/Scripts/UI/PortraitUiLayout.cs`、`PortraitHudViews.cs` 及 Unity `.meta`。
- `Assets/Resources/UI/Theme/*_v02.png` 共 11 张及 `.meta`：面板 3、按钮状态 6、槽位 1、计时盘 1。
- `ArtSource/Raw/UI/PortraitRefresh_20260905/`：四张生成源图和来源/提示词。
- `ArtSource/Normalized/UI/PortraitRefresh_20260905/`：归一化组件。
- `ArtSource/Previews/UI/PortraitRefresh_20260905/InEngine/`：运行截图与字体诊断图。
- 本实施记录。

没有修改用户原有 `ArtSource/Previews/UI/MainHUD/` 的未提交内容。

## Play Mode 证据

Unity 6000.5.4f1，`MainPrototype`，540×960 优先。战斗场景使用调试入口/反射安排，部分敌人气血为测时做了临时调整；这属于 UI 场景与计时回归，不是自然通关、数值平衡或真机性能证据。所有调试值仅在 Play Mode 内，未保存到场景。

| 验证 | 结果 |
| --- | --- |
| 普通战斗约 3 秒观察窗 | 58 → 54.67018；`NormalBattleRunning`，主时间继续 |
| 洞穴战斗约 3 秒观察窗 | 50 → 50；`CaveRunning` 且战斗仍活跃 |
| 最终 Boss 约 3 秒观察窗 | 主时间 0 → 0；Boss 用时 0 → 3.331816 |
| 武学卡片/确认 | 实际鼠标点击选中后确认，进入 MainMapRunning 并获得剑气诀 |
| 武学重选 | 次数 1 → 0；再次点击不产生额外重选；选择期间主时间 60 |
| 装备卸下/装备 | 青钢剑卸下后攻击 16 → 12，重新装备恢复 16；主时间保持 36.7367935 |
| 商店购买 | 铜钱 100 → 88，破甲掌升至 1 重；洞穴主时间保持不变 |
| 商店刷新 | 连点两次只扣 5 铜钱，100 → 95；refreshed=True |
| 商店滚动 | 实际点击滚动条，merchantScroll.y 到 424.80；结束交易回 CaveRunning |
| 末关结果 | CanContinueToNextLevel=False，点击禁用项仍在 Result；返回主页进入 Ready |
| 横屏 | 960×540 探索、Boss、商店截图检查；沿用现有横屏布局与新版材质 |

三条核心时间规则没有改动，相关 GameFlowController、BattleManager、PlayerStats 业务代码未修改。

最终静态检查：Unity 编译后 Console 无 Error；中文校验通过 841 个非 ASCII 字形；4 组横竖屏/异形屏安全区基础计算通过；`git diff --check` 通过。安全区菜单只证明基础坐标计算，不代表所有控件或真实刘海设备已验收。已退出 Play Mode，场景无未保存修改。

字体检查不仅看缺字：原始 Play Mode 曾出现不正确的汉字笔画；独立字体渲染及与锁定源字体轮廓比较正常。补全 CFF 身份、再生成并重启 Editor 后，实际截图中的暂停/音乐/卸下/武器/暴击等字形恢复。现有缺字菜单检查与肉眼截图检查必须保留，不能相互替代。

## 如何运行、测试与绑定

无需手工创建 GameObject、挂脚本、拖资源或绑定 Inspector；沿用当前场景中的 HUD、BattleScreen、CaveRoom 控制器，v02 贴图由 Resources 自动加载。

1. 打开 `Assets/Scenes/MainPrototype.unity`，等待脚本和贴图导入完成。
2. 运行 `37 MiniGame/Validate Chinese Fonts`、`37 MiniGame/Validate UI Safe Areas`。
3. Game View 选择 `Portrait 540x960` 后 Play，选择第二关、选择起手武学。
4. 滑动移动/键盘移动；自动碰怪战斗。右侧人物/背包入口或 P/B 打开面板，齿轮打开真正暂停。
5. 通过洞穴检查商店和暂停主计时；F1 调试入口可快速进入各战斗阶段。用于视觉场景安排时，不把调试通关当成平衡验收。
6. 再检查 `Landscape 960x540`；真机另测异形屏、安全区、手指拖动滚动列表和方向切换。

## 尚未完成

- 手机/WebGL 实际构建与触摸测试、动态 Safe Area/横竖屏切换、长时间内存/帧率测试。
- UGUI Prefab 化、完整 UI 管理器、动画/音效规范及自动化截图对比。
- 首页/选关/教程和部分旧状态页仍沿用原布局，只共享新的材质。
- 最终用户视觉验收；本次 `Editor InEngineQA` 不能标成正式 `Approved`。

截图目录按 01–14 编号；`font_diagnostic.png` 是字体独立渲染诊断，不是 Unity 截图。10_result 使用调试击败 Boss，极短通关用时不代表真实平衡。
