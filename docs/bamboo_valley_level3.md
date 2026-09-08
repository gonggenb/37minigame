# 第三关：竹影幽谷

## 已接入

`Assets/Scenes/BambooValleyLevel.unity` 是独立第三关。前两关保留原场景；三关都在 Build Settings 中。

- 第二关最终 Boss 战胜利时保存第三关解锁。第二关结算可点击“下一关”；以后也可从选关页进入第三关。
- 第三关使用同一加载页，加载后先选择起手武学，确认后开始 60 秒探索。重复点击下一关不会产生重复载入。
- 第三关复用现有普通战斗、奖励、洞穴事件、九尾妖狐最终 Boss 和结算。没有新增 Boss 美术；第 30 秒玄甲镇关使节点仍只用于第二关。
- 第三关胜利后可返回主页；没有配置第四关。

## 地图及资源

Blender 母版：`ArtSource/Blender/BambooValley/BambooValley.blend`。

导入模型：`Assets/Art/Environment/BambooValley/BambooValley.fbx`。

环境 Prefab：`Assets/Prefabs/Environment/BambooValley.prefab`。

原 Blender 程序化色彩转换为顶点色，并使用项目内新增的 Unity 着色器；材质使用相同的木、瓦、石、竹和暖光方向，效果不与 Blender Cycles 完全一致。叶片网格已经过简化，FBX 从约 28 MB 降至约 23 MB；植被按空间块拆分，便于镜头剔除，取消大量细叶的实时阴影；外围追加共用网格竹林。镜头与主角之间的竹叶通过局部抖动透明减少遮挡。

Unity 模型根旋转为 `(0,180,0)`，高度偏移 `-0.4` 米。已通过 FBX 中的 Anchor 对象核对坐标。地图约 50 × 44 米，出生点 `(-20,0,-16)`，中央亭台 `(0,0,-2)`，武馆 `(0,0,12)`，东侧山洞 `(20,0,-2.8)`。

河道有连续阻挡，仅 `z=-6` 与 `z=7` 两座木桥可通行。桥两侧、外围、武馆、营帐、亭柱和洞口岩壁有简化碰撞体。角色碰撞根保持平面，显示层根据第三关地形和桥面抬升，避免改变遭遇距离及时间规则。

场景放置 16 个普通敌人、3 个精英、4 个宝箱、4 个药草、2 个望气灵物、1 个隐藏洞穴。敌人、美术和数值复用已有配置，这不是第三关专属平衡结果。

## Unity 操作

本次已实际生成场景、创建并引用环境 Prefab、材质、摄像机、玩家、互动点及碰撞体，不需要手动拖拽绑定。

运行游戏：打开 `MainPrototype` 并进入 Play Mode，通关第二关，在结算点击“下一关”。也可在解锁后从选关页选择第三关。若只想检查美术，可直接打开 `BambooValleyLevel`，在 Scene 视图查看环境。

重建地图：先停止 Play Mode、保存当前修改，再执行 `37 MiniGame/Build Bamboo Valley Level 3`。这会从第二关重新复制玩法宿主并重建第三关，覆盖第三关内的手动修改；手工精修前请另存版本。

重导 Blender 模型：运行 `ArtSource/Blender/BambooValley/export_to_unity.py`（使用 Blender 的 Python），随后运行上述 Unity 构建菜单。

## 验证

- 已编译并导入；材质映射和两个桥口通过静态检查。
- 执行了 `37 MiniGame/Validate Chinese Fonts`，并直接调用校验器确认通过。使用项目锁定的静态 Noto 源字体更新两份子集及指纹。原开场对话中的 `▾` 不在锁定源字体中，改为支持的 `▼`，避免整个构建被缺字阻断。
- 实际 Play Mode 探针通过 21 项检查，报告：`docs/validation/bamboo_valley/playmode_report.json`。
- 关键检查包含真实 Rigidbody 移动跨过两桥、水面阻挡、碰撞进入敌人及洞穴、普通战斗继续主时间、洞穴战斗冻结主时间、最终 Boss 独立计时、第二关战斗结算解锁第三关、下一关及选关重载。
- 探针只在 Editor 主动运行，结束会恢复原解锁存档与运行设置。通过 `37 MiniGame/Validate Bamboo Valley Play Mode` 重跑。
- 画面证据位于 `docs/validation/bamboo_valley/`。这是受控整合验证，不是自然构筑平衡、真机帧率或最终美术批准。

## 仍需后续验收

还需自然游玩验证路线、怪物强度与成长密度，以及 WebGL / 手机的帧率、内存、触控及长时间稳定性。本次没有重新发布 WebGL，没有新增独立第三关 Boss，也没有烘焙 Blender 的全部 PBR 材质或制作完整 LOD。

## 文件变更

修改：`LevelSequence.cs`、`GameFlowController.cs`（三关解锁与流转）；`GameTextCatalog.cs`、`PrototypeHUDController.MainMenu.cs`（关卡名与选关卡片）；`PlayerController.cs`（第三关显示高度）；`PrototypeSceneBuilder.cs`（重建前两关时保留后续构建场景）；`BattleScreenController.Opening.cs`（兼容字形）；两份字体与 `.meta`；`EditorBuildSettings.asset`；项目核心、玩法与技术文档。

新增：第三关 `.unity` 与环境 `.prefab`，FBX、导出清单、Unity Shader 和材质目录；`BambooValleyLevelBuilder.cs`、`BambooValleyLayout.cs`、`BambooValleyVisibility.cs`、`BambooValleyPlayModeProbe.cs` 及其 `.meta`；Blender 导出脚本、本说明和验证报告/截图。
