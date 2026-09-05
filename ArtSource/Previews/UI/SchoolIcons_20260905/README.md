# 流派图标配色与特效

日期：2026-09-05。UI 类型：武学图标、战斗 Slot、升级窗口、角色窗口、商店。

## 实现

| 流派 | 统一色 | 图形特效 |
| --- | --- | --- |
| 快剑 | 冰青 `#65C5DE` | 斜向剑气与细尾迹 |
| 毒掌 | 碧绿 `#83C25B` | 交叠毒雾与毒点 |
| 铁壁 | 琥珀 `#E0AB4F` | 硬边护盾轮廓 |
| 轻身 | 淡紫 `#AB8FD7` | 三层轻云残影 |
| 血刃 | 朱红 `#DB6458` | 弧形刀芒 |

20 门武学按 `MartialArtCatalog.school` 着色；5 个秘传使用 `firstSchool / secondSchool`
左右双色和对应纹样。保留原始 PNG、透明轮廓、墨线与高光；颜色、轮廓光和局部特效由
共享 Shader 在首次使用时渲染至 128×128 缓存。25 张缓存约 1.56 MiB，不含原始纹理。
特效是静态的，不使用持续闪烁、旋转或粒子；现有触发高亮、冷却遮罩、品类与重数仍独立绘制。
Shader 不可用时退回原图，代码重载及下次 Play Mode 启动时清理缓存。

## 本次文件

修改：

- `Assets/Scripts/UI/PrototypeHUDController.cs`：共享图标加载与完整五派配色；升级、角色、秘传和战斗栏接入。
- `Assets/Scripts/UI/PortraitHudViews.cs`：竖屏升级及角色列表使用流派色。
- `Assets/Scripts/Cave/CaveRoomController.cs`：横竖屏商店武学图标使用同一效果。

新增：

- `Assets/Scripts/UI/MartialArtIconRenderer.cs`：统一色板、秘传映射、GPU 缓存及降级。
- `Assets/Resources/UI/Effects/MartialArtIcon.shader`：主体着色、轮廓光、五派纹样；Resources 引用随构建打包。
- `Assets/Editor/MartialArtIconPreview.cs`：原图／效果图并排查看、64／48／32 px 切换及 GPU 导出。
- 对应 Unity `.meta` 和本目录 25 张 GPU 预览 PNG。本目录不是运行时图源。

工作区中本次任务开始前已有 UI、字体与场景改动；没有覆盖这些改动。
无需创建 GameObject、挂脚本、绑定 Inspector 或拖入 Prefab；共享加载路径自动生效。
没有新增运行时中文文案，也没有新增 PLACEHOLDER_UI。

## 验证与运行

- Unity 6000.5.4f1 编译通过；修正了此版本禁用旧 `GetInstanceID()` 的兼容问题。
- Editor / Metal 上 25 个图标均生成有效 RenderTexture，已实际导出本目录 PNG，未走原图降级。
- 预览窗口检查了全部图标的 64／48／32 px 前后对比；复杂绝学的细节在 32 px 仍较少，流派颜色与外轮廓可区分。
- `37 MiniGame/Validate UI Safe Areas`：4 组横竖屏与异形屏逻辑安全区校验通过。
- `MainPrototype` 的 Play Mode：540×960 竖屏起手选择显示淡紫轻身、朱红血刃、碧绿毒掌；选择饮血刀法后正常进入 60 秒探索。测试结束已退出 Play Mode，控制台未出现新增运行错误。
- 横屏 Game 预设下拉框未响应此次自动操作；**960×540 横屏 Play Mode 尚未完成**。安全区计算通过不替代实际画面验收。
- 三条时间规则的实现均未修改：普通战斗继续主时间、洞穴暂停主时间、Boss 独立计时。此次没有重跑完整战斗／洞穴／Boss 回归。

查看：Unity 菜单 `37 MiniGame/Preview School Icons`；切换 64／48／32 px 比较，左原图、右效果图。
运行：打开 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode → 选择关卡 → 关卡2。
继续验收：960×540 横屏升级与角色面板；横竖屏战斗冷却／触发高亮及商店武学；WebGL 构建和手机真机。
本批状态为局部 `InEngineQA`，尚未 `Approved`。
