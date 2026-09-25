# 一炷江湖 · 竖屏宣传片 v02

> v03 更新：用户将目标调整为约 90 秒。新 CG 素材见 `cg-v03/`，根目录分镜文档已更新为新提案；本文的成片与渲染说明仍对应已交付的 30 秒 v02，原批准文档见 `archive-v02/`。

用户于 2026-09-22 批准当前关键帧与配乐制作：“按这版生成，可以加 bgm，都可以生成”。

## 成片

- `一炷江湖_竖屏宣传片_v02.mp4`：30 秒，1080×1920，9:16，30 fps。
- 0–6 秒：两张 AI 原画的动态分镜，包含缓推、前景雾、狐火微亮和剑光转场。
- 6–24 秒：沿用 v01 Unity Play Mode 的探索、精英战、武学选择与 Boss 实机剪辑。
- 24–30 秒：沿用片名、再战邀请与制作组片尾。
- 配乐：项目原创合成的武侠电子乐，已混入剑鸣与冲击音效；提取并复用 v01 混音，无重复叠加音乐。

AI 段为动态关键帧，不是连续人物肢体表演。实机来自受控的真实 Play Mode 录制，不是一次完整自然通关，也未为宣传改写伤害或敌人数值。

## 文件

- `hyperframes-runtime/index.html`：HyperFrames/GSAP 主时间轴，局部改动开场不必重做实机。
- `hyperframes-runtime/assets/`：完整本地图片、字体、GSAP、实机视频和混音素材。
- `hyperframes-runtime/DESIGN.md`：动画工程视觉规范。
- `video-spec.md`、`preview-board.md`、`shot-manifest.json`：本版需求、关键帧、用户批准记录。
- `hyperframes-runtime/check.log`：结构、运行、布局、动效和对比度检查。
- `hyperframes-runtime/snapshots/`：渲染前关键帧抽查。
- `hyperframes-runtime/.hyperframes/anim-map/animation-map.json`：动画轨迹审查。
- `validation.json`：最终媒体验证结果。

本次新增独立动画工程与成片，更新上述批准记录；未修改 Unity 场景、脚本或字体源资源。Unity Editor 无需手动绑定，三条核心时间规则保持不变。当前任务不包含上传平台或真机投放验收。

## 预览与重新渲染

在 `hyperframes-runtime/` 下执行：

```sh
PATH="$PWD/.bin:$PATH" npm run check
PATH="$PWD/.bin:$PATH" npx --yes hyperframes@0.8.58 preview --background --port 3017
PATH="$PWD/.bin:$PATH" npx --yes hyperframes@0.8.58 render --output '../一炷江湖_竖屏宣传片_v02.mp4' --fps 30 --quality high --workers 2 --video-frame-format png
```

Studio：`http://localhost:3017/#project/hyperframes-runtime`。直接播放上方 MP4 即可观看，无需 Unity。

当前机器 `.bin/` 链接到本地 FFmpeg 与项目安装的 arm64 FFprobe。迁移电脑后需安装 Node 22+、Chrome、FFmpeg/FFprobe，并执行 `npm ci`。所有实际成片素材已经本地化，不依赖在线字体或图床。

## 检查说明

最终 MP4 共 900 帧，完整音视频解码通过；AAC 48 kHz 双声道，峰值 -1.9 dBFS。8 个成片抽帧已检查。

`hyperframes check` 同时覆盖 lint、runtime、layout、motion 和 contrast。自动布局抽查无问题，开场、转场、武学界面、Boss 和片尾已检查画面。

旧插件的 animation-map 脚本依赖未单独发布的 `@hyperframes/producer`；本工程的 `scripts/animation-map.mjs` 保留原分析逻辑，使用本机 Playwright 适配读取 GSAP。其 paced-slow 是 3 秒镜头/雾层慢动，collision 是背景/雾层/溶接的有意叠层，offscreen 是剑光扫出画面；6.5–30 秒由实机视频继续运动，不能将 GSAP 无 tween 误判为画面静止。框架原生 Motion 检查通过。

大于 2 MB 的素材没有内联到 HTML，但均随工程放在 assets/ 下，已验证渲染加载。
