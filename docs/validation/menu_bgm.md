# 首页 BGM

## 当前版本：武侠朋克 + DJ（v02）

用户进一步明确风格后，首页默认曲目已改为
`Assets/Resources/Audio/Menu/bgm_menu_wuxia_punk_dj_60s_v02.wav`。

- 原创本地合成，128 BPM，32 小节／60 秒；古筝感五声旋律、笛箫、失真弦乐短奏，
  配四拍底鼓、反拍电子低音、踩镲、电子主奏及随底鼓起伏的音量。
- 0–7.5 秒：旋律与律动；7.5–30 秒：第一段完整节奏；30–37.5 秒：笛箫过门与蓄力；
  37.5–60 秒：第二段高潮并返回开头。无人声、无第三方录音。
- 峰值 -3 dBFS，RMS -13.42 dBFS，未削波；循环边缘 3 ms 微淡化，首尾采样差为零。
- 生成：`python3 Tools/Audio/generate_menu_wuxia_punk_dj.py`，复用已有合成乐器函数。
- 保持 Streaming/Vorbis 0.7、2D 循环声源、音量 0.32、淡入淡出与音乐开关。
  无需 Editor 手动绑定，不改运行时中文、玩法、计时规则或场景文件。
- v02 专项 Play Mode 结果见 `menu_bgm_dj_playmode.json`；主观听感与手机外放尚待人工验收。

旧舒缓版连同 GUID 移至 `Assets/Audio/Generated/Music` 留作备选，不再从 Resources 随默认首页加载。
下面保留第一版的制作与验证记录，v01 的结果不作为 v02 听感验收。

## 第一版：舒缓武侠 BGM（已替换）

2026-09-10：首页与关卡选择在 `Ready` 阶段播放独立曲目，复用现有背景音乐开关。

- 曲目（现已归档）：`Assets/Audio/Generated/Music/bgm_menu_misty_mountains_loop_64s_v01.wav`。
- 原创本地合成：60 BPM、64 秒、D 宫五声音阶，古琴／古筝感拨弦与箫感旋律，无人声、无战鼓。
  音色为程序合成，不是实录民族乐器；没有使用第三方录音或现成曲目。
- 源工具：`python3 Tools/Audio/generate_menu_wuxia_music.py`，确定性生成，仅依赖 NumPy。
- 44.1 kHz 双声道 PCM 母版，峰值 -6 dBFS，RMS -18.59 dBFS，无削波；
  混响和乐器尾音绕回循环起点，接缝相邻采样差 0.00262，小于附近正常采样差峰值 0.01498。
- Unity 使用 Vorbis 0.7 质量、Streaming、不预加载，运行时以 2D AudioSource 循环播放。
- `MainMapMusicController` 自动加载资源、创建独立声源，无需修改场景或手动绑定。
  默认音量 0.32，首页 1.5 秒淡入、离开 0.65 秒淡出，进入游戏沿用已有地图配乐。
  开屏的全局音频暂停期间不播放首页音乐或推进淡入；返回首页后恢复首页曲目。

## 验证

Unity 6000.5.4f1、当前 MainPrototype 场景实际 Play Mode；通过 MCP 临时执行检查，未保存测试场景。
原始结果见 `menu_bgm_playmode.json`。本轮验证首页播放、关卡选择继续、64 秒循环回绕、音乐开关、
进入游戏淡出、返回首页，以及全局音频暂停。编译与运行时 Console 错误检查为零。

三条时间规则的实现未修改：普通战斗继续主时间，洞穴暂停主时间，最终 Boss 独立计时。
本轮检查了原 `GameFlowController.Update` 分支，未重跑完整战斗、洞穴和 Boss 回归。
未新增运行时中文文案。未更改既有场景文件；其他开发任务的未提交改动保留。

## 试听与复核

1. 打开 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode，等待或跳过品牌开屏。
2. 首页等待淡入，打开关卡选择，音乐应继续；在设置中关闭／开启背景音乐。
3. 开始游戏，首页曲目淡出；从设置返回首页，舒缓曲目重新淡入。
4. 首页停留超过 64 秒，试听循环接缝。真实设备扬声器音量与最终听感仍需人工验收。

状态：已生成、已导入、播放逻辑已通过 Editor Play Mode；未标记最终听感／设备验收通过。
