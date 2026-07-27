# 当前技能与局内养成系统实现

本文档基于 2026-07-27 仓库中的代码、CSV、`GameDatabase.asset`、场景、Prefab、UI、美术资源和 EditMode 测试整理，描述的是“当前已经实现并实际接入的内容”，不是只依据策划案推测的目标形态。

本文所说的“养成”仅指单局内成长：修为、等级、武学、装备、铜钱、恢复与战斗属性。项目当前没有复杂局外养成、账号、云存档或长期装备词条系统。

## 1. 结论摘要

项目当前并行保留两套技能与养成实现：

| 实现 | 当前状态 | 实际入口 | 主要特点 |
| --- | --- | --- | --- |
| 旧原型链路 | 默认场景实际运行 | `MainPrototype.unity` | 功能较完整，IMGUI、武学图标、装备背包、洞穴商人和九门武学已能形成局内成长闭环，但数据与规则主要硬编码 |
| 新架构链路 | 代码、CSV、数据库和部分运行时对象已实现，场景闭环未接通 | `MainPrototype_Architecture.unity` 中的 `RunManager` 等对象 | 纯 C# 服务、稳定 ID、连续多级突破、数据驱动和可测试性更好，但 uGUI、生成器、敌人 Prefab 和部分效果仍未完成接入 |

当前最重要的事实是：

1. 默认可玩的 `MainPrototype.unity` 不读取 `martial_arts.csv`、`equipment.csv` 和 `rewards.csv`，而是继续使用旧脚本、场景序列化数值和中文武学名。
2. 新架构已能从 CSV 生成的 `GameDatabase.asset` 读取九门武学、六件装备和七组奖励，但 `GameUiPresenter`、`LevelUpView` 尚未放入架构场景，三个生成器的引用仍为空。
3. 新旧实现的规则并非完全等价：连续升级、候选生成、铁布衫气血计算、吸星诀毒伤回血、装备发放与部分装备特效存在差异。
4. 旧 UI 已绑定 6 张武学图标和 5 张装备图标；剩余武学和毒镖囊使用回退图标。新 `LevelUpView` 目前没有图标字段，无法直接展示这些美术资源。
5. 三条核心时间规则在旧流程代码和新 `RunTimerService` 中均被保留；升级阶段会停止主地图计时，但新架构尚未统一冻结玩家移动、物理和全局 `Time.timeScale`。

## 2. 当前养成内容边界

### 2.1 成长维度

当前单局成长由以下部分组成：

- **修为与等级**：修为达到阈值后升级，并进入武学三选一。
- **武学**：快剑、毒掌、铁壁三条流派，共 9 门武学，每门最多三重。
- **装备**：兵器、护甲、饰物三个槽位，共 6 件机制装备。
- **铜钱**：旧洞穴商人可消费铜钱购买恢复、稀有装备或随机武学。
- **恢复**：药草、奖励配置、商人药品、吸血和部分装备效果恢复气血。
- **战斗属性**：最大气血、攻击、防御、攻速、暴击、吸血、闪避、移速，以及新领域层预留的伤害加成、减伤和恢复。

### 2.2 升级需求

新旧两套实现都使用以下修为阈值：

| 当前等级 | 升到下一级所需修为 |
| ---: | ---: |
| 1 | 20 |
| 2 | 35 |
| 3 | 55 |
| 4 | 80 |
| 5 及以后 | 120 |

数组最后一项会被重复使用，因此当前代码没有硬性等级上限。

### 2.3 当前没有实现的养成

- 复杂局外天赋树、门派成长或永久属性。
- 武学解锁存档和跨局收集。
- 装备随机词条、强化、锻造、套装和耐久。
- 武学主动释放、真气消耗、冷却 UI 和技能栏操作。
- Boss 奖励的实际发放：新配置存在 `reward_boss`，但 `RunManager.OnBossBattleFinished` 没有调用奖励服务；旧流程同样直接进入结算。

## 3. 两套实现与数据源

### 3.1 旧原型：当前默认运行时事实来源

```text
场景中的 EncounterTrigger 数值
  -> GameFlowController
  -> PlayerStats / PlayerEquipment
  -> MartialArtCatalog（中文名硬编码）
  -> BattleManager（按中文武学名读取效果）
  -> PrototypeHUDController / BattleScreenController / CaveRoomController
```

旧链路的实际来源包括：

- `Assets/Scripts/MartialArts/MartialArtCatalog.cs`：九门武学、流派、说明和各重效果文本。
- `Assets/Scripts/Player/PlayerStats.cs`：修为、等级、铜钱、武学等级和三种直接属性武学。
- `Assets/Scripts/Player/PlayerEquipment.cs`：六件装备模板、背包、穿戴、自动换装和装备战斗效果。
- `Assets/Scenes/MainPrototype.unity`：地图中每个敌人、精英、宝箱、药草和洞穴入口的实际奖励数值。
- `Assets/Scripts/Battle/BattleManager.cs`：剑气、破甲、毒、护盾、反震、毒伤回血和装备触发。

旧链路使用中文武学名作为逻辑键，例如 `剑气诀`、`毒砂掌`；装备使用 `qinggang_sword` 这类无前缀 ID。

### 3.2 新架构：目标数据驱动来源

```text
Assets/GameData/Tables/*.csv
  -> GameDatabaseImporter
  -> Assets/GameData/Generated/GameDatabase.asset
  -> CharacterManager / RunManager / BattleRunner
  -> ProgressionService / RewardService / CharacterFactory / BattleService
  -> GameUiPresenter / LevelUpView / HudView
```

新链路使用稳定英文 ID：

- 武学：`skill_sword_qi`、`skill_venom_palm` 等。
- 装备：`equipment_qinggang_sword`、`equipment_black_iron_ring` 等。
- 奖励：`reward_normal_enemy`、`reward_treasure` 等。

CSV 是新架构的唯一源数据，运行时只读取 `GameDatabase.asset`。`GameDatabaseValidator` 会校验稳定 ID、武学等级数组长度、概率范围和跨表引用。

### 3.3 当前双轨风险

同一内容目前需要在两处维护：

| 内容 | 旧原型 | 新架构 |
| --- | --- | --- |
| 武学数值 | `MartialArtCatalog`、`PlayerStats`、`BattleManager` | `martial_arts.csv` |
| 装备数值 | `PlayerEquipment.BuildTreasurePool/ResetRun` | `equipment.csv` |
| 奖励数值 | 场景 `EncounterTrigger`、`GameFlowController`、`CaveRoomController` | `rewards.csv`、`spawns.csv` |
| 显示 ID | 中文武学名、旧装备 ID | 稳定英文 ID + 中文 `display_name` |
| UI 图标映射 | `PrototypeHUDController` Inspector 数组 | 新 uGUI 尚无图标映射实现 |

因此，在新场景完全替换旧链路前，修改 CSV 不会自动改变默认场景中的武学、装备或奖励行为。

## 4. 核心脚本与组件职责

### 4.1 旧原型脚本

| 文件 | 主要职责 |
| --- | --- |
| `Assets/Scripts/GameFlow/GameFlowController.cs` | 单局阶段、主时间、奖励结算、升级入口、候选生成、刷新、洞穴和 Boss 跳转 |
| `Assets/Scripts/Player/PlayerStats.cs` | 等级、修为、铜钱、击杀、洞穴次数、武学等级和直接属性变更 |
| `Assets/Scripts/MartialArts/MartialArtCatalog.cs` | 九门武学的硬编码定义、三流派、起手武学和三重说明 |
| `Assets/Scripts/Player/PlayerEquipment.cs` | 三槽装备、背包、穿脱、自动装备、装备池、属性和触发参数 |
| `Assets/Scripts/Player/EquipmentItem.cs` | 单件装备的属性、稀有度、战斗机制参数和 UI 摘要 |
| `Assets/Scripts/Battle/BattleManager.cs` | 读取已学武学与已穿装备，执行具体战斗效果 |
| `Assets/Scripts/UI/PrototypeHUDController.cs` | 主 HUD、角色状态、武学列表、装备背包、升级三选一、刷新和调试入口 |
| `Assets/Scripts/UI/BattleScreenController.cs` | 战斗画面、伤害反馈和已学武学简要显示 |
| `Assets/Scripts/Cave/CaveRoomController.cs` | 洞穴战斗、宝藏、商人、铜钱消费、随机装备和随机武学 |
| `Assets/Scripts/Map/EncounterTrigger.cs` | 场景遭遇类型、敌人属性、修为/铜钱/恢复奖励和防重复触发 |

### 4.2 新架构脚本

| 层级 | 文件 | 主要职责 |
| --- | --- | --- |
| Domain | `Domain/Configuration/GameConfigModels.cs` | 角色、武学、装备、奖励和生成规则模型 |
| Domain | `Domain/Characters/CharacterRuntime.cs` | 当前气血、属性修正和重算 |
| Domain | `Domain/Characters/CharacterStats.cs` | 不可变战斗属性和加法/乘基础值公式 |
| Application | `Application/Progression/ProgressionService.cs` | 修为结算，支持一次奖励连续升级 |
| Application | `Application/Rewards/RewardService.cs` | 恢复气血并返回修为、铜钱、装备 ID、武学 ID |
| Application | `Application/Characters/CharacterFactory.cs` | 创建角色、应用武学/装备属性、构造战斗效果 |
| Application | `Application/Combat/CombatEffectRegistry.cs` | 按 `CombatEffectType` 聚合剑气、毒、破甲、护盾、反震和吸血 |
| Application | `Application/Combat/BattleService.cs` | 使用角色属性和效果列表执行确定性自动战斗 |
| Architecture | `Architecture/Characters/CharacterManager.cs` | 本局等级、修为、铜钱、武学等级、装备槽和奖励落地 |
| Architecture | `Architecture/GameFlow/RunManager.cs` | 状态、奖励入口、连续突破、候选、刷新、恢复阶段和 Boss 跳转 |
| Architecture | `Architecture/Battle/BattleRunner.cs` | 从数据库创建敌人，并在每场战斗开始时构造玩家效果列表 |
| UI | `Architecture/UI/GameUiPresenter.cs` | 订阅运行状态，刷新 View，并把按钮事件转发给 `RunManager` |
| UI | `Architecture/UI/LevelUpView.cs` | 三个选择按钮、文字和刷新按钮 |
| UI | `Architecture/UI/HudView.cs` | 时间、气血、等级、修为、铜钱和状态文本 |

## 5. 武学系统实现

### 5.1 流派与候选关系

当前共有三个流派：

- **快剑**：剑气诀、疾剑式、破甲掌。
- **毒掌**：毒砂掌、百毒心经、吸星诀。
- **铁壁**：铁布衫、金钟罩、反震诀。

每个流派有一门起手核心：剑气诀、毒砂掌、铁布衫。每门武学最多三重，重复选择会升阶，不会在已学列表中增加第二个条目。

### 5.2 九门武学的当前落地

| 武学 | 旧原型实现 | 新架构实现 | 当前差异 |
| --- | --- | --- | --- |
| 剑气诀 | 每 3/2/2 次有效命中追加 60%/80%/100% 攻击剑气 | CSV `SwordQi`，由 `CombatEffectRegistry.SwordQiDamage` 结算 | 主要行为一致 |
| 疾剑式 | 每重直接给 `attackSpeed +0.12` | CSV 总值 0.12/0.24/0.36，`CharacterFactory` 替换上一重总修正 | 最终数值一致，新实现更适合重算 |
| 破甲掌 | 每次命中累计破甲 0.35/0.70/1.05，上限为敌方基础防御 | CSV `ArmorBreakOnHit` | 主要行为一致 |
| 毒砂掌 | 每次命中施加 1/2/3 层毒 | CSV `PoisonOnHit` | 主要行为一致 |
| 百毒心经 | 毒上限 12/16/20，每层每秒伤害 0.80/1.05/1.30 | 同样通过 `PoisonOnHit` 的 `secondaryValue/maxStacks` 表达 | 主要行为一致 |
| 吸星诀 | 每重普攻吸血 +4%，毒伤回血 10%/20%/30% | 新战斗支持额外普攻吸血 4%/8%/12% | **新实现没有毒伤回血，`secondaryValue` 当前未被 `BattleService.ApplyPoisonTick` 使用** |
| 铁布衫 | 每重按“当前最大气血”再乘 15%，立即补足新增气血，防御 +1 | CSV 总修正 15%/30%/45% 基础气血，防御 +1/+2/+3；重算时保持当前气血比例 | **旧实现会逐重乘算并直接治疗，新实现按基础值非复利并保持血量比例** |
| 金钟罩 | 开战护盾为 8/16/24 + 防御×1.5/3/4.5 | CSV `OpeningShield` | 主要行为一致 |
| 反震诀 | 受伤后按防御×0.65/0.85/1.05 反击 | CSV `Retaliation` | 主要行为一致 |

### 5.3 属性武学与战斗武学的生效时机

新架构把武学分成两种落地方式：

1. `CombatEffectType.StatModifier`：学习时由 `CharacterFactory.ApplyMartialArt` 立即更新角色属性。当前是疾剑式和铁布衫。
2. 其他 `CombatEffectType`：只记录武学等级；下一场战斗开始时，`BattleRunner` 调用 `CharacterManager.BuildCombatEffects` 生成效果列表，再交给 `BattleService`。

这意味着战斗型武学的配置在“下一场战斗构造时”被快照使用；属性型武学在选择后立即反映到角色属性。

## 6. 装备系统实现

### 6.1 装备槽和起始装备

当前实际保留三个槽位：兵器、护甲、饰物。

每局开始时，旧 `PlayerEquipment.ResetRun` 会把以下三件装备加入背包并自动穿戴；新 `CharacterManager.StartNewRun` 也会按稳定 ID 直接装备同三件物品：

- 青钢剑：兵器。
- 轻鳞衣：护甲。
- 练功护腕：饰物。

### 6.2 六件装备的当前落地

| 装备 | 属性/效果 | 旧原型 | 新架构 |
| --- | --- | --- | --- |
| 青钢剑 | 攻击 +4；每 3 次命中追加 35% 攻击剑气 | 已实现 | 已实现 |
| 轻鳞衣 | 防御 +2、气血 +18；开战护盾 +10 | 已实现 | 已实现 |
| 练功护腕 | 攻速 +0.08；暴击后下次攻击间隔缩短 30% | 已实现 | 属性已实现，**`CriticalHaste` 战斗效果未实现** |
| 玄铁戒 | 攻击 +2、暴击 +4%；命中破甲 0.35 | 已实现 | 已实现 |
| 游侠披风 | 气血 +12、闪避 +4%；闪避时恢复 3% 气血 | 已实现 | 属性已实现，**`DodgeHeal` 战斗效果未实现** |
| 毒镖囊 | 攻击 +1；每次命中额外施加 1 层毒 | 已实现 | 已实现 |

### 6.3 旧装备背包规则

- 稀有装备池包含玄铁戒、游侠披风、毒镖囊。
- 宝箱、洞穴秘藏和洞穴商人会从未获得的稀有装备中随机抽取。
- 新装备进入背包后通过 `GetPowerScore` 判断是否强于当前同槽装备；更强时自动装备。
- 玩家可在主地图打开“装备背包”，手动装备或卸下；打开角色界面会把 `Time.timeScale` 设为 0。
- 增加最大气血时会治疗等额新增气血，卸下时把当前气血压到新上限。

### 6.4 新装备规则

- `CharacterManager` 只保存“槽位 -> 装备 ID”，没有背包集合、稀有度比较或战力评分。
- 获得装备会直接替换同槽装备，并移除旧装备的属性修正。
- 属性重算会保持当前气血比例，不会像旧实现一样治疗完整的新增最大气血。
- 当前 `reward_cave_treasure` 固定给玄铁戒；其他奖励没有装备 ID。
- `reward_treasure` 当前只给 18 修为和 10 铜钱，因此新架构宝箱不会像旧原型一样随机掉装备。
- 六件装备都存在于 CSV，但游侠披风、毒镖囊目前没有奖励配置入口。

## 7. 旧原型的一次升级调用链

以下是默认场景中“击败普通敌人并升级”的实际调用链：

```text
EncounterTrigger.OnTriggerEnter
  -> GameFlowController.HandleEncounter
  -> GameFlowController.BeginNormalBattle
  -> BattleManager.BeginBattle
  -> BattleManager.RunBattle 协程
  -> GameFlowController.OnNormalBattleFinished
  -> GameFlowController.GiveRewards
  -> PlayerStats.GainCopper
  -> PlayerStats.GainCultivation
  -> GameFlowController.EnterLevelUp
  -> GameFlowController.GenerateMartialArtChoices
  -> GameFlowController.SetPhase(LevelUpPaused)
  -> PrototypeHUDController.OnGUI
  -> PrototypeHUDController.DrawLevelUpPanel
  -> 玩家点击武学卡
  -> GameFlowController.ChooseMartialArt
  -> PlayerStats.ApplyMartialArt
  -> 恢复 MainMapRunning / CaveRunning，或进入 BossBattle
```

### 7.1 奖励进入升级

`GameFlowController.GiveRewards` 先增加铜钱，再调用 `PlayerStats.GainCultivation`。

旧 `GainCultivation` 每次调用最多只提升一级：

```text
当前修为 + 奖励
  < 当前阈值：不升级
  >= 当前阈值：只扣一次阈值、等级 +1、返回 true
```

因此一笔足以跨多级的奖励不会连续弹出多个突破；多余修为会保留，等待下一次奖励调用再触发下一次升级。调试按钮一次增加 25 修为也走同一入口。

### 7.2 旧候选生成规则

`GenerateMartialArtChoices` 的规则是：

1. 排除已经三重的武学。
2. 未学过任何武学时，只允许三门起手核心，并分别从快剑、毒掌、铁壁各取一门。
3. 已有武学后：
   - 取一门当前总等级最高流派的候选。
   - 取一门其他流派的起手核心，提供转向机会。
   - 再从全部可用候选中取一门。
4. 去重、补足到最多三项，再随机打乱顺序。
5. 每局只有一次“重观残页”刷新，刷新不会恢复次数。

非起手武学只有在玩家已经拥有该流派至少一门武学后才会进入候选池。

### 7.3 玩家选择与即时生效

`ChooseMartialArt` 调用 `PlayerStats.ApplyMartialArt`：

- 更新 `martialArtRanks`。
- 第一次学习时把中文武学名加入 `learnedMartialArts`。
- 疾剑式、铁布衫、吸星诀立即修改 `runtimeStats`。
- 其他武学由下一场或后续战斗中的 `BattleManager` 按等级读取。

### 7.4 升级后的阶段恢复

- 普通战斗奖励升级：`phaseBeforeLevelUp` 被设为 `MainMapRunning`。
- 洞穴战斗或洞穴奖励升级：恢复为 `CaveRunning`。
- 如果主时间在普通战斗中已经归零，先完成武学选择，再进入 Boss 战。
- `GameFlowController.Update` 只在主地图和普通战斗减少主时间，因此 `LevelUpPaused` 不消耗主时间。
- `SetPhase` 会关闭玩家移动，但升级阶段没有设置全局 `Time.timeScale = 0`；它是状态级暂停，不是全局 Unity 时间暂停。

### 7.5 其他旧成长入口

| 入口 | 调用链与结果 |
| --- | --- |
| 宝箱 | `HandleEncounter -> GiveRewards -> GrantTreasureEquipment`；给修为、铜钱和随机未获得稀有装备 |
| 药草 | `HandleEncounter -> HealPercent`；不增加修为 |
| 洞穴战斗 | `BeginCaveBattle -> BattleManager -> GiveRewards`；升级后回到洞穴 |
| 洞穴秘藏 | `GrantCaveTreasure`；硬编码给 18 修为、10 铜钱、随机装备和随机武学 |
| 洞穴商人 | 花 6/10/14 铜钱购买 45% 恢复、稀有装备或随机武学 |
| 随机武学 | `GrantRandomMartialArt -> PlayerStats.ApplyMartialArt`；绕过三选一直接学习或升阶 |

旧场景中的普通怪、精英、宝箱奖励由每个 `EncounterTrigger` 单独序列化，并非统一表值；同类型敌人的修为奖励存在 9、10、12、13、14、16、18、20、21、24、25 等多组数值。

## 8. 新架构的一次升级调用链

新架构中，普通战斗升级链路设计为：

```text
EnemyEncounter.Interact
  -> RunManager.TryBeginNormalBattle
  -> BattleRunner.BeginBattle
  -> BattleService
  -> RunManager.OnNormalBattleFinished
  -> RunManager.GrantPendingReward
  -> CharacterManager.GrantReward
      -> RewardService.Apply
      -> ProgressionService.AddCultivation
      -> 可选 Equip
      -> 可选 LearnMartialArt
  -> CharacterRewardGrant(LevelsGained)
  -> RunManager.EnterLevelUp
  -> RunManager.GenerateMartialArtChoices
  -> RunManager.SetState(LevelUp)
  -> GameUiPresenter.Refresh
  -> LevelUpView.Render
  -> 玩家点击 Button
  -> LevelUpView.ChoiceRequested
  -> GameUiPresenter.ChooseMartialArt
  -> RunManager.ChooseMartialArt
  -> CharacterManager.LearnMartialArt
  -> CharacterFactory.ApplyMartialArt
  -> 若仍有 pendingLevelUps，再生成下一轮候选
  -> 恢复 MainMap / Cave，或进入 BossBattle
```

### 8.1 奖励结算顺序

`CharacterManager.GrantReward` 的顺序是：

1. `RewardService.Apply` 根据 `healRatio` 恢复气血，并返回修为、铜钱、装备 ID、武学 ID。
2. 增加铜钱。
3. `ProgressionService.AddCultivation` 使用 `while` 连续扣除升级需求，返回实际提升等级数。
4. 如果奖励配置有装备 ID，直接装备。
5. 如果奖励配置有武学 ID，直接学习或升阶。
6. 触发 `Changed`，返回 `CharacterRewardGrant`。

当前 `rewards.csv` 没有任何 `martial_art_id`，所以“奖励直接给固定武学”的代码路径存在但没有实际配置入口。

### 8.2 连续多级突破

新 `ProgressionService` 可以正确处理一笔大额修为。例如从 1 级、0 修为获得 120 修为后：

```text
扣 20 -> 2 级，剩 100
扣 35 -> 3 级，剩 65
扣 55 -> 4 级，剩 10
```

`RunManager.pendingLevelUps` 会记录 3 次突破。玩家每选择一门武学后减 1；仍有待处理等级时立即生成下一轮候选，全部完成后才恢复原状态或进入 Boss。

### 8.3 新候选生成规则

当前新算法比旧算法简单：

- 未学任何武学：只把 `isStarter = true` 的武学放入候选池，随机取最多三项。
- 已学至少一门武学：所有未满三重的武学都可进入候选池，随机取最多三项。
- 没有计算主流派，也没有保证“同流派协同 + 其他流派起手 + 随机项”。
- 每局共享一次刷新；连续多级突破不会为每一级恢复刷新次数。

这与策划案和旧原型中“后续三选一保留同流派协同与转向机会”的规则不完全一致。

### 8.4 新选择后的属性和战斗效果

`CharacterManager.LearnMartialArt` 更新稳定 ID 对应的等级，然后：

- 属性型武学立即通过来源 ID `martial:<skill_id>` 替换旧总修正，避免重复叠加和顺序依赖。
- 战斗型武学不直接改属性；`BattleRunner.BeginBattle` 在下一场战斗开始时构建 `CombatEffectDefinition`。
- 装备使用来源 ID `equipment:<equipment_id>`，替换装备时可完整移除旧属性。

### 8.5 新升级暂停的当前边界

- `RunTimerService` 只在 `MainMap` 和 `NormalBattle` 减少主时间，所以 `LevelUp` 不消耗主时间。
- `RunManager` 的 `explicitlyPaused` 可以停止所有计时，但 `SetExplicitPause` 当前没有调用方。
- `RunManager` 没有引用旧 `PlayerController`，也没有新的移动门控接口；因此即使 uGUI 接通，仍需要补充“LevelUp 时禁止移动/交互”的统一控制。
- 新升级状态没有设置全局 `Time.timeScale = 0`，物理、动画和其他未受状态管理的系统不会自动冻结。

## 9. UI 实现现状

### 9.1 默认场景实际使用的 IMGUI

`PrototypeHUDController` 已在 `MainPrototype.unity` 和 `MainPrototype_Architecture.unity` 中绑定并启用，提供：

- 主地图剩余时间、等级、气血、修为进度、铜钱和状态信息。
- 角色状态页：主要属性、击杀、洞穴次数、已学武学和各重效果。
- 装备背包页：三个穿戴槽、装备图标、属性摘要、装备/卸下按钮。
- 升级三选一：武学图标、升到第几重、下一重效果、流派/类别、详细说明和一次刷新。
- 调试按钮：增加修为、增加战力等。

`BattleScreenController` 在战斗中接管显示，并最多列出两门已学武学及重数。

### 9.2 新 uGUI 代码能力

`GameUiPresenter`、`HudView`、`LevelUpView`、`BattleView` 和 `ResultView` 已完成脚本实现，但当前界面能力较基础：

| View | 当前显示 | 养成相关缺口 |
| --- | --- | --- |
| `HudView` | 时间、气血、等级、当前修为、铜钱、状态 | 不显示下一级需求、主要武学、装备或战力 |
| `LevelUpView` | 武学显示名、通用描述、刷新次数 | 无图标、无流派、无当前/下一重、无实际数值摘要、无详情区 |
| `BattleView` | 双方气血、护盾、破甲、毒层、战斗时间 | 不显示当前触发武学、装备和效果日志 |
| `ResultView` | 胜负、击杀、洞穴、等级、铜钱、Boss 时间 | 不显示关键武学、装备和本局构筑 |

### 9.3 新 uGUI 场景接入状态

`MainPrototype_Architecture.unity` 中没有以下组件的序列化实例：

- `GameUiPresenter`
- `LevelUpView`
- `HudView`
- `BattleView`
- `ResultView`

因此新 `RunManager.StartRun`、选择按钮和刷新事件目前没有用户可操作入口；架构场景仍显示旧 `PrototypeHUDController`。

## 10. 场景、Prefab 与组件接入状态

### 10.1 `MainPrototype.unity`

- 实际运行旧 `GameFlowController`、`PlayerStats`、`PlayerEquipment`、`BattleManager` 和 IMGUI。
- 玩家、敌人、宝箱、药草和洞穴主要使用旧 `EncounterTrigger`。
- 没有 `GameDatabaseProvider`、`CharacterManager`、`RunManager` 或新 uGUI。
- 当前可用于验证旧养成闭环。

### 10.2 `MainPrototype_Architecture.unity`

已存在并绑定：

- `GameDatabaseProvider -> GameDatabase.asset`
- `CharacterManager -> GameDatabaseProvider`
- `BattleRunner -> CharacterManager`
- `RunManager -> CharacterManager/BattleRunner`

仍未绑定：

- `RunManager.enemySpawner/itemSpawner/caveSpawner`
- 三个 Spawner 的 `databaseProvider`
- 三个 Spawner 的 `prefabCatalog`
- 三个 Spawner 的 `regions`
- 三个 Spawner 的 `runManager`
- 新 uGUI 和 `GameUiPresenter`

架构场景同时保留并启用了旧 `GameFlowController`、旧 `PlayerStats`、旧 `BattleManager` 和旧 IMGUI，所以当前不是已经切换完成的新闭环。

### 10.3 玩家 Prefab

`Assets/Prefabs/Player/Player.prefab` 同时包含：

- 旧 `PlayerStats`
- 旧 `PlayerEquipment`
- 新 `PlayerInteractionActor`

新交互标记已添加，但 `RunManager` 尚未负责玩家移动状态。

### 10.4 生成目录与敌人 Prefab

`SpawnPrefabCatalog.asset` 已建立 7 个映射，但当前存在接入错位：

- 宝箱、药草和洞穴 Prefab 已包含新 `TreasureChest/HerbPickup/CaveEntrance` 与 `WorldInteractionTrigger`，同时仍保留旧 `EncounterTrigger`。
- `南坡恶徒.prefab` 包含新 `EnemyEncounter` 与 `WorldInteractionTrigger`，但它没有被当前 `SpawnPrefabCatalog` 使用。
- 当前被 `SpawnPrefabCatalog` 使用的四个敌人 Prefab——山贼喽啰、流寇、灰岩巨鼠、黑风刀客——仍只有旧 `EncounterTrigger`，缺少 `EnemyEncounter`。
- 即使生成器引用补齐，`EnemySpawner.ConfigureEnemy` 也会因为找不到 `EnemyEncounter` 打出警告，生成的敌人无法进入新战斗链路。
- 如果在保留旧系统时直接启用同时含新旧交互组件的物品/洞穴 Prefab，可能发生两套触发器竞争或重复结算，迁移场景中必须禁用其中一套。

## 11. 美术资源与绑定

### 11.1 武学图标

当前目录 `Assets/Art/Generated/Icons/Skills/` 有 6 张 128 × 128 PNG：

| 武学 | 资源 |
| --- | --- |
| 剑气诀 | `ico_skill_jianqi_v01_128.png` |
| 疾剑式 | `ico_skill_jijian_v01_128.png` |
| 破甲掌 | `ico_skill_pojiazhang_v01_128.png` |
| 毒砂掌 | `ico_skill_dushazhang_v01_128.png` |
| 吸星诀 | `ico_skill_xixing_v01_128.png` |
| 铁布衫 | `ico_skill_tiebushan_v01_128.png` |

缺少独立图标：百毒心经、金钟罩、反震诀。

旧 UI 的回退规则：

- 百毒心经复用毒砂掌图标。
- 金钟罩、反震诀复用铁布衫图标。

### 11.2 装备图标

当前目录 `Assets/Art/Generated/Icons/Equipment/` 有 5 张 128 × 128 PNG：

- 青钢剑。
- 轻鳞衣。
- 练功护腕。
- 玄铁戒。
- 游侠披风。

毒镖囊没有独立图标，旧 UI 会回退复用玄铁戒图标。

### 11.3 当前绑定与制作状态

- 两个场景中的旧 `PrototypeHUDController` 都已绑定上述 6 张武学图标和 5 张装备图标。
- 图标使用手绘武侠、墨线、朱红/暗金/玉色的视觉语言，与 `art_production_pipeline.md` 一致。
- 默认导入配置为 Sprite、Single、Bilinear、Mip Map Off、透明通道、默认平台 Max Size 128、无压缩。
- 美术生产文档把这批图标标记为 `InEngineQA`，尚不能写成最终 `Approved` 正式资源。
- 新 `LevelUpView` 只持有 `Button[]` 和 `Text[]`，没有 `Image[]` 或稳定 ID 到 Sprite 的目录，因此当前不能在新 uGUI 中绑定这些图标。
- 旧图标映射使用中文武学名和旧装备 ID，新数据层使用稳定英文 ID；迁移时需要建立统一的 `visual_id/icon_id` 或图标目录映射，不能直接复用旧 Inspector 键值。

## 12. 当前奖励配置

新架构 `rewards.csv` 当前内容：

| 奖励 ID | 修为 | 铜钱 | 恢复 | 装备 | 武学 |
| --- | ---: | ---: | ---: | --- | --- |
| `reward_normal_enemy` | 10 | 2 | 0 | 无 | 无 |
| `reward_elite_enemy` | 20 | 5 | 0 | 无 | 无 |
| `reward_treasure` | 18 | 10 | 0 | 无 | 无 |
| `reward_herb` | 0 | 0 | 35% | 无 | 无 |
| `reward_cave_enemy` | 25 | 8 | 0 | 无 | 无 |
| `reward_cave_treasure` | 18 | 10 | 0 | 玄铁戒 | 无 |
| `reward_boss` | 60 | 30 | 0 | 无 | 无 |

当前接入注意：

- `reward_cave_treasure` 没有已接入的新洞穴宝藏 Prefab 或场景入口。
- `reward_boss` 没有在 Boss 胜利回调中发放。
- 精英奖励没有装备，宝箱奖励也没有装备，与旧原型和策划倾向不同。
- `spawns.csv` 只负责主地图敌人、宝箱、药草和洞穴入口；没有洞穴敌人或洞穴宝藏的生成规则。

## 13. 自动化测试覆盖

### 13.1 已覆盖

| 测试 | 已验证内容 |
| --- | --- |
| `ProgressionServiceTests` | 一笔 120 修为连续提升 3 级 |
| `RewardServiceTests` | 修为、铜钱、装备/武学 ID 返回和按最大气血比例恢复 |
| `CharacterFactoryTests` | 角色创建、武学升阶替换旧总修正 |
| `CharacterRuntimeTests` | 属性重算保持气血比例、乘基础值与加法顺序无关 |
| `BattleServiceTests` | 毒层、开战护盾和攻速 Tick |
| `RunTimerServiceTests` | 主地图/普通战斗计时、洞穴/升级暂停、Boss 独立计时 |
| `GameDatabaseValidatorTests` | 重复 ID、缺失引用和最小合法配置 |
| `GameDatabaseTests` | ScriptableObject 替换数据后可按 ID 查询 |

### 13.2 尚未覆盖

- 旧 `GameFlowController -> PlayerStats -> PrototypeHUDController` 升级链路没有自动测试。
- `CharacterManager.GrantReward/LearnMartialArt/Equip` 没有直接集成测试。
- `RunManager` 的候选生成、一次刷新、连续升级选择、洞穴恢复和倒计时归零后进 Boss 没有组件级测试。
- 九门武学没有逐项行为测试；当前只直接覆盖毒和护盾，铁布衫只覆盖属性重算的一部分。
- 剑气、破甲、反震、吸星诀、暴击加速、闪避回血和六件装备组合没有完整回归测试。
- `LevelUpView`、`GameUiPresenter`、按钮数组顺序和场景引用没有 PlayMode 测试。
- `SpawnPrefabCatalog` 指向的敌人 Prefab 是否包含 `EnemyEncounter` 没有自动校验。
- 实际 `GameDatabase.asset` 的 9/6/7 条内容和所有数值没有快照测试。

## 14. 已知问题与优先级

### P0：阻止新养成闭环运行

1. 架构场景没有新 uGUI 和 `GameUiPresenter`，无法开始新流程或完成武学选择。
2. 三个生成器和 `RunManager` 的生成器引用为空。
3. `SpawnPrefabCatalog` 使用的敌人 Prefab 缺少新 `EnemyEncounter`。
4. 新 `RunManager` 没有统一禁止玩家在升级、战斗和结果阶段移动/交互。

### P0：新旧战斗成长结果不一致

1. 新吸星诀没有毒伤回血。
2. 新练功护腕没有暴击加速。
3. 新游侠披风没有闪避回血。
4. 铁布衫和装备最大气血的治疗/重算语义与旧原型不同，需要确定目标规则后统一。

### P1：构筑与奖励体验不一致

1. 新候选算法没有保证同流派协同和转向核心。
2. 新宝箱和精英奖励不掉装备，洞穴宝藏与 Boss 奖励未接入。
3. 新装备系统没有背包、升级比较、手动换装或防止低价值装备覆盖高价值装备的规则。
4. 候选池全部满阶时，新旧流程都会进入没有可选项的升级状态，需要提供替代奖励或自动跳过。

### P1：UI 与美术不完整

1. 新三选一没有图标、重数、实际数值和流派信息。
2. 新 HUD 不显示下一级需求和主要武学。
3. 新结算不显示关键武学和装备。
4. 百毒心经、金钟罩、反震诀、毒镖囊缺少独立图标。
5. 图标仍处于 `InEngineQA`，需要在 64/48/32 px 下完成 Play Mode 验收。

### P2：维护性

1. 旧中文 ID、旧装备 ID和新稳定 ID并存。
2. 默认场景奖励散落在大量 `EncounterTrigger` 中，与 CSV 不同步。
3. `docs/unity_editor_architecture_binding.md` 的部分“尚无 Prefab”描述已经落后于仓库现状，后续迁移时应按本文件和实际资源复核。

## 15. 建议的完善顺序

### 阶段一：先让新升级闭环可运行

1. 在 `MainPrototype_Architecture.unity` 创建并绑定新 uGUI。
2. 给 `RunManager` 增加玩家控制门控，至少在非 `MainMap` 状态禁止移动与世界交互。
3. 补齐生成器引用、区域和 `SpawnPrefabCatalog`。
4. 为目录中实际使用的四个敌人 Prefab 增加新交互组件，并在架构版本中禁用旧触发器。
5. 通过一次普通怪升级、一次宝箱升级和一次洞穴升级验证完整调用链。

### 阶段二：统一成长规则

1. 把旧候选算法中的“主流派协同 + 转向核心”迁入可测试的纯 C# 服务。
2. 明确铁布衫和最大气血装备是“保持血量比例”还是“补足新增气血”，两套实现统一为一条规则。
3. 实现 `CriticalHaste`、`DodgeHeal` 和吸星诀毒伤回血。
4. 明确装备获得是直接替换、自动比较还是保留简化背包，再统一奖励和 UI。
5. 接通宝箱、精英、洞穴宝藏和 Boss 奖励。

### 阶段三：统一 UI 与美术 ID

1. 为武学和装备配置增加可稳定查询的图标 ID，或建立独立 `ProgressionIconCatalog`。
2. 扩展 `LevelUpView`：图标、流派、当前重数、下一重实际效果、详情区。
3. 扩展 HUD、角色页和结算页，显示当前构筑。
4. 补齐 3 张武学和 1 张装备独立图标并完成小尺寸验收。

### 阶段四：测试和退役旧链路

1. 为候选生成、连续升级、奖励、九门武学和六件装备增加纯 C# 测试。
2. 增加 PlayMode 测试或 Editor 校验，检查按钮数组、Presenter 引用和 Prefab 组件。
3. 新架构完整通过后，在架构场景中依次停用旧 IMGUI、旧触发器、旧流程和旧角色数据。
4. 保留 `MainPrototype.unity` 作为回退场景，直到新场景连续通过完整闭环验证。

## 16. Unity Editor 后续绑定清单

本次只生成文档，没有修改场景。要实际启用新养成系统，Unity Editor 至少需要：

### Runtime

- `RunManager.characterManager`：已绑定。
- `RunManager.battleRunner`：已绑定。
- `RunManager.enemySpawner/itemSpawner/caveSpawner`：待绑定。
- 三个 Spawner 的 `databaseProvider/prefabCatalog/regions/runManager`：待绑定。

### 玩家与 Prefab

- 确认架构场景玩家实例包含 `PlayerInteractionActor`。
- 给 Spawn Catalog 使用的四个敌人 Prefab 增加 `EnemyEncounter` 和 `WorldInteractionTrigger`。
- `WorldInteractionTrigger.interactableComponent` 绑定同对象的交互组件。
- 架构场景中禁用相同对象上的旧 `EncounterTrigger`，避免双触发。

### LevelUpPanel

- 创建 3 个武学 Button 和 3 个对应 Text。
- `LevelUpView.choiceButtons` 与 `choiceLabels` 保持相同顺序和长度。
- 创建刷新 Button 和刷新次数 Text，绑定 `rerollButton/rerollText`。
- 创建 `GameUiPresenter`，绑定 `RunManager` 和全部 View。
- 当前脚本没有图标字段；要显示图标必须先扩展代码，不能只靠 Inspector 完成。

## 17. 核心时间规则核对

| 规则 | 旧原型 | 新架构 |
| --- | --- | --- |
| 普通战斗主时间继续 | `GameFlowController.Update` 在 `NormalBattleRunning` 减少主时间 | `RunTimerService` 在 `NormalBattle` 减少主时间 |
| 洞穴主时间暂停 | `CaveRunning` 不减少主时间 | `Cave` 不减少主时间 |
| 升级选择暂停主时间 | `LevelUpPaused` 不减少主时间，玩家移动关闭 | `LevelUp` 不减少主时间；玩家移动门控仍待接入 |
| 普通战斗中归零 | 完成战斗和升级后进入 Boss | `bossTransitionPending`，完成战斗和全部待处理升级后进入 Boss |
| Boss 独立计时 | 只增加 `bossBattleTime`，不再减少主时间 | 只增加 `BossBattleTime`，主时间保持 0 |

三条项目硬规则在逻辑层均保留。本次文档整理没有修改计时、战斗、洞穴或 Boss 行为。

## 18. 维护入口

在完成旧链路退役前，修改养成内容应按以下方式核对：

1. 修改默认可玩场景的武学：同时检查 `MartialArtCatalog`、`PlayerStats.ApplyMartialArt` 和 `BattleManager`。
2. 修改新架构武学：修改 `martial_arts.csv`，重新执行 `Tools > 一炷江湖 > 导入 CSV 配置`，并检查对应 `CombatEffectType` 是否已经在 `CombatEffectRegistry/BattleService` 实现。
3. 修改旧装备：检查 `PlayerEquipment.ResetRun/BuildTreasurePool` 和 `BattleManager`。
4. 修改新装备：修改 `equipment.csv`，确认属性和战斗效果两部分都已实现。
5. 修改奖励：确认目标是旧场景 `EncounterTrigger` 还是新 `rewards.csv/spawns.csv`，不要误以为两者会自动同步。
6. 修改升级流程后，必须重新验证普通战斗继续计时、洞穴暂停、升级完成后恢复正确状态、普通战斗归零后延迟进入 Boss，以及 Boss 独立计时。
