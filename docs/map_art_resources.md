# 大地图美术资源候选

## 当前项目状态

- 当前 `MainPrototype` 已经导入 KayKit Medieval Builder Pack 的一组低多边形模型。
- 已有可复用内容包括房屋、集市、矿洞、城门、城墙、瞭望塔、桥、井、树和岩石。
- `Walkable Ground` 与全部主地图道路已经使用同一套世界坐标平铺哑光 Shader，缩放时不会拉伸纹理。
- 草地使用 `mat_mainmap_grass`，道路使用 `mat_mainmap_dirt`，并统一为灰绿草地、暖灰褐道路与柔和暖光。
- 地图正式采用 [原创武侠 HD-2D 美术规范](art_style_guide.md)：像素角色、3D 微缩地形与 2D 像素风景切片组成有意的混合媒介。

## 主世界 HD-2D v01

- 已生成并导入山河远景、竹林、松石、水面、雾带和光束 6 类原创资源，Unity 交付目录为 `Assets/Art/Generated/Environment/HD2D/`。
- 完整生成提示词保存在 `ArtSource/Raw/Environment/HD2D/prompts_hd2d_main_world_v01.md`；原图与归一化母版分别位于 `ArtSource/Raw/Environment/HD2D/`、`ArtSource/Normalized/Environment/HD2D/`。
- `Tools/ArtPipeline/prepare_hd2d_world_art.py` 负责统一尺寸、透明裁切、水面无缝化、雾带与光束生成，并输出水面 2×2 平铺预览。
- `Assets/Art/Generated/Environment/Shaders/HD2DWaterSurface.shader` 提供低反射、双层缓慢流动的青瓷色水面。
- `37 MiniGame > Apply HD-2D Main World Art` 会重建 `HD2D Main World Art` 场景根节点，生成弯曲河道与土岸、远景地台、像素景观切片、雾层、地标暖光，并调整主地图镜头至更低的斜俯视角。
- 河面现在是不可通行地形：沿水面生成低成本分段 `BoxCollider`，只在西林、中央驿路和东郊三座 KayKit 桥梁处留出通行缺口；桥面不抬高玩家的固定移动平面。
- `MainMapRiverLayout` 统一保存河道中心线、宽度和桥点。场景应用时自动迁出压在水面的怪物、洞穴、拾取物与指定景观资产；`37 MiniGame > Validate Main World River Crossings` 可检查桥、阻挡段与遭遇物安全距离。
- 西、中央、东三条过河主路已对齐对应桥位；远东与远西外圈道路在河岸处分段，西林支路在河前收口，不再出现路面穿过不可通行水域的错误引导。
- 山河远景由北侧单张切片升级为水平无缝的 `2048 × 1024` 全景天空盒，四面共享同一套低对比远山与雾色，不再在东西边缘露出竖直贴图墙。
- 远景地台与气氛装饰仍不带 Collider，不改变原有地图外边界和遭遇触发器。
- 横屏 Unity Game 视图已完成构图与可读性检查；当前状态为 `InEngineQA`，仍需在目标机型完成横竖屏连续试玩后进入 `Approved`。

## 主地图地面 v02

- 正式候选草地：`Assets/Art/Generated/Environment/tex_env_mainmap_grass_albedo_1024_v02.png`。
- 正式候选土路：`Assets/Art/Generated/Environment/tex_env_mainmap_dirt_albedo_1024_v02.png`。
- 两张资源均由内置 `imagegen` 生成，完整提示词记录在 `ArtSource/Raw/Environment/prompts_mainmap_ground_v02.md`。
- 原始母版位于 `ArtSource/Raw/Environment/`；归一化母版与 2×2 平铺预览分别位于 `ArtSource/Normalized/Environment/`、`ArtSource/Previews/Environment/`。
- 使用 `Tools/ArtPipeline/normalize_mainmap_ground.py` 统一输出为 `1024 × 1024` RGB、降低饱和度与对比度，并通过错位融合处理接缝。
- 无缝检查结果：草地左右 / 上下边缘比为 `1.124 / 1.125`，土路为 `1.067 / 1.132`，均低于流水线门槛 `1.35`。
- Unity 已通过 `37 MiniGame > Refresh Main Map Ground` 接入；横屏 Play Mode 已确认道路引导、角色可读性、世界坐标平铺和镜头内接缝，当前状态为 `InEngineQA`。

## 推荐候选

| 优先级 | 资源 | 用途 | 页面标注授权 | 当前状态 |
| --- | --- | --- | --- | --- |
| 1 | [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) | 树木、石头、植被、地貌和自然区域补充 | CC0 | 已找到，未下载、未导入 |
| 2 | [Kenney Castle Kit](https://www.kenney.nl/assets/castle-kit) | 城墙、城门、塔楼和边关区域 | CC0 | 已找到，未下载、未导入 |
| 3 | [Quaternius Medieval Village MegaKit](https://quaternius.com/packs/medievalvillagemegakit.html) | 更丰富的中世纪村庄建筑与模块化场景 | CC0；页面提供免费内容，完整源文件有会员版本 | 已找到，未下载、未导入 |
| 4 | [KayKit Medieval Builder Pack](https://kaylousberg.itch.io/kaykit-medieval-builder-pack) | 与当前地图风格最接近的建筑和场景延续 | CC0；页面为自由定价 | 项目已有精选模型 |

## 建议接入顺序

1. 先用 Kenney Nature Kit 补足森林、岩地和道路两侧的自然装饰。
2. 再用 Kenney Castle Kit 加强北门、南关和地图边界的视觉识别。
3. 如果需要把村落做得更丰富，再选择性接入 Quaternius Medieval Village MegaKit。
4. 下载后保留资源包自带的授权文件，并先做一个小区域风格测试，再批量替换当前原型资源。

## 约束

- 不在用户确认前购买付费资源或会员内容。
- “风格匹配”“授权清晰”“已经导入”分别记录，不把找到资源写成已经接入。
- 优先保持同一区域内模型比例、材质色调和轮廓语言一致，避免混用过多不同作者的资源。
