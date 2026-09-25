# 前景遮挡优化

第二关牌匾、木梁、铜字与第三关竹竿、竹节原先没有进入植被淡出逻辑。原有叶片中心还保留 12% 覆盖，多层竹林叠加后依然会挡住角色。

## 当前实现

- 两关场景 Shader 共用 `ForegroundOcclusion.cginc`。所有前景场景材质都参与局部遮挡处理，不再依赖 `_Foliage` 白名单，因此牌匾的底板、字和边框，以及整丛竹子能一起让开视线。
- 角色周围采用椭圆形视线开口，中心完整剔除遮挡像素，外围通过固定屏幕抖动渐变恢复。物体移出开口后恢复原样，保留区域外的场景轮廓。
- 按透视投影修正近处遮挡范围；横竖屏和望气扩视沿用现有镜头参数。默认在角色深度处水平半径 2 米、垂直半径 2.3 米。
- 开口跟随 `PlayerController.visualRoot` 的脚点，包含实际地形/桥面显示层抬升。低于脚点 0.12 米的表面保持完整，0.12–0.42 米之间渐变；人物后方的景物保持完整。
- 保留不透明材质的深度写入与排序，阴影投射不挖洞，不创建每个物体的材质副本，不扫描全场景 Renderer 或依赖植被碰撞体。
- `CameraFollow.Awake` 自动添加 `ForegroundOcclusion`，进入探索约 0.2 秒渐入。只在该相机的探索渲染期间设置参数，渲染结束/组件禁用/重新进入 Play Mode 时清理，战斗、洞穴与 Boss 阶段不启用。
- 不改模型、场景、碰撞、移动、镜头角度、FOV、UI 文案或玩法时间规则。旧环境可见性组件和 `_Foliage` 属性保留序列化兼容，两个更新后的 Shader 不再读取它们。

## 本次验证

Unity 6000.5.4f1 Editor 实际 Play Mode 验证通过 25 项检查，无运行时错误；两套 Shader 的导入编译均无错误。
报告见 [playmode_report.json](playmode_report.json)。

- 第二关主街南牌楼：横屏 960×540、竖屏 540×960，真实画面出现局部遮挡消隐。
- 第三关竹林：同样覆盖横竖屏，已逐图检查角色可见性。
- 四层实体叠放：角色中心 9×9 像素与没有遮挡物的参考图完全一致。
- 人物后方实体与脚点高度地板：开关效果的渲染逐像素一致。
- 普通战斗主时间继续、洞穴战斗主时间冻结、Boss 主时间冻结且独立时间增长，三条规则全部通过。
- 战斗、洞穴、Boss 和禁用相机跟随时，渲染准备阶段均没有残留开口；切到第三关自动接入有效。
- 结束后恢复原编辑场景、Game View 尺寸、时间倍率和后台运行设置，不保存测试场景、不改存档。

截图中的 `before` 表示**完全关闭遮挡处理**的对照，`after` 表示新效果；并非旧版算法与新版的逐像素比较。截图是相机渲染，不包含 HUD。

| 点位 | 关闭遮挡处理 | 开启遮挡处理 |
| --- | --- | --- |
| 第二关横屏 | [关闭](town_gate_landscape_before.png) | [开启](town_gate_landscape_after.png) |
| 第二关竖屏 | [关闭](town_gate_portrait_before.png) | [开启](town_gate_portrait_after.png) |
| 第三关横屏 | [关闭](bamboo_landscape_before.png) | [开启](bamboo_landscape_after.png) |
| 第三关竖屏 | [关闭](bamboo_portrait_before.png) | [开启](bamboo_portrait_after.png) |

## 运行与复验

无需创建 GameObject、挂脚本、拖资源或手动绑定 Inspector。打开 `MainPrototype` 或 `BambooValleyLevel`，进入 Play Mode 并开始探索即可生效。沿牌楼前后穿行、进入竹林边缘，确认人物和脚前路线可见、离开后景物恢复。

自动复验：停止 Play Mode，打开 `MainPrototype`，执行 `37 MiniGame/Validate Foreground Occlusion Play Mode`。测试会在 Play Mode 内加载第三关，结束返回原编辑场景。

人工补验：移动穿过开口边缘、桥上遮挡、望气扩视前后、横竖屏切换、暂停/恢复、重开及回首页。尚未进行手机/WebGL GPU 性能和实机动态观感验收，也未重新发布构建。

## 文件

修改：`Assets/Scripts/Camera/CameraFollow.cs`、第二关 `PingchuanTownSurface.shader`、第三关 `BambooValleyVertexSurface.shader`、`docs/gameplay_systems.md`。

新增：`Assets/Scripts/Camera/ForegroundOcclusion.cs`、`ForegroundOcclusion.cginc`、`Assets/Scripts/Debug/ForegroundOcclusionPlayModeProbe.cs` 及各自 `.meta`，本目录说明、报告和截图。
