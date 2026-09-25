# 第二关六种小怪接入记录

## 交付

> 后续玩法更新：第二关三类普通怪特点已接入，当前规则与新验证见 [区域与战斗优化](../route_combat_review/README.md)。下文保留本次美术接入的范围与历史证据。

2026-09-09：MainPrototype（第二关平川山镇）改为新老混排：22 个原有小怪点位 + 18 个新怪点位。
六种新怪各 3 处；各区域均保留老怪。重复应用或重建关卡也会恢复这套混合分布。
关卡1与关卡3未加入此批，共用遭遇模板未改；原有 10 个精英及中期、最终 Boss 保持配置。

| 小怪 | 数量 | 分布倾向 | 普攻动作 |
| --- | ---: | --- | --- |
| 铁獠山猪 | 3 | 入口、练武场外围、北关 | 冲撞顶击 |
| 赤练蛇妖 | 3 | 练武场外围、营地、北关 | 蓄力毒咬 |
| 斗笠刀匪 | 3 | 入口、镇区、营地 | 挥刀斩击 |
| 酒葫芦恶僧 | 3 | 镇区、练武场、营地 | 葫芦重击 |
| 灯笼怨灵 | 3 | 镇区、洞区、北关 | 吐出灵火 |
| 菌甲小妖 | 3 | 入口、洞区 | 根臂拳击 |

上述为造型和普通攻击表现，不增加独立毒伤、投射物命中、范围伤害或位移规则。
每个点位原有数值、等级推导、铜钱和修为保持不变。

## 资源

- 12 张真正透明 RGBA 图集，各 2048×256，96 个 256×256 Sprite 槽位。
- 每种 8 格待机与 8 格攻击，统一脚底 `(128,224)` / Unity Pivot `(0.5,0.125)`。
- 蛇妖、怨灵、菌妖待机为两个原始中立姿势组成的八格循环；其他三种使用补充待机序列。
  未将八格循环描述为八张独立呼吸绘制。造型不匹配或背景不合格的生成候选已排除。
- 160 PPU、Point、Full Rect、Clamp、无压缩、无 Mipmap；保留贴地阴影，战斗水平翻转朝左。
- 整条统一缩放；待机和攻击母版先以中立姿势匹配生成尺寸，再统一归一化；攻击首尾衔接待机首帧。
- 母版与完整提示词：[manifest.json](../../../ArtSource/Raw/Monsters/LevelTwoPack/manifest.json)。内置 image_gen 生成。
- [全部图集预览](../../../ArtSource/Previews/Monsters/LevelTwoPack/all_monsters.png)；同目录包含六份 GIF。
- 状态：已生成、Normalized、Imported、受控 Editor Play Mode 验证；不代表手机或最终美术 Approved。

## 修改与新增文件

修改：

- `Assets/Scenes/MainPrototype.unity`：22 个普通敌人恢复原有名称和动画，18 个普通敌人使用新名称、视觉 ID、待机引用；战斗注册六套视觉配置。
- `Assets/Editor/PrototypeSceneBuilder.cs`：复用切片方法，第二关刷新配置时保留六种小怪注册。
- `Assets/Editor/PingchuanTownLevelBuilder.cs`：重建第二关时应用同一套怪物分布。
- `Assets/Scripts/Runtime/GameTextCatalog.cs`：六个集中式中文名称。
- `docs/generated_monster_pack.md`、`docs/gameplay_systems.md`：资源与关卡分布说明。

新增：

- `Assets/Editor/LevelTwoMonsterPackBuilder.cs`：只作用于第二关的可重复导入、切片与场景绑定菜单。
- `Assets/Scripts/Debug/LevelTwoMonsterPlayModeProbe.cs`：显式运行的测试探针；仅 Editor 编译，不进入玩家构建。
- `Tools/ArtPipeline/prepare_level2_monster_pack.py`：透明母版连通区域分离、统一缩放、脚点对齐和预览。
- `Assets/Art/Generated/Characters/Enemies/LevelTwoPack/`：12 张游戏图集及 Unity 元数据。
- `ArtSource/Raw/Monsters/LevelTwoPack/`、`ArtSource/Previews/Monsters/LevelTwoPack/`：母版、提示词、静态/动态预览及归一化记录。
- 本验证目录：接入前后快照、范围校验、运行报告和截图。

仓库内任务开始前已存在其他未提交的地图与玩法改动。本次通过接入前快照核对，不将既有差异归入本任务。

## 验证证据

最终资源复测：`LEVEL2_MONSTER_PLAYMODE_PASS`，98 项检查通过，运行时错误 0。
导入审计见 `import_audit.json`；六套注册无重复，12 张图集均解析为八帧，22 个老怪地图动画与原模板一致。
测试后已退出到 Edit Mode。场景混排在测试前已保存，并直接检查磁盘场景确认六种各 3 处；
测试后编辑器报告场景有未保存标记，未自动重载或保存，以保留可能的其他编辑。

- `before.json` 与 `after.json`：逐项比较 73 个对象。位置、类型、奖励与战斗数值相同；
  仅 18 个普通敌人的 `displayName` / `visualId` 与原始快照不同，另外 22 个完全恢复。
- `scope_hashes.json` 保存首次接入的范围基线。此次混排复核：关卡1/3场景、共用遭遇模板、BattleManager 仍一致。
  GameFlowController 在两次任务之间另有改动，因此不再将其旧哈希作为当前一致性证据；本次混排未编辑该脚本。
- Unity 菜单 `37 MiniGame/Validate Chinese Fonts` 通过：910 个非 ASCII 字形，常规体/粗体覆盖。
- [Play Mode 报告](playmode_report.json)：六种新怪注册、地图动画序列、全部 11 个新老视觉 ID 的实际普通战斗与视觉选择，
  横屏 960×540 / 竖屏 540×960，普通战斗继续主时间、洞穴战斗暂停主时间、最终 Boss 独立计时、重开恢复。
- 新老视觉 ID 各有横竖屏战斗截图；截图过程先观察实际攻击推进，再通过真实攻击结算器定格动作。
  批量测试提高了双方生命作为持续战斗夹具，因此多数截图里的等级/伤害不是平衡样本；这些夹具未保存到场景。
- `natural_stats_battle.png` 使用未加测试生命的初始战斗配置，用于查看实际 UI 与小怪呈现。

## 运行与复现

1. 打开 `Assets/Scenes/MainPrototype.unity`，Play，从首页进入第二关并选择起手武学。
2. 入口、镇区、练武场、营地、洞区与北关会遇到新老混合小怪。碰怪后显示同名角色并播放攻击。
3. 自动接入已保存，无需手工创建 GameObject、挂脚本或拖 Inspector 引用。
4. 重导入：Edit Mode 执行 `37 MiniGame/Apply Level 2 Monster Pack`。其他关卡会被拒绝处理。
5. 自动验证：`37 MiniGame/Validate Level 2 Monster Pack Play Mode`；结束自动退出 Play Mode，恢复 Game View 设置。
6. 从母版重新归一化：`uv run --with pillow --with numpy --with scipy python Tools/ArtPipeline/prepare_level2_monster_pack.py`，
   随后重导入并重新验证。

未完成的独立验收：真机触摸/性能、自然路线长期平衡、三种两姿势待机进一步丰富及最终美术批准。
