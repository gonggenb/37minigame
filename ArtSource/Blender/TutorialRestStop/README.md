# 第一关：山脚古道的小歇脚地

依据本轮用户提供的 `33283148-7eca-40e2-988a-ffc77885bc3a.png`，新建的独立 Blender 场景。核心区约 18×18 米，外围地表、溪岸和竹林向外延伸。地图采用实建网格、分组模型和程序化材质；参考图只作为打包参考，不是用来冒充 3D 场景的背景贴图。

## 打开和查看

用 Blender 5.2 打开 `TutorialRestStop_v01.blend`，活动场景为 `Level 01 | 山脚古道的小歇脚地`。

- 小键盘 `0` 查看总览相机，`F12` 渲染。默认 Cycles CPU 48 samples，1600×1200。
- `92 Cameras` 中有总览、正俯视和茶桌/老松/路亭近景三台相机。选中相机后 `Ctrl + 小键盘 0` 切换活动相机。
- 材质预览使用场景灯光与世界环境；景深、体积雾、合成辉光以实际渲染为准。
- 米制：1 Blender 单位=1 米，北为 `+Y`，竖直为 `+Z`。
- 所有参考图和使用的字体已打包。材质无外部图片依赖。

## 布局与细节

本轮按新参考图的四角布局建模。它与旧教学场景的南/东/西/北配置不同，本次没有修改 Unity 中的互动坐标。

| 区域 | Blender 位置约值 X,Y | 模型内容 |
|---|---|---|
| 南侧入口 | 0,-9 | 低石桥、石柱护栏、灯笼、木路标 |
| 中央歇脚区 | 0,0 | 弯曲老松、松针簇、裸露树根、茶桌、长凳、茶具、茶旗 |
| 西北宝箱点 | -5.2,4.3 | 旧路亭、分片曲面瓦、檐口瓦当、梁柱斗拱、栏杆、台阶、铆钉宝箱 |
| 东北洞穴 | 5.65,4.1 | 苔岩洞口、实建短洞道、灯火、洞口补给、山洞布旗 |
| 西南药草点 | -5.5,-2.2 | 一簇红果白花药草、苔石、低灯 |
| 东南遭遇预留点 | 4.75,-2.15 | 邻近帐篷、篝火、吊锅、绳索木桩、木车、练武架和箱桶 |

中央石路环绕老松并分出四条支路，路宽约 1.65–2.1 米。营地遭遇预留点与篝火保持距离。`80 Anchors` 中的 Empty 仅作为未来接入定位，不是已实现的敌人、道具或事件脚本。

模型约 59 万多边形（未计倒角修改器的求值增量），524 根分节竹竿，20 盏灯。按地形、路径、路亭、宝箱、岩洞、老松、茶桌、营地、植被和照明分组。相同区域按材质合并，编辑模式可选相连几何，使用 `P → 按松散块` 拆分；制造件保留可调节倒角 Modifier。

## 光影

借鉴用户要求的 HD-2D 光影方向：冷色山林与环境补光、暖色灯笼和篝火、路亭局部照明、后景薄雾、克制辉光与近景景深。不是商业游戏资产的提取或复制。主角/敌人动画不在本模型中制作，游戏中仍可复用现有像素角色。

## 文件

- `TutorialRestStop_v01.blend`：可编辑模型、材质、灯光和相机。
- `RestStop_Overview.png`、`RestStop_Layout.png`、`RestStop_Detail.png`：最终实际 Blender 渲染。
- `RestStop_Preview.png`：中途低采样检查图，不作为最终效果。
- `01_foundation.py`：基础几何、材质、地形、溪流与石路。
- `02_landmarks.py`：入口、路亭、宝箱、岩洞与定位点。
- `03_dressing.py`：老松、茶桌、药草、营帐、篝火、木车、路标。
- `04_nature_lighting.py`：自然边界、竹林、灯光、相机与保存。
- `05_refinement.py`：首轮渲染后的树冠、外围和细节修整，由第四阶段调用。
- `build_scene.py`：依次运行建模阶段；在新文件中执行，不删除其他场景，但会覆盖本目录交付文件，因此精修后请另存版本。
- `render_views.py`：只读母版并输出三张检查图，不保存修改后的相机设置。
- `validate_scene.py`、`validation.json`：结构、资源打包、锚点和文件检查。
- `manifest.json`：规模及资源状态。

从项目根目录重建：

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background --factory-startup --python ArtSource/Blender/TutorialRestStop/build_scene.py
```

从项目根目录渲染：

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Blender/TutorialRestStop/TutorialRestStop_v01.blend --python ArtSource/Blender/TutorialRestStop/render_views.py
```

## Unity 操作与验收边界

本次仅新增本目录，未修改现有 Unity 文件、旧竹影幽谷或教学规则。Blender 三维模型和渲染已经制作；Unity 导入、绑定、碰撞、NavMesh、横竖屏与真机性能尚未做。

后续用于游戏需要：导出环境网格；对主要建筑和道具整理 UV、烘焙或转换程序化材质；处理植被 LOD 和灯光烘焙；在 Unity 为地面、溪流、桥、岩壁及建筑配置简化碰撞；将现有玩家、宝箱、药草、洞穴和敌人对象放到本图对应区域。当前 `WorldPlanar` UV 只是基础平面映射，不代表已完成可烘焙 UV 展开。Blender 体积雾和合成辉光须在 Unity 中重建，不能随 FBX 原样导入。

三条核心时间规则保持：普通战斗主时间继续、洞穴暂停主时间、最终 Boss 独立计时。本次没有改动运行时代码，因此没有重跑 Unity 时间规则测试。教学 30 秒时限保持原样。

后续 Play Mode 必查入口过桥、洞口与四个目标可达、树冠及屋顶遮挡、中文标签、计时和设备帧率。当前交付状态不等于游戏最终验收或移动端性能批准。

## Unity 接入状态（2026-09-08）

已导入 `Assets/Scenes/TutorialLevel.unity`，四个互动点、角色高度、简化碰撞与材质已绑定。此前文中的 Unity 待接入事项以 `docs/tutorial_rest_stop.md` 为准。受控 Play Mode 34 项通过，移动端性能和真人自然路线试玩仍待验收。
