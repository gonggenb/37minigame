# 哈基米组呈献 · 品牌开屏

2026-09-10。原创工作室标识与应用启动流程已接入。

## 设计与播放

标识采用暗金猫首、山形和断开印章轮廓，墨黑底；“哈基米组呈献”由随包中文字体独立绘制，不烘焙在图片里。
采用 `image_gen` 内置生成工具，未使用参考公司标识或参考图。完整生成词保存在
[prompt_hakimi_logo_v01.md](../../../ArtSource/Raw/Branding/prompt_hakimi_logo_v01.md)。

动画用运行时代码完成：0.55 秒淡入 → 1.8 秒停留 → 0.45 秒淡出，总长约 2.8 秒。
仅使用透明度变化，没有额外视频、逐帧素材、粒子、闪光或缩放，也未新增声音。
首个实际绘制帧才开始计时，避免场景加载吃掉展示时间。淡出下方是原有首页。
0.35 秒启动点击保护后支持点击、触摸或任意键跳过，仍保留 0.45 秒淡出。

开屏暂停玩法与既有音频、禁用底层菜单和移动输入；关闭恢复原时间倍率与音频暂停状态。
关闭后额外拦截一帧，避免跳过操作穿透到首页。返回主页、重玩与切关均不重播。
每次应用重新启动或重新进入 Editor Play Mode 会播放一次；不写入玩家解锁存档。无渲染批处理模式跳过展示。

## 资源与文件

新增：

- `Assets/Scripts/UI/StudioSplashScreen.cs` 及 `.meta`：启动创建、真实时间动画、跳过、音频与时间恢复。
- `Assets/Scripts/Debug/StudioSplashPlayModeProbe.cs` 及 `.meta`：仅 Editor 的开屏集成检查。
- `ArtSource/Raw/Branding/hakimi_cat_seal_v01.png`：保留工具原始透明母版，实际 1254 × 1254。
- `ArtSource/Raw/Branding/prompt_hakimi_logo_v01.md`：生成说明与完整提示词。
- `Assets/Resources/UI/Branding/logo_hakimi_group_v01.png` 及目录 / 图片 `.meta`：随包 Logo，源 PNG 与母版一致。
- 本目录的 README、JSON 报告、横竖屏开屏截图与进入首页截图。

修改：

- `Assets/Scripts/Runtime/GameTextCatalog.cs`：复用组名，集中“哈基米组呈献”。
- `Assets/Scripts/GameFlow/GameFlowController.cs`：开屏时停止推进玩法计时。
- `Assets/Scripts/UI/PrototypeHUDController.cs`：开屏时屏蔽操作，淡出下方保留不可交互首页。
- `Assets/Scripts/UI/BattleScreenController.cs`、`Assets/Scripts/UI/MobileInputController.cs`：阻止底层战斗 UI 与移动输入。
- `Assets/Scripts/Debug/GameEndingPlayModeProbe.cs`：原结局回归先等待启动开屏结束。
- `docs/project_core.md`、`docs/gameplay_systems.md`、`docs/unity_tech.md`：同步启动流程和接入方式。

导入设置：RGBA32、保留 Alpha、Clamp、Bilinear、无 Mipmap、无压缩、最大 1024；运行时尺寸 1024 × 1024。
Logo 以 240（横屏）/ 280（竖屏）逻辑单位显示，并受 Safe Area 限制。此标识为启动品牌图，不是 128 像素物品图标。
素材状态为 `Generated → Imported → InEngineQA`，无像素后处理，无新增占位美术，尚未标为 `Approved`。

## 运行与检查

无需创建 GameObject、挂载脚本、绑定 Inspector 或调整 Build Settings。
打开 `Assets/Scenes/MainPrototype.unity` 并进入 Play Mode，即先播放开屏再进入首页。
应用启动事件自动创建对象；普通关卡切换不会再次创建。

自动复验：`37 MiniGame/Validate Studio Splash Play Mode`。
原流程回归：`37 MiniGame/Validate Game Ending Play Mode`。
中文校验：`37 MiniGame/Validate Chinese Fonts`。本轮常规体与粗体字形覆盖通过，无需重建字体。

证据：

- [playmode_report.json](playmode_report.json)：13 项真实 Editor Play Mode 检查通过，无运行时错误。
- [原结局回归报告](../game_ending/playmode_report.json)：开屏接入后重跑 16 项通过，包括普通战斗继续主时间、洞穴暂停主时间、Boss 独立计时与第三关片尾名单。
- [splash_landscape.png](splash_landscape.png)：960 × 540 开屏。
- [splash_portrait.png](splash_portrait.png)：540 × 960 开屏。
- [main_menu_after_splash.png](main_menu_after_splash.png)：自然结束后的首页。
- Logo 随包加载、首次自动播放、横竖屏切换、两种玩法计时保持、时间与音频恢复、同会话不重播、跨关不重播均已验证。
- 跳过回调由测试调用，已检查短淡出、精确保留原全局状态、没有打开底层菜单；未声称真实触摸设备验证。
- Codex 已目视检查实际游戏截图，Logo 透明底正常，标题无乱码和截断。最初发现的 IMGUI 层级遮挡已修正并重跑。

仍需用户最终视觉选择、手机触摸 / 刘海安全区与发布构建验证；当前不代表真机验收或最终美术批准。
