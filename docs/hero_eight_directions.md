# 主角八方向移动资源接入

2026-09-08。基于用户提供的八方向图，生成 8 条完整跑步循环，共 64 帧；另从原图提取 8 个方向的站立姿势。已替换主地图、新手关、竹谷及洞穴探索中的运行时主角移动显示。

## 素材与适配

- 原创作使用内置 `image_gen`，每方向整条生成；原图、生成原稿及实际提示词保存在 `ArtSource/HeroEightDirections/`，提示词见 `prompts.json`。
- 游戏资源：`Assets/Resources/Characters/HeroEightDirections/`。8 张跑步条带（每张 2048×256）和 8 张站立图（每张 256×256），均为真正 RGBA 透明 PNG。
- 图集帧从左向右播放，8 帧循环，基础 12 FPS。方向为右、右上、上、左上、左、左下、下、右下；使用独立绘制的方向，不依赖左右翻转。
- 单条动作使用同一个缩放系数，统一腰部水平中心、脚底锚点；站立高度统一到 192 像素。源图生成的灰白棋盘背景已清理，白色衣领、袖口和靴带保留。
- 单帧 256×256，脚底位于自上向下第 223 像素（零起始）；Unity Pivot `(0.5, 0.125)`，PPU 160，Full Rect，Point，Clamp，无 Mipmap、无压缩。
- 主地图沿用原有横屏 1.82、竖屏 1.5 的缩放设置以及相机、物理移动和桥面/地形抬升。新脚底 Pivot 配套清零旧素材的基础视觉 Y 偏移，并在地面阴影初始化前完成。
- 地图步频跟随摇杆幅度和角色当前移速；转向保留步态进度，边界有 5° 滞回，松开输入后立即保持最后方向的站立姿势。
- 洞穴共用同一资源库，每帧仅推进一次动画时钟，商店或战斗打开时停止走动；活动边界、角色大小、姓名和底部提示/退出按钮为横竖屏分别预留空间。

## 文件变更

修改：

- `Assets/Scripts/Player/PlayerController.cs`：暴露二维移动输入与步频比例，适配脚底锚点及初始化顺序；保留此前地形跟随改动。
- `Assets/Scripts/Visual/SpriteFrameAnimator.cs`：主角使用八方向播放，其他角色继续使用现有动画路径。
- `Assets/Scripts/Cave/CaveRoomController.cs`：洞穴八方向、停步/弹窗状态及活动区域适配。

新增：

- `Assets/Scripts/Visual/HeroDirectionalAnimation.cs`：共享资源加载、方向解析与独立播放时钟。
- `Assets/Editor/HeroDirectionalArtImporter.cs`：固定导入规格和切帧菜单；重导入保留 Sprite ID。
- `Assets/Scripts/Debug/HeroDirectionalPlayModeProbe.cs`：仅 Editor 中编译的可复跑集成验证。
- `Tools/ArtPipeline/prepare_hero_eight_directions.cjs`：确定性的透明清理、切分、锚点归一和预览导出，依赖 Node.js 与 `sharp`。
- `Assets/Resources/Characters/HeroEightDirections/`：16 张 PNG 及 Unity `.meta`。
- `ArtSource/HeroEightDirections/`：参考图、8 张生成原稿、提示词和归一化记录。
- `docs/validation/hero_eight_directions/`：循环 GIF、帧总览、资产/导入/Play Mode 报告以及三张地图和洞穴的横竖屏截图。

## 运行与复测

无需手动创建 GameObject、挂脚本或拖拽 Inspector 引用。现有主角动画组件和洞穴控制器会自动加载 Resources 中的八方向资源，三张场景均已验证绑定。

1. 用项目的 Unity 6000.5.4f1 打开任一现有场景并进入 Play Mode，正常开始游戏。
2. WASD/方向键或虚拟摇杆控制移动；检查斜向、方向边界、慢推摇杆、加速和松手停步。
3. 切换横竖屏，进入洞穴检查主角、姓名、商店和退出按钮；检查桥面与地形上脚底和阴影。
4. 自动复测：菜单 `37 MiniGame/Validate Hero Eight Directions Play Mode`。测试使用临时输入、禁用场景碰撞的移动夹具及战斗夹具，结束自动退出 Play Mode，并恢复原场景和 Game View 尺寸。测试内的 600 秒计时仅为夹具，不写回场景。
5. 如重新制作或丢失切帧数据，运行 `37 MiniGame/Art/Reimport Hero Eight Directions`；提交 PNG 时一并提交 `.meta`。

## 验证与状态

- 资产检查：16 张 RGBA 纹理、72 个帧；每个方向的 8 张移动帧内容均不同，脚底一致，四边无裁切。
- Unity 导入检查：72 个 Sprite 的数量、PPU、Pivot、Full Rect、Point、无压缩/无 Mipmap 全部符合规格。
- Play Mode：三张地图各 8 个方向均实际移动并播放完整 8 帧，松手保留对应朝向；960×540 与 540×960 均应用正确缩放；脚底匹配地形高度，已有地面阴影匹配相同高度。原场景未配置地面阴影的关卡沿用其配置。洞穴八方向、停步、商店暂停和横竖屏截图已检查。
- 三条核心时间规则保持不变，并通过战斗夹具复核：普通战斗继续消耗主时间；洞穴探索与洞穴战斗暂停主时间；最终 Boss 使用独立计时。
- 状态：`Generated → Normalized → Imported → InEngineQA`。最终审美确认及手机真机触控/性能体验尚未标记 `Approved`；本轮未重新发布 WebGL 构建。攻击动作和 UI 头像沿用其现有资源。

检查证据：`asset_report.json`、`unity_import_report.txt`、`playmode_report.json` 位于上述验证目录。帧预览按“下、右下、右、右上、上、左上、左、左下”顺序排列。
