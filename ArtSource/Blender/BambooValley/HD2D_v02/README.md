# 竹影幽谷 · HD-2D Blender v02

本版依据用户提供的 `abd40112-e408-4289-8cfb-bc5ed03d6a52.png`，在已有 `../BambooValley.blend` 母版上制作独立修订。图内“第二关”是参考图文字，本次不据此更改游戏关卡编号；现有 Unity 竹影幽谷仍是第三关。Blender 模型采用真实 3D 几何和程序化材质，不是把参考图贴在平面上。

## 打开

用 Blender 5.2 打开 `BambooValley_HD2D_v02.blend`。活动场景为 `Bamboo Valley HD2D v02`，相机为 `CAM 04 • HD2D bridge courtyard`。

- 小键盘 `0`：相机视图；`F12`：Cycles 渲染。
- 材质预览已开启场景灯光和世界环境；最终雾与辉光效果以 F12 为准。
- `90 Cameras` 保留总览、正俯视和近景相机；正俯视适合检查路线。
- 米制单位，北为 Blender `+Y`、高为 `+Z`，地图约 50×44 米。
- 集合 `01`–`20` 沿用原地图结构；`21`–`23` 是新增碎石、落叶和窗纸；`93 HD2D` 是新增灯光及薄雾。
- 原地图的网格按材质分组。编辑模式可选择相连几何，必要时用 `P → 按松散块` 拆分。石路和木桥倒角保留为可调节 Modifier。

## 场景内容与本版变化

入口牌坊、两座溪流木桥、中央六角亭、北部武馆旧院、西北营地、东北比武台、东部岩洞、竹林、围栏、战旗、木车和箱桶均保留。中心和支路空地承担行走，水体与岩壁承担地形分隔。

本版新增道路边角倒角、肩部碎石与落叶，武馆窗纸自发光面和投射光。材质纹理改用米制空间坐标，压低瓦顶反射，增加石材凹凸与木质纹理。灯光采用冷色林间补光、暖色灯笼与窗光、局部亭台灯和洞口灯；使用真实体积薄雾、透视相机景深和克制的合成辉光。

光影参考用户要求的 HD-2D 氛围，不包含任何商业游戏模型、贴图或提取资源。

## 重现与文件

- `../refine_hd2d_v02.py`：可重现本版建模、材质、灯光和相机设置的脚本。读取原母版运行，输出固定为本目录；再次运行会覆盖本版，因此手工精修应先另存。
- `BambooValley_HD2D_v02.blend`：本次交付的可编辑场景，参考图和字体沿用母版的打包资源。
- `BambooValley_HD2D_Hero.png`：实际 Cycles 渲染。
- `manifest.json`：模型规模和验证范围。
- `Blender_session_before_v02.blend`：打开地图前保存的用户 Blender 会话备份。

命令行重现：

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background ArtSource/Blender/BambooValley/BambooValley.blend --python ArtSource/Blender/BambooValley/refine_hd2d_v02.py
```

## Unity 边界与后续操作

本次不覆盖原 `.blend`、原 FBX、Unity 场景、Prefab、脚本或运行时文案。原版已接入 Unity 的说明见 `docs/bamboo_valley_level3.md`；该接入与测试结论不能自动套用到 v02。

v02 的程序化材质、体积雾、Cycles 灯光与合成节点不能通过 FBX 原样进入 Unity。后续接入须导出网格、烘焙纹理或转换顶点色，在 Unity 中重建对应材质、灯光和后处理。雾体积对象和相机属于表现辅助，不作为实体碰撞导出。

本版约 56 万多边形、38 盏灯，用于可编辑美术母版与离线预览；尚未做移动端性能优化或灯光烘焙。游戏接入后仍须检查两桥通行、水面阻挡、洞口可达、横竖屏目标遮挡，以及设备帧率。

没有改动三条时间规则：普通战斗继续主时间；洞穴暂停主时间；最终 Boss 独立计时。本次没有运行 Unity Play Mode 回归，也不标记游戏内最终验收。
