# Q 版水墨国风美术完整替换

## 需求描述

将 `Assets/Art/Q版水墨国风（行侠仗义五千年）/` 作为只读测试素材库，把当前由 Generated、KayKit、TinySwords、CraftPix 等混合资源组成的画面，迁移为统一的 Q 版水墨国风运行时美术。

本次范围覆盖主地图、玩家、普通敌人、Boss、世界物品、洞穴入口、战斗画面、菜单、HUD、升级、洞穴、结算、图标和特效；音频不在本轮替换范围内。

新画面以竖屏为主设计，同时提供独立横屏布局。游戏运行时根据设备或窗口宽高比动态切换两套布局，切换时不得重启单局或丢失游戏状态。

## 实现方案

### 已确认现状

- 素材库包含 3,216 张图片和 122 个音频文件，其中图片当前全部按普通 Texture 导入，`Sprite Mode=None`。
- `MainPrototype_Architecture.unity` 和 Architecture Prefab 对该素材库的直接引用为 0。
- 当前场景和 Prefab 仍直接引用至少 66 项 `Assets/Art/Generated`、`ThirdParty/KayKitMedieval`、`ThirdParty/TinySwords` 和 `ThirdParty/CraftPixEnemyVariety` 资源。
- 当前主地图采用“3D 平面与碰撞 + KayKit 模型装饰 + Billboard Sprite 角色”的混合结构；美术替换保留碰撞、出生区域、流程组件和玩法脚本，只重建视觉层。
- 当前架构场景已有完整 uGUI 与 Presenter 绑定，但只有一套横屏布局；README 中“uGUI 尚未创建”的描述已经过时，完成本需求时必须同步修正。

### 资源分层

```text
Assets/Art/Q版水墨国风（行侠仗义五千年）/   只读素材库，不修改、不重命名、不建立运行时直接引用
Assets/Art/RuntimeInkArt/                    精选运行时副本
  Environment/                               地图、地标、洞穴入口和世界物品
  Characters/Player/                         玩家逐帧图
  Characters/Enemies/                        敌人和 Boss 逐帧图
  UI/Shared/                                  通用面板、按钮、进度条
  UI/Portrait/                                竖屏专用背景与构图资源
  UI/Landscape/                               横屏专用背景与构图资源
  Icons/                                     武学、装备和状态图标
  Effects/                                   战斗命中、水墨遮罩和提示特效
  Materials/                                 运行时地图与透明 Sprite 材质
Assets/Prefabs/InkArt/                        只引用 RuntimeInkArt 的 Prefab 副本
Assets/GameData/Runtime/InkArtCatalog.asset   稳定 ID 到 Sprite/帧序列的映射
Assets/GameData/Runtime/InkSpawnPrefabCatalog.asset
Assets/Scenes/MainPrototype_InkArt.unity      独立水墨场景
```

### 首轮精选映射

| 游戏职责 | 素材库来源 | 运行时目标 |
|---|---|---|
| 主地图底图 | `场景背景/sactx-0-2048x2048-ETC2-Textures_Map_shaolin-cc8e713e.png` | `Environment/map_main.png` |
| 横屏菜单/战斗背景 | `场景背景/beijing_001.png` | `UI/Landscape/background_main.png` |
| 竖屏菜单背景 | `场景背景/zjm_bg.png` | `UI/Portrait/background_main.png` |
| 洞穴背景 | `场景背景/wx_bg.png` | `UI/Portrait/background_cave.png` |
| 地标装饰 | `taohuagu_dibiao.png`、`wudangpai_dibiao.png`、`gaibang_dibiao.png`、`mizong_dibiao.png` | `Environment/Landmarks/` |
| 玩家 | `动画序列/nvjianke1-idle_00.png`、`nvjianke1-run_0..5.png` | `Characters/Player/` |
| 山贼 | `shanzei-idle_0.png`、`shanzei-run_0..4.png` | `Characters/Enemies/Bandit/` |
| 竹林傀儡 | `jiguanrenou-idle_00.png`、`jiguanrenou-run_0..4.png` | `Characters/Enemies/Bamboo/` |
| 墨影狼 | `yegou1-idle_00.png`、`yegou1-run_0..4.png` | `Characters/Enemies/InkWolf/` |
| 石臂猿/Boss | `xingxing-idle_00.png`、`xingxing-run_0..5.png` | `Characters/Enemies/StoneApe/` |
| 宝箱 | `UI界面/baoxiang.png` | `Environment/treasure.png` |
| 药草 | `UI界面/hulu.png` | `Environment/herb.png` |
| 洞穴入口 | `场景背景/rukou1.png` | `Environment/cave_entrance.png` |
| 通用面板 | `UI界面/common_board_xinxi_00.png` | `UI/Shared/panel.png` |
| 通用按钮 | `UI界面/common_btn_big_yellow.png` | `UI/Shared/button_primary.png` |
| 顶部水墨框 | `UI界面/common_board_top_shuimo_01.png` | `UI/Shared/top_frame.png` |
| Boss 框与进度条 | `UI界面/zd_board_boss.png`、`zd_board_jindutiao_01.png` | `UI/Shared/` |

### 运行时结构

- 新增纯 C# `PresentationLayoutResolver`，宽大于高时返回 Landscape，否则返回 Portrait；正方形按竖屏主设计处理。
- 新增 `AdaptivePresentationController`，监听 `Screen.width/height` 变化，动态切换 `PortraitCanvas` 与 `LandscapeCanvas`，并同步相机 FOV 和跟随偏移。
- 两套 Canvas 各自包含完整的 `MainMenuView`、`HudView`、`BattleView`、`CaveView`、`LevelUpView`、`ResultView` 和 `GameUiPresenter`；只有当前方向的 Canvas 激活。
- `InkArtCatalog` 负责稳定 ID 到玩家/敌人帧、世界 Sprite、UI Sprite、武学图标和装备图标的映射，业务代码不引用素材文件名。
- 新增 Editor 自动化菜单 `37 MiniGame/Ink Art/Rebuild Ink Art Scene`：复制精选资源、统一 TextureImporter、创建 Catalog、复制 Architecture 场景、创建 InkVisualRoot、生成 InkArt Prefab、绑定双布局并保存新场景。
- 新场景禁用旧 3D 美术 Renderer 和旧混合美术根节点，但保留地面/道路/边界 Collider、出生区域、Architecture 管理器和三条时间规则相关逻辑。
- 原 `MainPrototype.unity`、`MainPrototype_Architecture.unity`、原始 Prefab 和只读素材库均保留，用于回退与对照。

### Unity MCP 与 Skills 使用边界

- MCP 先读取 `mcpforunity://editor/state`、场景层级和组件资源，确认 Editor 空闲及目标对象实例 ID。
- 脚本和自动化工具通过文件补丁维护；场景/Prefab/Inspector 由 Editor 自动化菜单生成，再通过 MCP `execute_menu_item` 执行。
- 执行自动化后等待编译完成，读取 Console 错误，再用 MCP 查询新场景层级、Prefab 依赖和组件引用。
- 使用 MCP 截取竖屏与横屏 Game View 验收图；不通过电脑控制工具操作 Unity Editor。
- 代码行为遵循 TDD：先写布局解析与 Catalog 查找测试并确认失败，再实现最小代码；场景自动化通过 EditMode 结构测试固定。

### 验收标准

- `MainPrototype_InkArt.unity` 可独立运行，旧场景未被覆盖。
- 新场景与 InkArt Prefab 不直接依赖 `Assets/Art/Generated` 或 `Assets/Art/ThirdParty` 的视觉资源。
- 竖屏与横屏分别拥有完整布局，运行中改变宽高比后立即切换且游戏状态不重置。
- 玩家、四类生成敌人、宝箱、药草、洞穴入口、战斗背景、HUD、菜单、升级和结算均使用 `RuntimeInkArt` 资源。
- TextureImporter 按用途设置为 Sprite，关闭 Mipmap，透明图启用 Alpha，角色使用统一 PPU，UI 可拉伸资源设置九宫格边界。
- Unity Console 无新增编译错误或 MissingReference；EditMode 测试全部通过。
- 普通战斗主计时继续、洞穴暂停主计时、Boss 独立计时三条规则保持不变。

## 执行清单

- [x] 增加布局解析和 Catalog RED 测试并验证失败。
- [x] 实现 `PresentationLayoutResolver`、`InkArtCatalog` 与 `AdaptivePresentationController`。
- [x] 增加水墨场景结构和资源依赖 RED 测试并验证失败。
- [x] 实现精选资源复制、导入设置、InkArt Prefab/Catalog 和水墨场景 Editor 自动化。
- [x] 通过 Unity MCP 执行自动化并检查 Console。
- [x] 验证竖屏、横屏、运行时切换和全部核心玩法阶段。
- [x] 更新 README 与本计划变更记录。

## 变更记录

### 2026-07-27 - 建立美术替换方案与执行基线
- **新增文件**：`plans/plan-ink-art-replacement.md`
- **变更内容**：完成素材库审计，确定只读源资产、精选运行时副本、独立水墨场景、InkArt Prefab/Catalog、竖横双布局和 MCP 自动化方案。
- **关联说明**：本需求不修改三条核心时间规则；旧场景、旧 Prefab 和原素材库作为回退基线保留。

### 2026-07-27 - 重建水墨场景并恢复战斗表现引用
- **修改文件**：`Assets/Scenes/MainPrototype_InkArt.unity`、`Assets/GameData/Runtime/**`、`Assets/Prefabs/InkArt/**`、`Assets/Art/RuntimeInkArt/**`、`README.md`、`plans/plan-ink-art-replacement.md`
- **变更内容**：通过水墨场景自动化重新生成运行时资源、Catalog、Prefab 和双布局 UI，修复战斗舞台 Catalog 引用缺失导致只显示气血 UI 的问题。
- **关联说明**：水墨专项测试 `5/5`、全量 EditMode `33/33` 通过；普通战斗 Play Mode 已确认全屏背景、玩家和敌人 Sprite 正常显示。竖横屏动态切换与其余全部玩法阶段仍需后续完整试玩验收。

### 2026-07-27 - 完成双布局、运行态与全量回归验收
- **修改文件**：`README.md`、`plans/plan-ink-art-replacement.md`
- **变更内容**：通过 Unity MCP 在同一局内将 Game View 从 750×1338 切换到 1920×1080，确认竖屏/横屏 Canvas 互斥激活且 `RunManager` 状态不重置；重新验证主地图、普通战斗、洞穴和 Boss 阶段，并补充运行与目录说明。
- **关联说明**：全量 EditMode `33/33` 通过；洞穴主时间保持 `59.1503→59.1503`，Boss 主时间保持 `59.1503→59.1503` 且 Boss 独立时间 `0→1.5589`。Console 未出现业务脚本、Sprite、字体或空引用运行时错误；仍可见的三条日志均来自 Unity Inspector 对自动化已销毁选择对象的编辑器残留引用。
