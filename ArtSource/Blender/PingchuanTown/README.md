# 平川山镇 · Blender 大地图

依照用户提供的六张概念图制作的独立三维环境。第一张确定整体分区，后五张提供建筑、道具与区域细节。概念图中的文字作为视觉参考。后续根据用户明确要求，已将游戏版接入第二关；关卡功能来自用户的替换请求。

## 打开与查看

用 Blender 5.2 打开 `PingchuanTown_v01.blend`，选择场景 `Pingchuan Town | 平川山镇 · 大地图`。

- 单位：米；北向：+Y；向上：+Z。主体地图设计范围约 240×220 米；外围山景延伸在此范围之外。
- 默认相机：`CAM 01 / full valley overview`。
- 数字键盘 0 进入相机视图；从 Outliner 选择其他相机后用 Ctrl+数字键盘 0 将它设为当前相机。
- 相机 02 是北向俯视图；03–08 依次是山镇、洞穴、营地、关隘、练武场、入口。
- `previews/01_Preview.png` 至 `08_Preview.png` 是真实 Blender 渲染，无后期绘制替代模型。
- Image Editor 中的 `Pingchuan reference 01` 至 `06` 是打包进文件的原始参考图。
- 六张参考图与中文字体已打包，打开模型不依赖 Downloads 中的图片。

## 已建内容

| 区域 | 内容 |
|---|---|
| 中央山镇 | 25 栋独立分组建筑，双层客栈、会馆、沿街商铺、后巷民居、门楼、9 个市场摊位、运货车、窗格、招牌、茶具、院落与药圃 |
| 山洞入口群 | 5 处岩洞，拱形洞口、短距离内部通道、支撑木构、入口石阶、火灯、戒字旗、残墙和弃车 |
| 山脚营地 | 大型主帐、6 座营帐、瞭望塔、尖木栅栏、入口、火堆、长凳、武器架、物资堆与马车 |
| 北方关隘 | 设有开口的石木关门、残墙、瞭望塔、守备帐、路障、界碑和戍字旗 |
| 大平原 | 带入口缺口的练武围栏、草人、木桩、箭靶、木台、兵器架、茶棚和瞭望设施 |
| 地形环境 | 连续盆地、区域干道及支路、河流、穿镇水渠、8 处桥梁、松林、竹丛、草叶、花丛、河岸石与外围山脉 |

细节采用实际几何：重叠瓦片与连续屋面底层、翘檐、屋脊、斗拱构件、栏杆、窗格、木箱板条、桶箍、车轮辐条、帐篷绳索和部分补丁。地表、木材、岩石和布料使用 Blender 程序化材质。树木与山体是风格化模型，尚未达到参考图的写实美术精度。

## 文件组织

Blender 源文件保存在本目录；Unity 导出与第二关接入文件见下方验证记录：

- `PingchuanTown_v01.blend`：可编辑主文件。
- `geometry.py`、`01_foundation.py` 至 `08_cave_shells.py`：可复用几何函数、地形、建筑模块、分区、植被、灯光、细化与检查脚本。
- `build_scene.py`：完整重建入口。会写入同名 v01 文件；重建前先另存手工改动。
- `render_views.py`：批量渲染相机。
- `manifest.json`：实际场景统计、地标坐标、检查结果与未完成的验证项目。
- `previews/`：总览、俯视及区域近景。

源脚本沿用当前仓库绝对路径；迁移到另一台机器时，先调整 `01_foundation.py` 的输出路径、字体路径及 `05_presentation.py` 的参考图路径。已保存 `.blend` 的打开和编辑不依赖这些源路径。

重建命令（当前 Mac 仓库）：

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python ArtSource/Blender/PingchuanTown/build_scene.py
```

渲染命令：

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Blender/PingchuanTown/PingchuanTown_v01.blend --python ArtSource/Blender/PingchuanTown/render_views.py -- 01 02 03 04 05 06 07 08
```

## 验证与边界

已进行 Blender 建模、保存、网格有效性检查与多机位渲染检查。检查包括空网格、非有限坐标、缺失材质、零面积面和参考图打包；详见 `manifest.json`。原始光照过曝、窗纸过亮、屋面缝隙漏光、东侧洞口朝向等问题已在渲染复查中调整。

这是高精度环境源模型，约 591 万三角面。独立游戏版导出为约 125 万三角面的空间分块 FBX，采用顶点色材质；源文件未被游戏版简化覆盖。

已接入 `Assets/Scenes/MainPrototype.unity`，并完成环境/控制器绑定、八桥通行、河流阻挡、树干与高山碰撞、40 普通敌人 + 10 精英、五洞口触发与补给布置。三条核心时间规则及 30 秒中期 Boss 已通过 Unity Play Mode 集成检查。细节与证据见 [第二关接入记录](../../../docs/validation/pingchuan_town/README.md)。

仍未完成完整 UV/PBR 纹理烘焙、独立道具库、多级 LOD、分块流式加载、手机性能测试和最终人工视觉验收。采用现有平面 Rigidbody 控制器，不依赖 NavMesh。洞口模型只有短通道；进入后仍使用游戏已有洞穴内容场景。

游戏版导出命令：

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Blender/PingchuanTown/PingchuanTown_v01.blend --python ArtSource/Blender/PingchuanTown/export_to_unity.py
```

导出后在 Unity 使用 `37 MiniGame/Build Pingchuan Town Level 2` 重建第二关。
