# 一炷江湖 · 90 秒竖屏宣传片 v03

用户批准：“没问题，开始制作”。本版使用 HyperFrames 制作；30 秒 v02 保留，说明见 archive-v02/README.md。

## 观看

- 成片：`一炷江湖_90秒宣传片_v03.mp4`，90 秒、1080×1920、30 fps。
- 0–36 秒：8 张全新 AI CG 原画，镜头运动、烟雾、雨、火星、狐火路径、刀光与冲击特效。
- 36–82 秒：46 秒新录实机，探索 12 秒、两场交锋剪辑 10 秒、武学选择与后续行动 10 秒、连续九尾战 14 秒。
- 82–90 秒：片名与再战邀请，两拍收尾。
- 原创 120 BPM 配乐和独立音效轨，无旁白。

AI 段是动态分镜，人物保持原画姿态。实机来自真实 Unity Editor Play Mode，采用受控输入与正常武学接口准备构筑，为多场取景剪辑，不是一次自然完整通关。无敌人／伤害数值覆盖，未用循环或慢放补时长。CG 山城与招式属于动画演绎。

## 文件与改动

- 新增 `hyperframes-v03/index.html`、`build_film.py`：完整 HyperFrames/GSAP 时间轴。
- 新增 `hyperframes-v03/assets/`、`asset-ledger.json`：本地画面、字体、GSAP、实机、BGM、SFX 与来源／哈希。
- 新增 `hyperframes-v03/compose_score.py`、`encode_gameplay.py`：音乐生成、实机编码脚本。
- 新增 `Assets/Scripts/Debug/TrailerLongCapture.cs` 及 Unity `.meta`：Editor 专用录制菜单，不进入玩家构建。
- 新增 `cg-v03/capture*`：原始 PNG 帧及录制报告，正式选择记录见视频规格。
- 更新 `video-spec.md`、`shot-manifest.json`、`preview-board.md`、README：批准记录与实际成片说明。
- 原提案保存在 `cg-v03/approved-proposal/`；v02 工程与成片保持独立。

## 播放与重渲染

直接打开 MP4 即可观看，无需 Unity。

在 `hyperframes-v03/` 中：

```sh
PATH="$PWD/.bin:$PATH" npm run check
PATH="$PWD/.bin:$PATH" npx --yes hyperframes@0.8.58 preview --background --port 3018
PATH="$PWD/.bin:$PATH" npx --yes hyperframes@0.8.58 render --output '../一炷江湖_90秒宣传片_v03.mp4' --fps 30 --quality delivery --workers 2 --video-frame-format png
```

Studio：`http://localhost:3018/#project/hyperframes-v03`。`build_film.py` 可重建 HTML；`compose_score.py` 需要 numpy，并复用仓库 Tools/Audio 下的乐器合成函数。素材已完整打包，重新渲染不依赖音乐脚本。

当前 `.bin/` 链接到本机 FFmpeg 和 arm64 FFprobe；迁移机器需重新配置这两个可执行文件，并安装 Node 22+ 与 Chrome。`assets/` 必须与 HTML 一起保留（部分图片／字体超过内联阈值）。

## Unity 录制复现

打开已保存且无未保存修改的 MainPrototype 场景，退出 Play Mode。依次执行：

1. `37 MiniGame/Promotional/Capture 90 Second Trailer`
2. `37 MiniGame/Promotional/Capture V03 Combat Pickup`
3. `37 MiniGame/Promotional/Capture V03 Boss Pickup`

各次完成后自动退出 Play Mode；在 hyperframes-v03 中执行 `python3 encode_gameplay.py base`、`python3 encode_gameplay.py combat`、`python3 encode_gameplay.py boss`。

无需手动绑定 GameObject 或 Inspector。未修改场景、Prefab、运行时 UI、字体或战斗规则。普通碰怪战斗仍消耗主时间；洞穴暂停逻辑未改（本次未录制洞穴）；Boss 独立计时。录制结束恢复 Game View 尺寸、后台运行和 Time.captureFramerate，不保存临时游戏对象。

## 验证与边界

- 三次录制报告 success=true，runtimeErrors 为空；录制后退出 Play Mode，场景保持未修改。
- HyperFrames check：lint/runtime/layout/motion 零错误、零警告；12 项文字对比度通过。镜头越界裁切与短溶接文字重叠是已审查的信息提示。
- 渲染前已检查 CG 与实机联络表；成片解码、音视频规格、抽帧和音量结果见 `hyperframes-v03/validation.json`。
- AI 画面原始分辨率约 941×1672；实机原始 720×1280，合成输出 1080×1920。
- 交付包含成片与源工程；抖音／视频号上传、平台压缩和手机主观观感／听感验收未执行。
