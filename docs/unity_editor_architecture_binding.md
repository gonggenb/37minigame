# Unity Editor 架构自动化与检查清单

本文档记录 `MainPrototype_Architecture` 的自动生成内容，以及仍需要在 Unity Editor 中确认的项目级选择。

脚本、Inspector 引用、uGUI 和 Architecture Prefab 已由 `Assets/Editor/ArchitectureSceneAutomation.cs` 自动处理，不再要求逐项手工拖拽。

## 1. 执行自动化

1. 使用 Unity `6000.5.4f1` 打开项目并等待编译结束。
2. 执行 `Tools > 一炷江湖 > 导入 CSV 配置`，确认存在：
   - `Assets/GameData/Generated/GameDatabase.asset`
3. 执行 `37 MiniGame > Architecture > Rebuild Architecture Scene`。
4. 打开 `Assets/Scenes/MainPrototype_Architecture.unity`。
5. 保存项目并运行 EditMode 测试。

该命令可以重复执行，会重建：

- `MainPrototype_Architecture` 中的 `ArchitectureRoot`。
- `Assets/Prefabs/Architecture/**`。
- `Assets/Scripts/Config/SpawnPrefabCatalog.asset` 的七条 Prefab 映射。

该命令不会修改：

- `Assets/Scenes/MainPrototype.unity`。
- `Assets/Prefabs/Enemy/**`、`Assets/Prefabs/Items/**`、`Assets/Prefabs/Canvas/**` 中的源 Prefab。
- 三条核心时间规则。

## 2. 自动生成的场景层级

执行后应存在：

```text
ArchitectureRoot
  Data
    GameDatabaseProvider
  Manager
    CharacterManager
    BattleRunner
    RunManager
  SpawnRegions
    Region_EastForest
    Region_SouthQuarry
    Region_NorthPass
    Region_MainMap
  WorldSpawners
    EnemySpawner
    ItemSpawner
    CaveSpawner
  RuntimeSpawnedObjects
  UGUI
    Canvas
      MainMenuPanel
      HudPanel
      BattlePanel
      CavePanel
      LevelUpPanel
      ResultPanel
    EventSystem
```

`ArchitectureRoot` 的世界坐标应为 `(0, 0, 0)`。

## 3. 自动绑定的核心引用

以下字段应全部非空；正常情况下无需手工修改：

| 组件 | 字段 | 自动绑定对象 |
|---|---|---|
| `GameDatabaseProvider` | `database` | `Assets/GameData/Generated/GameDatabase.asset` |
| `CharacterManager` | `databaseProvider` | `ArchitectureRoot/Data` |
| `BattleRunner` | `characterManager` | `ArchitectureRoot/Manager/CharacterManager` |
| `RunManager` | `characterManager` | `CharacterManager` |
| `RunManager` | `battleRunner` | `BattleRunner` |
| `RunManager` | `enemySpawner` | `WorldSpawners/EnemySpawner` |
| `RunManager` | `itemSpawner` | `WorldSpawners/ItemSpawner` |
| `RunManager` | `caveSpawner` | `WorldSpawners/CaveSpawner` |
| 三个 Spawner | `databaseProvider` | `ArchitectureRoot/Data` |
| 三个 Spawner | `prefabCatalog` | `Assets/Scripts/Config/SpawnPrefabCatalog.asset` |
| 三个 Spawner | `regions` | 四个 `SpawnRegion` |
| 三个 Spawner | `spawnedRoot` | `RuntimeSpawnedObjects` |
| 三个 Spawner | `runManager` | `ArchitectureRoot/Manager/RunManager` |

玩家根对象会自动补齐 `PlayerInteractionActor`。

## 4. 自动生成的出生区域

| GameObject | `regionId` | 默认中心 | 默认范围 |
|---|---|---:|---:|
| `Region_EastForest` | `east_forest` | `(12, 0.08, 7)` | `(14, 0, 12)` |
| `Region_SouthQuarry` | `south_quarry` | `(10, 0.08, -10)` | `(15, 0, 11)` |
| `Region_NorthPass` | `north_pass` | `(-9, 0.08, 10)` | `(16, 0, 9)` |
| `Region_MainMap` | `main_map` | `(0, 0.08, 0)` | `(38, 0, 31)` |

如果后续修改地图边界，需要在 Scene 视图检查绿色 Gizmo。可以在最终构建后临时调整 Inspector；若希望重建后仍保留调整，应同步修改 `ArchitectureSceneAutomation.cs` 中的区域参数。

## 5. 自动生成的 Architecture Prefab

| `prefabId` | Architecture 副本 | 新交互组件 |
|---|---|---|
| `prefab_enemy_bandit` | `Assets/Prefabs/Architecture/Enemies/山贼喽啰.prefab` | `EnemyEncounter` |
| `prefab_enemy_bamboo` | `Assets/Prefabs/Architecture/Enemies/流寇.prefab` | `EnemyEncounter` |
| `prefab_enemy_ink_wolf` | `Assets/Prefabs/Architecture/Enemies/灰岩巨鼠.prefab` | `EnemyEncounter` |
| `prefab_enemy_stone_ape` | `Assets/Prefabs/Architecture/Enemies/黑风刀客.prefab` | `EnemyEncounter` |
| `prefab_treasure` | `Assets/Prefabs/Architecture/Items/东市宝箱.prefab` | `TreasureChest` |
| `prefab_herb` | `Assets/Prefabs/Architecture/Items/北门药草.prefab` | `HerbPickup` |
| `prefab_hidden_cave` | `Assets/Prefabs/Architecture/Cave/古藏秘窟.prefab` | `CaveEntrance` |

每个副本会自动：

- 保留原视觉、Collider、动画和缩放组件。
- 将交互 Collider 设置为 Trigger。
- 挂载 `WorldInteractionTrigger` 并绑定同对象的新交互组件。
- 移除旧 `EncounterTrigger`。
- 在移除旧 Trigger 前移除依赖它的 `EnemyLevelLabel`、`TreasureMapIndicator` 或 `CaveEntranceIndicator`。
- 更新 `SpawnPrefabCatalog.asset` 指向 Architecture 副本。

## 6. 自动生成的 uGUI

Canvas 配置：

- `Screen Space - Overlay`。
- `CanvasScaler = Scale With Screen Size`。
- Reference Resolution 为 `1920 × 1080`。
- Match 为 `0.5`。
- 使用项目内 Noto Sans CJK SC 字体，支持中文。

自动生成并绑定：

| 面板 | View | 内容 |
|---|---|---|
| `MainMenuPanel` | `MainMenuView` | 标题、玩法提示、开始按钮 |
| `HudPanel` | `HudView` | 主时间、气血、气血条、境界/修为、铜钱、状态 |
| `BattlePanel` | `BattleView` | 双方气血、双方气血条、效果、普通/Boss 战时间 |
| `CavePanel` | `CaveView` | 洞穴暂停说明、离开按钮 |
| `LevelUpPanel` | `LevelUpView` | 三个武学选择、刷新按钮和次数 |
| `ResultPanel` | `ResultView` | 胜负、本局统计、重新开始按钮 |
| `Canvas` | `GameUiPresenter` | `RunManager` 与六个 View 的全部引用 |

架构场景中旧 `PrototypeHUDController`、`BattleScreenController`、`GameFlowController`、`BattleManager`、`CaveRoomController`、`BattleFeedbackAudio` 和 `MainMapMusicController` 会被停用；带旧 `EncounterTrigger` 的场景对象会被设为 inactive。旧场景不受影响。

## 7. 仍需在 Unity Editor 确认的项目选择

以下不是缺失绑定，而是需要项目负责人确认的内容：

- [ ] 在 Scene 视图检查四个 SpawnRegion 是否覆盖可行走区域且不穿越边界。
- [ ] 决定是否把 `MainPrototype_Architecture.unity` 加入 Build Settings 并替换当前默认的 `MainPrototype.unity`。
- [ ] 试玩确认移动、相机和生成物碰撞手感；当前新架构仍复用旧玩家移动组件。
- [ ] 决定洞穴内敌人、商人、秘藏和洞穴专属场景/房间的下一阶段方案。
- [ ] 决定是否把旧音乐控制器迁移为订阅新 `RunManager` 状态的音频适配器。
- [ ] 正式美术阶段再替换基础 UI 色板、按钮图和面板背景；不要在数据架构验收前扩大范围。

## 8. EditMode 验证

1. 打开 `Window > General > Test Runner`。
2. 选择 `EditMode`。
3. 执行 `Run All`。
4. 预期 `22/22` 通过。

其中三项自动化验收测试检查：

- 核心管理器、Spawner、Catalog 和四个 SpawnRegion 引用。
- Canvas、EventSystem、六个 View 和 `GameUiPresenter` 引用。
- 七个 Architecture Prefab、Trigger、新交互组件及 Catalog 路径。

## 9. Play Mode 验证

### 主菜单与生成

1. 打开 `MainPrototype_Architecture.unity`。
2. 进入 Play Mode。
3. 确认显示“踏入江湖”按钮。
4. 点击开始，确认主菜单隐藏、HUD 显示。
5. 展开 `RuntimeSpawnedObjects`，确认按 CSV 生成敌人、宝箱、药草和洞穴入口；数量会受范围和权重影响。

### 三条时间规则

1. 普通战斗停留 5 秒，主时间应减少约 5 秒。
2. 洞穴状态或洞穴战斗停留 5 秒，主时间应保持不变。
3. Boss 战中主时间应保持不变，Boss 独立时间应增加。

本轮 UnityMCP 运行态核验记录：

```text
普通战斗：60.0 -> 55.0
洞穴状态：60.0 -> 60.0
Boss 主时间：60.0 -> 60.0
Boss 独立时间：0.0 -> 5.0
```

### UI 状态

- [ ] `MainMap` 显示 HUD。
- [ ] `NormalBattle` 显示 BattlePanel，且 HUD 保留。
- [ ] `Cave` 无战斗时显示 CavePanel。
- [ ] 获得足够修为后显示三个武学选择和刷新按钮。
- [ ] Boss 结束后显示 ResultPanel，重新开始按钮可开启新一局。

## 10. 需要保存的资产

- `Assets/GameData/Generated/GameDatabase.asset`
- `Assets/Scripts/Config/SpawnPrefabCatalog.asset`
- `Assets/Prefabs/Architecture/**`
- `Assets/Scenes/MainPrototype_Architecture.unity`
- `Assets/Editor/ArchitectureSceneAutomation.cs`
- Unity 自动生成的 `.meta` 文件
