# 一炷江湖 · 竖屏宣传片 v01

用途：抖音／视频号。30 秒，9:16，1080 × 1920，30 FPS，H.264 + AAC。
这是带配乐的第一版宣传剪辑，尚未发布。无配音、下载链接或上线日期。

## 分镜

| 时间 | 画面 | 文案 |
| --- | --- | --- |
| 0–3 秒 | 血月古刹、九尾妖狐立绘推进、雾带、刀光切场 | 六十秒后，强敌来袭 |
| 3–6 秒 | 主角立绘推进、山景与飘尘 | 这一局，你如何破局？ |
| 6–11 秒 | Play Mode 道路移动、自然碰怪进入战斗，保留 HUD | 60 秒探索／每一步，都是选择 |
| 11–14 秒 | Play Mode 精英自动战斗 | 碰怪即战／自动交锋，时间不停 |
| 14–17 秒 | Play Mode 武学选择与融合路线 | 武学搭配／选择你的江湖路 |
| 17–24 秒 | Play Mode 九尾妖狐战斗 | 挑战九尾妖狐／以本局构筑，迎战强敌 |
| 24–30 秒 | 双人立绘、片名、哈基米组 Logo | 再来一局，换条路闯江湖 |

## 素材与真实性

- 立绘来自 `ArtSource/Normalized/OpeningDialogue/`，背景来自 `Assets/Art/Generated/Backgrounds/`。
- 雾带来自 `Assets/Art/Generated/Environment/HD2D/`；Logo 来自 `Assets/Resources/UI/Branding/`。
- 中文使用项目随包 Noto Sans CJK SC 粗体子集，合成前检查全部字幕字形。
- 配乐使用项目原创合成曲 `bgm_menu_wuxia_punk_dj_60s_v02.wav`，刀光和切场使用项目战斗音效。没有录入麦克风或其他应用声音；实机战斗声音未逐事件录制。
- 实机原始画面为 720 × 1280，30 FPS。整片合成后放大导出 1080 × 1920，不称为原生 1080p 录屏。
- 实机段来自当前 Unity Editor Play Mode，以受控移动输入、奖励接口配置的可获得武学组合与指定遭遇录制。没有伤害、血量、敌方数值覆盖，但不是连续自然通关录像或平衡测试。
- 动画是现有立绘的动态分镜：推进、平移、烟雾、粒子和刀光转场，不包含新生成的连续角色动作。
- 片中明确标记“动画演绎”和“实机演示”；不使用角色动作源 MP4 假充实机。

## 复现

1. Unity 打开并保存 `Assets/Scenes/MainPrototype.unity`，确保不在 Play Mode。
2. 执行 `37 MiniGame/Promotional/Capture Vertical Trailer`。工具自动进入 Play Mode、切换临时竖屏尺寸、录制并退出，恢复尺寸、后台运行设置与捕获帧率；不保存场景。不需要 GameObject、Prefab、Inspector 手动绑定。
3. 检查 `capture/report.json`：`success` 为 true，`runtimeErrors` 为空。每次重录覆盖该目录同名镜头。
4. 使用安装了 Pillow、numpy、fontTools 的 Python 执行 `Tools/render_vertical_trailer.py --ffmpeg /path/to/ffmpeg`；`--preview` 仅生成分镜预览。
5. 播放 `一炷江湖_竖屏宣传片_v01.mp4` 检查全片。`silent.mp4` 为无声中间件，`capture/` 为原始帧。

## 范围与验证

- 只新增录制工具、合成工具和本目录；不修改原有游戏逻辑、场景、角色美术和配乐。
- 三条核心时间规则保持原实现：普通地图战斗继续主时间、洞穴暂停主时间、最终 Boss 独立计时。
- 实际录制检查和最终编码检查见 `validation.json`。宣传片录制不等于洞穴回归测试、手机性能验证或最终视觉批准。
- 尚未完成：真人观看与手机平台发布后画质检查；没有擅自上传或发布。
