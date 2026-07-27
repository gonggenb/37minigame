# 当前战斗系统

> 分析日期：2026-07-27  
> 文档范围：当前仓库中的战斗规则、运行链路、配置、表现、测试和迁移状态。  
> 事实优先级：场景实际绑定与代码行为 > CSV/生成资产 > 策划说明。

## 1. 当前结论

项目目前并存两套战斗实现：

1. **旧原型战斗链路**已经接入 `MainPrototype.unity`，也是当前构建设置中实际可玩的闭环。
2. **新架构战斗链路**已经完成 Domain/Application 纯 C# 核心、Unity 适配器、CSV 数据和 EditMode 测试，但 `MainPrototype_Architecture.unity` 的关键 Inspector 引用、生成器和 uGUI 尚未完整绑定，当前不能替代旧链路运行完整一局。

当前构建场景只有：

```text
Assets/Scenes/MainPrototype.unity
```

因此，分析“当前玩家实际体验”时应以旧原型链路为准；分析“后续维护方向”时应以新架构链路为准。

## 2. 两套实现的状态边界

| 项目 | 旧原型链路 | 新架构链路 |
| --- | --- | --- |
| 主要入口 | `EncounterTrigger` | `EnemyEncounter` / `CaveEntrance` |
| 流程协调 | `GameFlowController` | `RunManager` |
| 战斗执行 | `BattleManager` 协程 | `BattleRunner` + `BattleService` |
| 玩家运行时属性 | `PlayerStats.runtimeStats` | `CharacterManager.Player` |
| 数据来源 | 场景序列化数值 + 代码内武学/装备目录 | CSV -> `GameDatabase.asset` |
| 战斗 UI | IMGUI `BattleScreenController` | uGUI `BattleView` + `GameUiPresenter` |
| 自动化测试 | 没有直接覆盖 | 纯 C# 服务有 EditMode 测试 |
| 当前可玩状态 | 已接入，默认运行 | 代码存在，场景未完整接通 |

### 2.1 默认场景的实际运行链路

```text
玩家碰撞 EncounterTrigger
  -> GameFlowController.HandleEncounter
  -> 切换 GamePhase
  -> BattleManager.BeginBattle
  -> 自动攻击 / 武学与装备效果 / 胜负判定
  -> GameFlowController 接收战斗结果
  -> 奖励、突破选择、返回地图、进入 Boss 或结算
```

### 2.2 新架构的目标链路

```text
CSV 配置
  -> GameDatabase.asset
  -> CharacterManager 构造玩家与战斗效果
  -> EnemyEncounter 请求 RunManager 开战
  -> BattleRunner 构造 BattleService
  -> BattleService 逐 Tick 结算
  -> RunManager 发奖励、突破、切状态或结算
  -> GameUiPresenter 刷新 uGUI View
```

## 3. 战斗类型与时间规则

| 阶段 | 旧状态 | 新状态 | 主地图倒计时 | 结束去向 |
| --- | --- | --- | --- | --- |
| 普通/精英战斗 | `NormalBattleRunning` | `NormalBattle` | 继续流逝 | 胜利后奖励并返回地图；失败直接结算 |
| 洞穴战斗 | 仍保持 `CaveRunning` | 仍保持 `Cave` | 暂停 | 胜利后留在洞穴；失败直接结算 |
| 突破选择 | `LevelUpPaused` | `LevelUp` | 暂停 | 完成选择后回到地图或洞穴 |
| 最终 Boss | `BossBattle` | `BossBattle` | 固定为 0，不再使用 | 胜负后进入结算 |

三条核心时间规则在两套流程设计中都得到保留：

1. 普通战斗期间主地图时间继续减少。
2. 洞穴及洞穴战斗期间主地图时间不减少。
3. Boss 战使用独立计时，不消耗主地图时间。

普通战斗中主时间归零时不会中断当前战斗。旧链路通过 `IsBossTransitionPending`，新链路通过 `bossTransitionPending` 记录待切 Boss；战斗胜利及其突破选择完成后才开始 Boss 战。

需要注意，“突破时所有时间暂停”目前主要指游戏流程计时停止：旧链路进入 `LevelUpPaused` 后不再扣主时间，新链路的 `RunTimerService` 也不会在 `LevelUp` 中计时。它们不等同于统一把 Unity 的 `Time.timeScale` 设为 0。

## 4. 旧原型战斗系统：当前实际运行版本

### 4.1 遭遇与开战

`EncounterTrigger.OnTriggerEnter` 检测到 `PlayerController` 后，把遭遇交给 `GameFlowController.HandleEncounter`。

- 普通怪和精英怪：立即消耗地图对象，复制其场景内 `enemyStats`，进入普通战斗。
- 洞穴入口：进入 `CaveRunning`；若洞穴内容为敌人，再由 `CaveRoomController` 调用同一个 `BattleManager`。
- 宝箱和药草不进入战斗，直接发奖励或恢复气血。
- Boss 不依赖地图碰撞，由主时间归零或调试命令触发。

普通敌人、精英、洞穴敌人的数值主要序列化在场景的各个 `EncounterTrigger` 上，因此同一视觉类型可以存在多套气血、攻击、防御、攻速和奖励数值。旧场景并不读取新 CSV 作为战斗数值来源。

### 4.2 战斗初始化

`BattleManager.BeginBattle` 每场都会重置以下战斗内状态：

- 当前敌人和战斗日志。
- 战斗用时、攻击序号和上一次攻击结果。
- 玩家本场成功命中次数。
- 敌人毒层和累计破甲。
- 玩家开场护盾。
- 毒伤 1 秒结算冷却。

玩家的当前气血不会在普通战斗和洞穴战斗之间自动恢复，构成整局续航压力。进入最终 Boss 战前，`GameFlowController` 会把玩家气血恢复到当前最大值。

### 4.3 自动攻击节奏

战斗由协程逐帧执行，默认 `battleSpeedMultiplier = 1.5`。

- 玩家初次攻击等待约 `0.2 / 1.5` 秒。
- 敌人初次攻击等待约 `0.7 / 1.5` 秒。
- 后续攻击间隔为 `1 / 攻速`，再由战斗速度倍率加速冷却。
- 每帧结算顺序是：玩家攻击 -> 毒伤 Tick -> 敌人攻击。
- 敌人若已被玩家攻击或毒伤击败，本帧不会继续攻击。
- `BattleElapsed` 记录真实经过的 `Time.deltaTime`，不是乘过 1.5 倍的模拟时间。

普通战斗的主地图倒计时始终按真实 `Time.deltaTime` 扣减，因此加速战斗模拟不会额外加速 60 秒主时间。

### 4.4 基础伤害公式

旧链路的单次普攻按以下顺序结算：

```text
有效防御 = 防御方防御
若玩家攻击当前敌人：有效防御 -= 本场累计破甲，最低为 0

基础伤害 = max(1, 攻击方攻击 - 有效防御)
若暴击：基础伤害 *= 暴击伤害倍率
若敌人攻击玩家：先由玩家护盾吸收
剩余伤害扣除气血
```

判定顺序是先闪避、再计算防御和暴击。闪避成功时本次伤害为 0，也不会触发命中型破甲、施毒、剑气或反震。

旧链路的吸血使用本次计算伤害与剑气伤害之和，不按敌人实际损失气血截断，因此击杀时的过量伤害也可能计入吸血；最终恢复仍受玩家最大气血限制。

### 4.5 当前武学效果

每门武学最多三重。旧链路通过中文武学 ID 直接分支，实际效果如下：

| 流派 | 武学 | 当前行为 |
| --- | --- | --- |
| 快剑 | 剑气诀 | 第 1 重每 3 次命中追加 60% 攻击；第 2/3 重每 2 次命中追加 80%/100% 攻击，无视普通防御计算 |
| 快剑 | 疾剑式 | 每重直接增加 `0.12` 攻速 |
| 快剑 | 破甲掌 | 每次命中累计破甲 `0.35 × 重数`，最多削减到敌人当前防御为 0 |
| 毒掌 | 毒砂掌 | 每次命中施加等于武学重数的毒层 |
| 毒掌 | 百毒心经 | 每重增加 4 层毒上限，并使每层每秒伤害增加 `0.25` |
| 毒掌 | 吸星诀 | 每重增加 4% 普攻/剑气吸血；毒伤额外按每重 10% 转化为治疗 |
| 铁壁 | 铁布衫 | 每重使最大气血按当时数值增加 15%、防御 +1，并补充新增气血 |
| 铁壁 | 金钟罩 | 开战护盾为 `重数 × (8 + 1.5 × 当前防御)` |
| 铁壁 | 反震诀 | 玩家受到实际气血伤害后，按 `防御 × (0.45 + 0.20 × 重数)` 反伤 |

毒伤每个模拟秒结算一次：

```text
每次毒伤 = 当前毒层 × (0.55 + 0.25 × 百毒心经重数)
毒层上限 = 8 + 4 × 百毒心经重数
```

毒层、破甲、护盾和命中计数均为单场战斗状态，进入下一场时重置。

### 4.6 当前装备效果

每局开始自动获得并装备青钢剑、轻鳞衣、练功护腕；宝箱可获得并按槽位战力评分自动换装其他装备。

| 装备 | 属性/机制 |
| --- | --- |
| 青钢剑 | 攻击 +4；每 3 次命中追加 35% 攻击剑气 |
| 轻鳞衣 | 防御 +2、最大气血 +18；每场获得 10 点护盾 |
| 练功护腕 | 攻速 +0.08；暴击后下一次攻击间隔缩短 30% |
| 玄铁戒 | 攻击 +2、暴击率 +4%；每次命中额外破甲 0.35 |
| 游侠披风 | 最大气血 +12、闪避率 +4%；成功闪避时恢复 3% 最大气血 |
| 毒镖囊 | 攻击 +1；每次命中额外施加 1 层毒 |

### 4.7 胜负、奖励与阶段切换

- 普通战斗失败：立即进入失败结算。
- 洞穴战斗失败：立即进入失败结算。
- Boss 战失败：进入 Boss 失败结算。
- 普通战斗胜利：击杀数 +1，按遭遇对象发修为和铜钱。
- 洞穴战斗胜利：击杀数 +1，修为至少 25、铜钱至少 8。
- 奖励导致升级：进入武学三选一；主时间暂停。
- Boss 战开始：取消其他战斗、恢复玩家满气血、重置 Boss 战计时。
- Boss 胜利：直接结算；当前旧链路没有发放 Boss 奖励表内容。

旧 `PlayerStats.GainCultivation` 每次奖励最多只处理一次升级，即使一次奖励足以跨越多级，也只弹出一次武学选择；新架构的 `ProgressionService` 已支持一次奖励连续升级。

### 4.8 战斗表现与音频

当前战斗画面由 `BattleScreenController` 使用 IMGUI 绘制，显示：

- 双方气血、攻击、防御、攻速和暴击率。
- 普通战斗主时间、洞穴暂停提示或 Boss 独立时间。
- 当前护盾、毒层、破甲、招数和战斗日志。
- 伤害跳字、暴击、闪避、受击闪烁、屏幕震动和角色帧动画。

`BattleFeedbackAudio` 监听 `AttackSequence`，播放挥击、命中、暴击和闪避音效；未绑定音频时会在运行时生成程序化占位音效。

`MainMapMusicController` 会根据普通战斗、洞穴战斗和 Boss 阶段切换音乐层。Boss 低血量时当前只会切换“狂暴”音乐层，不会改变 Boss 攻速或攻击行为。

## 5. 新架构战斗系统：已实现但未完整接入

### 5.1 分层职责

| 层 | 主要文件 | 职责 |
| --- | --- | --- |
| Domain | `CharacterStats`、`CharacterRuntime`、`BattleModels` | 属性、当前气血、战斗效果值对象、随机源接口 |
| Application | `BattleService`、`CombatEffectRegistry`、`CharacterFactory` | 自动攻击、伤害、效果解析、配置转运行时对象 |
| Unity Adapter | `BattleRunner`、`CharacterManager`、`RunManager` | Unity 帧更新、数据库访问、流程和奖励协调 |
| Interaction | `EnemyEncounter`、`CaveEntrance` | 世界碰撞转为开战/进洞穴请求 |
| UI | `BattleView`、`GameUiPresenter` | uGUI 显示和按钮转发 |

Domain/Application 程序集不引用 UnityEngine，可以用固定随机源和手动 Tick 做确定性测试。

### 5.2 新战斗服务的结算顺序

`BattleService` 的攻击间隔为：

```text
攻击间隔 = 1 / max(0.1, 攻速)
```

与旧链路不同，新服务的双方首次攻击都要等待完整攻击间隔。`Tick` 使用 `while` 补算大帧间隔内应发生的多次攻击，结算顺序仍为：

```text
所有到期的玩家攻击
-> 所有到期的毒伤 Tick
-> 所有到期的敌人攻击
```

`BattleRunner` 默认把 `Time.deltaTime × 1.5` 传入服务，因此 `BattleService.Elapsed` 是加速后的模拟时间；`RunTimerService.BossBattleTime` 和主地图时间仍按真实 `Time.deltaTime` 记录。两种“战斗用时”的语义目前不完全一致。

### 5.3 新伤害模型

```text
有效防御 = 防御方防御 - 本场破甲
基础伤害 = max(1, 攻击 × (1 + 伤害加成) - 有效防御)
暴击伤害 = 基础伤害 × 暴击倍率
敌人攻击玩家时先扣护盾
气血伤害 = 剩余伤害 × (1 - 伤害减免)
```

新链路的吸血按目标实际损失的气血与实际剑气伤害计算，不包含过量伤害。`CharacterStats` 已预留伤害加成、伤害减免和恢复属性，但当前 CSV 与运行时流程没有完整使用这些字段。

### 5.4 新旧效果支持对照

| 效果类型 | 旧链路 | 新 `BattleService` | 当前差异 |
| --- | --- | --- | --- |
| `SwordQi` | 已实现 | 已实现 | 新链路从稳定配置 ID 读取 |
| `PoisonOnHit` | 已实现 | 已实现 | 新链路从所有效果取最高毒上限和每层伤害 |
| `ArmorBreakOnHit` | 已实现 | 已实现 | 都是单场累计 |
| `OpeningShield` | 已实现 | 已实现 | 都按固定值 + 防御系数计算 |
| `Retaliation` | 已实现 | 已实现 | 都要求玩家实际受到气血伤害 |
| `LifeSteal` | 普攻、剑气和毒伤回血 | 只实现普攻与剑气回血 | CSV 的毒伤回血次级值尚未使用 |
| `CriticalHaste` | 已实现 | 枚举和配置存在，但服务未处理 | 练功护腕在新链路缺少暴击加速机制 |
| `DodgeHeal` | 已实现 | 枚举和配置存在，但服务未处理 | 游侠披风在新链路缺少闪避回血机制 |
| `StatModifier` | 仅部分武学硬编码 | 已通用化 | 新链路按来源 ID 替换同一武学的旧重数修正 |

### 5.5 数据来源

新链路以 `Assets/GameData/Tables/*.csv` 为唯一源数据：

- `characters.csv`：玩家、普通敌人、精英、洞穴敌人和两个 Boss。
- `martial_arts.csv`：九门武学、重数数组和稳定 `CombatEffectType`。
- `equipment.csv`：装备属性与机制效果。
- `rewards.csv`：修为、铜钱、恢复、装备和武学奖励。
- `spawns.csv`：实体、角色 ID、Prefab ID、奖励 ID、数量和权重。

运行时只读取导入生成的 `GameDatabase.asset`，不直接解析 CSV。

### 5.6 当前场景接入状态

`MainPrototype_Architecture.unity` 中已经存在 `CharacterManager`、`BattleRunner` 和 `RunManager`，但实际序列化状态仍有以下缺口：

- `BattleRunner.characterManager` 未绑定。
- `RunManager.enemySpawner`、`itemSpawner`、`caveSpawner` 未绑定。
- 三个生成器的 `databaseProvider`、`prefabCatalog`、`regions`、`spawnedRoot`、`runManager` 均未绑定。
- 场景中没有绑定完成的 `GameUiPresenter` 和整套 uGUI View。
- 旧 `GameFlowController`、`BattleManager`、`EncounterTrigger` 和 IMGUI 仍存在并启用。

因此复制场景目前仍主要依赖旧链路。新 `RunManager.StartRun` 即使被调用，也不会生成新配置对象；后续进入新战斗时还会因为 `BattleRunner.characterManager` 为空而失败。

完整人工绑定步骤见 `docs/unity_editor_architecture_binding.md`。

## 6. 自动化测试覆盖

当前 `Assets/Tests/EditMode` 共定义 19 项测试，其中战斗相关覆盖如下：

### 6.1 已覆盖

- 玩家攻击按防御减伤并施加类型化毒效果。
- 开场护盾先于玩家气血吸收伤害。
- 手动 Tick 按攻速触发攻击，不依赖 Unity 时间 API。
- 主时间只在主地图和普通战斗中流逝。
- 洞穴和突破状态不消耗主时间。
- Boss 时间与主时间相互独立。
- 显式暂停会停止全部流程计时。
- 属性修正、气血比例保持、角色工厂和数据索引。

### 6.2 尚未自动覆盖

- 旧 `BattleManager` 的实际行为。
- `RunManager + BattleRunner + CharacterManager` 的集成流程。
- 暴击、闪避、吸血、剑气、破甲、毒伤、反震的完整组合。
- `CriticalHaste`、`DodgeHeal` 和毒伤回血。
- 普通战斗中归零后“战斗 -> 连续突破 -> Boss”的完整链路。
- 洞穴战斗结束后 UI 显隐。
- Boss 满血重置、胜负结算和重复开局。

## 7. 当前已知限制与风险

### 7.1 迁移与数据一致性

- 旧场景战斗数值来自各 `EncounterTrigger`，新架构来自 CSV，当前存在双数据源。
- 默认构建仍使用旧场景，所以修改 CSV 不会改变旧场景敌人的实际战斗数值。
- 新架构完成绑定前，不应删除旧组件或把默认构建场景切换到复制场景。

### 7.2 战斗能力缺口

- 战斗始终是单玩家对单敌人的自动普攻，没有多目标、主动技能、真气、技能冷却或目标选择。
- Boss 当前使用普通敌人同一套攻击逻辑，没有重斩、半血狂暴、召唤等 Boss 技能。
- `reward_boss` 已存在于 CSV，但当前新旧流程都没有在 Boss 胜利后发放它。
- 新链路未实现练功护腕的暴击加速、游侠披风的闪避回血和吸星诀的毒伤回血。
- 新链路的 `Recovery`、伤害加成和伤害减免属性已建模，但缺少完整配置和玩法接入。

### 7.3 新 uGUI 当前问题

- `BattleView` 在洞穴战斗时仍显示“主时间继续”，与核心规则不符。
- `BattleRunner` 战斗结束后保留已结束的 `CurrentBattle`；洞穴胜利后状态仍是 `Cave`，现有 Presenter 条件可能让 `BattleView` 与 `CaveView` 同时显示。
- 新 `BattleView` 使用敌人配置 ID 显示名称，没有读取角色中文显示名。
- 新链路只有概览数值，没有旧 IMGUI 已具备的逐招日志、暴击/闪避提示和伤害反馈。

### 7.4 验证风险

- 当前自动测试主要保护纯 C# 服务，不能证明 Unity 场景引用正确。
- 新架构绑定完成后，必须在 Play Mode 重新验证三条核心时间规则。
- 战斗速度倍率改变时，要分别核对战斗模拟时间、主地图真实时间和 Boss 独立时间的显示语义。

## 8. 维护入口

| 修改目标 | 旧链路入口 | 新链路入口 |
| --- | --- | --- |
| 伤害/攻击顺序 | `Assets/Scripts/Battle/BattleManager.cs` | `Assets/Scripts/Application/Combat/BattleService.cs` |
| 武学机制 | `MartialArtCatalog.cs` + `BattleManager.cs` | `martial_arts.csv` + `CombatEffectRegistry.cs` |
| 装备机制 | `PlayerEquipment.cs` + `BattleManager.cs` | `equipment.csv` + `CombatEffectRegistry.cs` |
| 玩家属性 | `PlayerStats.cs` | `CharacterStats.cs`、`CharacterRuntime.cs`、`CharacterManager.cs` |
| 敌人与 Boss 数值 | 场景中的 `EncounterTrigger` / `GameFlowController.bossStats` | `characters.csv` |
| 战斗奖励 | 场景中的遭遇奖励 | `rewards.csv`、`spawns.csv` |
| 时间与阶段 | `GameFlowController.cs` | `RunTimerService.cs`、`RunManager.cs` |
| 战斗 UI | `BattleScreenController.cs` | `BattleView.cs`、`GameUiPresenter.cs` |
| 自动化测试 | 暂无直接测试 | `Assets/Tests/EditMode/Core/BattleServiceTests.cs`、`RunTimerServiceTests.cs` |

## 9. 修改战斗系统后的最低验证清单

1. 普通战斗约持续数秒时，主地图倒计时同步减少。
2. 主时间在普通战斗中归零时，当前战斗不中断；有突破时先完成全部选择，再进入 Boss。
3. 洞穴内停留和洞穴战斗期间，主地图时间保持不变。
4. 离开洞穴后，主地图时间恢复减少。
5. Boss 开始时玩家恢复满气血，主地图时间保持 0，Boss 独立时间增加。
6. 普通、洞穴、Boss 任一战斗失败都进入失败结算。
7. 护盾、破甲、毒层和命中计数在新战斗开始时重置，玩家当前气血在非 Boss 战之间保留。
8. 修改 CSV 后重新导入 `GameDatabase.asset`，并确认运行场景确实使用新架构而不是旧场景内嵌数值。
9. 执行全部 EditMode 测试，并在 Unity Play Mode 完成上述流程验证。
