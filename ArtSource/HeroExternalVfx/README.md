# A1 / B1 / C1 外置特效动画源文件

用户已选择推荐的 A1 月牙剑气、B1 前冲掌劲、C1 赤金斜劈，并授权制作动画和接入 Unity。
使用内置 `image_gen`，完整输入与提示词见 `prompts.json`；没有调用外部 API CLI。

- `references/`：用户确认的三个候选对照图；每图只选左侧设计。
- `raw/`：新生成的三张透明 RGBA 六帧原图，1536×1024，三列两行；原始 Alpha 保留。
- Unity 图集：`Assets/Resources/Effects/HeroExternal/`。
- 归一工具：`Tools/ArtPipeline/prepare_hero_external_vfx.cjs`。每张等分六格，所有格子使用同一中心
  与 224/512 缩放，放入 256×256 单帧，最近邻缩放；不逐帧裁切重心，不重新绘制美术。
- 验证、深浅底接触表与动图：`docs/validation/hero_external_vfx/`。

运行：设置 `NODE_PATH` 指向含 sharp 的 Node 模块目录，然后执行
`node Tools/ArtPipeline/prepare_hero_external_vfx.cjs`。
随后在 Unity 中执行 `37 MiniGame/Art/Reimport Hero External VFX`。
18 帧全部检查透明区、非空内容、边缘裁切和重复帧。

动图为 12 FPS 的素材审阅循环，游戏中每次只播放一遍，并随出招时长压缩。游戏内核对入口为
`37 MiniGame/Validate Hero Attack Pack Play Mode`，具体状态以验证报告为准。
