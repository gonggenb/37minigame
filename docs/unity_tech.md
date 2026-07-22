# Unity 技术约束

## 1. 技术栈定位

本项目使用 Unity 制作。

默认使用 C# 编写游戏逻辑。

Codex 不得擅自把项目改成其他引擎方案，也不要生成 Godot、Unreal、Cocos 专属结构。

## 2. Unity 项目原则

实现时优先保持项目结构清晰。

推荐目录结构：

```text
Assets/
  Scripts/
    Core/
    GameFlow/
    Player/
    Enemy/
    Battle/
    Map/
    Cave/
    Boss/
    Reward/
    MartialArts/
    UI/
    Config/
    Save/
    Debug/
  Prefabs/
  Scenes/
  ScriptableObjects/
  Art/
  Audio/
  UI/
```

如果当前项目已有目录结构，应优先遵守当前项目结构，不要强行重建。

## 3. 推荐脚本模块

推荐使用以下模块命名：

- `GameFlowController`
- `RunManager`
- `MainTimerController`
- `PlayerController`
- `PlayerStats`
- `EnemyController`
- `EnemySpawner`
- `EncounterManager`
- `BattleManager`
- `AutoBattleSimulator`
- `CaveManager`
- `BossBattleManager`
- `RewardManager`
- `MartialArtManager`
- `UIManager`
- `HUDController`
- `ResultPanelController`
- `SaveManager`
- `DebugPanelController`

不要把所有逻辑写在一个超大 MonoBehaviour 中。

## 4. 场景建议

初期建议至少有一个主测试场景：

```text
Assets/Scenes/MainPrototype.unity
```

该场景用于验证最小核心闭环：

1. 进入游戏。
2. 开始 60 秒主地图倒计时。
3. 玩家移动。
4. 敌人生成。
5. 玩家触碰敌人进入自动战斗。
6. 普通战斗期间主倒计时继续。
7. 战斗结束返回地图。
8. 隐藏洞穴暂停主倒计时。
9. 60 秒结束进入 Boss 战。
10. Boss 战独立计时。
11. Boss 战结束进入结算。
12. 可以重新开始。

## 5. Prefab 原则

需要 Prefab 的对象建议包括：

- Player。
- NormalEnemy。
- EliteEnemy。
- BossEnemy。
- HiddenCaveEntrance。
- ResourcePoint。
- EventPoint。
- DamageText。
- MainHUD。
- BattlePanel。
- ResultPanel。
- DebugPanel。

如果 Codex 只能生成脚本，不能可靠创建或绑定 Prefab，则必须明确告诉用户需要在 Unity Editor 中手动创建和绑定。

## 6. UI 原则

初期 UI 以可测试为第一目标，不追求最终美术效果。

普通地图 HUD 至少显示：

- 主地图剩余时间。
- 玩家气血。
- 当前攻击或战力。
- 当前铜钱或修为。
- 当前主要武学。

战斗界面至少显示：

- 玩家血量。
- 敌人血量。
- 战斗状态。
- 胜负结果。

结算界面至少显示：

- 是否击败 Boss。
- 击杀数量。
- 获得奖励。
- 本局关键武学。
- 重新开始按钮。

## 7. 数据配置原则

优先使用 ScriptableObject 或集中式配置类管理数值。

适合配置化的内容包括：

- 玩家基础属性。
- 敌人属性。
- Boss 属性。
- 武学效果。
- 奖励内容。
- 地图生成规则。
- 洞穴内容。
- 随机事件。

不要把大量数值散落写死在逻辑脚本中。

早期原型允许使用临时配置，但必须集中管理，并保留后续迁移空间。

## 8. Unity Editor 手动绑定说明

涉及 Unity Editor 的内容时，Codex 必须清楚说明：

- 需要创建哪些 GameObject。
- 每个 GameObject 挂载哪些脚本。
- Inspector 需要绑定哪些字段。
- 哪些 Prefab 需要拖入引用。
- 哪些 UI Text、Button、Slider 需要绑定。
- 如何进入 Play Mode 验证。

Codex 不得假装已经完成 Unity Editor 中的手动操作。

## 9. 输入控制

初期玩家移动可以使用 Unity 默认输入方式实现。

如果项目已经使用 Input System，则遵守现有 Input System。

如果项目没有配置 Input System，不要强行引入复杂输入框架，优先实现可运行的基础移动。

## 10. 调试要求

建议保留 Debug 面板或快捷键，用于快速验证：

- 开始一局。
- 重置本局。
- 增加玩家攻击。
- 增加玩家气血。
- 生成敌人。
- 生成隐藏洞穴。
- 直接进入 Boss。
- 强制胜利。
- 强制失败。
- 打印当前游戏状态。
- 打印当前属性。

Debug 功能不得破坏正式玩法逻辑。

## 11. 核心时间规则验证

每次修改涉及流程、战斗、洞穴或 Boss 时，都必须验证：

1. 普通地图碰怪战斗时，主地图 60 秒倒计时继续。
2. 隐藏洞穴内战斗时，主地图 60 秒倒计时暂停。
3. 最终 Boss 战不受 60 秒限制，独立计算 Boss 战时间。

这三条是硬规则。

