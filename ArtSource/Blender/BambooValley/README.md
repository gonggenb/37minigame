# 竹影幽谷 · Blender 地图

依据用户提供的 `abd40112-e408-4289-8cfb-bc5ed03d6a52.png` 制作的可编辑场景。采用风格化三维模型还原参考图的区域布局、竹林山谷、灰瓦木构和暖色灯火；不是原图的照片级复刻。

## 打开与查看

直接用 Blender 打开同目录 `BambooValley.blend`。已在 Blender 5.2.1 LTS 中执行建模并保存，当前活动场景是 `Bamboo Valley | 竹影幽谷`；原默认 `Scene` 保留。

- 小键盘 `0`：进入总览相机；`Home` 或滚轮可调整视口。
- Outliner 中 `90 Cameras` 包含总览、正俯视、桥亭近景三台相机。选中相机后按 `Ctrl + 小键盘 0` 切换活动相机。
- `F12` 渲染当前相机。最终预览见 `BambooValley_Overview.png`、`BambooValley_Layout.png`、`BambooValley_Detail.png`。
- 集合 `01`–`20` 按地形、水体、道路、建筑和植被分组。网格按材质合并；需要修改单件时，在编辑模式选择相连几何，或用 `P > 按松散块` 分离。
- 参考图及战旗字体已经打包；材质由 Blender 程序化节点生成，无外部贴图依赖。

## 地图内容

米制单位，地形约 50 × 44 米，北为 `+Y`，竖直为 `+Z`。参考图没有尺寸标注，因此尺寸为本次制作假定值。

| 区域 | 位置约值 (X, Y) | 模型内容 |
| --- | --- | --- |
| 竹林入口 | (-17, -12) | 木牌坊、灯笼、弯曲石板路 |
| 溪流木桥 | (-8, -6) | 拱形木板、双侧护栏、桥头灯 |
| 中央亭台 | (0, -2) | 六角石台、六柱、翘角瓦顶、石桌木凳 |
| 戒哨营地 | (-15, 12) | 两顶营帐、篝火、战旗、木箱木桶 |
| 武馆旧院 | (0, 12) | 长屋脊灰瓦、木柱、格窗、双门、台阶、围栏 |
| Boss 比武场 | (15, 12) | 十二边台基、圆形石面、纹圈、周边石柱 |
| 隐藏山洞 | (20, -1) | 岩石拱口、短洞道、洞口灯、引入石板 |

另含营地支桥、竹林簇群、溪岸苔石、草叶、木车、路灯和分区战旗。入口—主桥—中央亭—武馆—比武场构成视觉主线，营地与山洞通过支路连接。

## 文件与重建

本任务只新增此目录，没有修改 Unity 脚本、场景、Prefab 或运行时中文文案。

- `BambooValley.blend`：可编辑场景主文件。
- `01_foundation.py`：材质、基础网格、地形、河道、石板路。
- `02_landmarks.py`：木桥、亭台、武馆、比武场、山洞。
- `03_dressing.py`：灯笼、旗帜、营地、道具、竹林、草石。
- `04_presentation.py`：相机、灯光、资源打包及保存。
- `build_scene.py`：顺序执行上述建模脚本。会替换同名生成场景，重新运行前请另存手工修改版。
- `render_views.py`：渲染三个检查镜头，不覆盖 `.blend` 内相机设置。
- `scene_manifest.json`：对象、面数、竹竿数等场景统计。

在 Blender Python 控制台重建：

```python
p='/Users/gongyuyang/Documents/37minigame/ArtSource/Blender/BambooValley/build_scene.py'
exec(compile(open(p).read(),p,'exec'))
```

## 验证与范围

已执行生成脚本、保存 `.blend`，并通过 Cycles 渲染总览、俯视及近景做视觉检查。首轮发现亭台和洞口被竹林遮挡，已扩大相关空地；武馆屋顶改为长屋脊。统计详见 `scene_manifest.json`。

该 Blender 母版已另行导出并接入 Unity 第三关 `Assets/Scenes/BambooValleyLevel.unity`。最新接入、运行方式及 Play Mode 证据见项目 `docs/bamboo_valley_level3.md`。本文件上方描述的是母版建模流程；Unity 使用独立 FBX、顶点色材质、环境 Prefab 和碰撞体，不会直接运行 Blender 程序化材质。

`export_to_unity.py` 使用 Blender 后台模式读取母版，简化叶片、按空间块分组并输出 Unity FBX；不会覆盖 `.blend` 母版。游戏专用材质和第三关通过 Unity 菜单 `37 MiniGame/Build Bamboo Valley Level 3` 创建。
