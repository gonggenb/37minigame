# 主界面山景封面改版

## 已实现

UI 类型：主菜单窗口、关卡选择窗口。沿用 `UI_STYLE_GUIDE.md` 的深木、黑铁、黄铜、低饱和暖光和随包中文字体。

- 用无字雾山、石阶、暖灯驿亭背景替换首页的视觉主图，运行时 Resources 自动加载；原 Inspector 背景保留为资源缺失回退。
- 首页取消居中大面板，改为标题、铜制六十息表盘、主按钮和低权重玩法摘要。横屏左文右景；竖屏上方标题、下方操作，裁切偏向亭子。
- 关卡选择改为卷一/卷二结构，横屏并列、竖屏纵排；主按钮跟随现有教学解锁状态。继续调用原有关卡流程。
- 首页设置保留山景承托；竖屏首页设置使用“设置”和游戏名，不显示为关卡暂停。
- 背景压暗使用缓存的 1×128 双线性 Alpha 渐变，避免多条矩形叠画的可见条纹，随 HUD 销毁释放。
- 首页按钮高 56/60，关卡与返回按钮高 44；文本、主题框体和表盘均复用项目资源。没有修改计时或战斗实现。

## 文件范围

修改：

- `Assets/Scripts/UI/PrototypeHUDController.cs`：主页/选关绘制迁出到局部类，设置背景衔接，释放渐变纹理。
- `Assets/Scripts/UI/PortraitHudViews.cs`：首页设置背景、标题和返回文案。
- `Assets/Scripts/Runtime/GameTextCatalog.cs`：新增游戏标题和两关名称常量，保留已有未提交的教学 Boss 文案。

新增：

- `Assets/Scripts/UI/PrototypeHUDController.MainMenu.cs` 及 `.meta`：响应式封面与章节选择绘制。
- `Assets/Resources/UI/MainMenu/bg_mainmenu_mountain_pass_v02.png`、纹理 `.meta` 和目录 `.meta`。
- 本记录。

保留任务开始前已有的教学引导、教学 Boss、战斗和文档改动；没有保存或重建场景。

## 资源登记

- 来源：本任务通过内置 imagegen 生成，原创无字山景；提示继承 UI 规范固定前缀，指定 HD-2D 像素手绘纵深、雾山石路与暖灯驿亭、横屏左侧低细节文字空间、竖屏主体裁切、无文字/Logo/按钮/版权图案。
- 交付：1536×1024 PNG；Unity Default Texture、Bilinear、Clamp、关闭 Mipmap、NPOT None、Max Size 2048、CompressedHQ。
- 状态：Generated → Imported → InEngineQA。未标记 Approved。
- 新增美术占位：无。资源全部缺失时存在明确注释的 `PLACEHOLDER_UI` 紧急主题面板回退；正常截图未触发。
- 继续使用既有 `PLACEHOLDER_UI` 程序设置图标；未在本次替换。

## 验证证据

Unity 6000.5.4f1，现有 Editor Play Mode：

- 编译成功；最终检查无编译错误或运行时警告。
- 执行 `37 MiniGame/Validate Chinese Fonts`：861 个非 ASCII 字形在常规体与粗体中均覆盖，文案完整性通过，无需更新子集。
- 执行 `37 MiniGame/Validate UI Safe Areas`：既有验证器 4 组横竖屏与异形屏几何检查通过。该验证器不是新首页逐元素或真机触摸验收。
- 实际检查 960×540、540×960 两套首页和选关页截图，未见标题/正文截断、重叠；修复并复查顶部压暗条纹。
- 实际鼠标点击：横屏及竖屏首页进入选关、返回主页、打开设置；竖屏设置返回主页、再次选关并进入正式关卡。
- 正式关卡入口衔接原起手武学三选一，读取阶段为 `LevelUpPaused`、关卡为“关卡2 · 驿路风云”、选择数为 3。
- 锁定条件与禁用逻辑经代码检查；没有清除用户教学进度，也没有以新用户存档运行锁定态。
- 三条核心时间规则保持原代码：普通战斗主时间继续，洞穴主时间暂停，最终 Boss 独立计时。本次没有重跑完整战斗/洞穴/Boss 回归，不把 UI 点击验证当作计时回归。

截图（本地 Logs，不作为新增运行时资源）：

- `Logs/MainMenuV2/home_landscape.png`
- `Logs/MainMenuV2/home_portrait.png`
- `Logs/MainMenuV2/chapters_landscape.png`
- `Logs/MainMenuV2/chapters_portrait.png`

结束时退出 Play Mode，恢复原 `MainPrototype` 编辑场景、竖屏 Game View 索引 7；不保存原场景的未保存内容。

## 运行与复验

打开 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode 即可看到新首页。无需新建 GameObject、挂载组件、Inspector 绑定或拖入 Prefab/UI/图片；新局部类属于既有 HUD，图片自动加载。

依次检查首页 → 选择关卡 → 返回；打开关闭设置；选择教学或已解锁的关卡2。Game View 分别设置 960×540、540×960 检查构图和按钮。

未完成：iOS/Android 真机触摸、刘海屏视觉验收、新用户锁定态实测、WebGL 重新构建和最终美术批准。本次不新增持续动画或闪烁。
