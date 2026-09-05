# 第二关第 30 秒 Boss 技能组接入

日期：2026-09-05。目标：把已生成的玄甲镇关使技能预览接入实际第二关中期战斗。

## 实现

| 技能 | 实际结算 | 动作与特效 |
| --- | --- | --- |
| 镇关·震岳斩 | 130% 攻击，动作 0.50 秒命中 | 保留原有 8 帧动作、6 帧刀气；总动作 0.80 秒 |
| 横刀连破 | 0.35 / 0.80 秒各 65% 攻击，分别计算防御、闪避、护盾与反震 | 新增 8 帧动作、两段各 3 帧刀光；总动作 1.20 秒 |
| 玄甲固守 | 首次半血触发一次，当前技能结束后发动；最大气血 8% 护甲，3 秒到期或提前被击破 | 新增 8 帧架守动作、6 帧结甲/维持/破裂特效；总动作 0.80 秒 |

- 气血 260 → 290，攻击 12 → 13；防御 3、攻速 0.78 保持原值。以上为内部数值，界面沿用项目既有显示倍率。
- 进攻技能按震岳斩 → 横刀连破交替，共享 6 个战斗秒冷却；开场延迟 4.5 个战斗秒。冷却受原有战斗倍率影响，动作与护甲寿命不受该倍率影响，但随游戏暂停冻结。
- 玩家在 Boss 技能动作中继续自动攻击。护甲不回血、不免疫；所有通向 `ApplyDamageToCurrentEnemy` 的伤害先扣护甲。
- 技能起手与伤害事件分离；两段命中分别通知伤害/音效路径，动作及特效不会被玩家攻击事件覆盖。
- 死亡后取消后续命中；取消、重新开战时清空技能、护甲、特效状态。

## 场景与素材

已在 Unity Editor 中执行 `37 MiniGame/Refresh Xuanjia Mid Boss Presentation` 并保存 `Assets/Scenes/MainPrototype.unity`。
实读场景确认左朝向、两套新动作各 8 帧、两套新特效各 6 帧、290 气血、13 攻击及第 30 秒触发。
无需手动创建 GameObject、挂脚本或拖 Inspector 引用。新增 partial 文件扩展原有组件，不作为额外挂件。

角色图集均为 2048×256、每格 256×256、PPU 160、脚底 pivot (0.5, 0.125)；特效为 1536×256、6 格。
首尾姿态复用既有待机种子帧，中间帧来自本次预览素材。预览目录保留原始生成图与处理记录。
材质和字体沿用项目现有风格。

## 实际验证

- Unity 6000.5.4f1，实际 `MainPrototype` Play Mode；固定种子 90530、受控角色属性，23 项集成检查全部通过。
- [机器结果](midboss_skill_pack_2026-09-05.json)：自动第 30 秒触发、入场不回血、攻击轮转、半血排队、护甲吸收/到期/提前击破、暂停冻结、取消重置、两段独立防御/护盾/闪避、首击致死取消次击、胜负结算及返回地图。
- 测试中两段理论伤害合计为 10.9；角色默认装备提供 10 点开场盾，因此实际掉血约 0.9，验证统计同时计入盾的消耗。
- 普通战斗跨过第 30 秒继续结算原敌人，然后进入中期 Boss；中期胜利后恢复地图探索。
- 三条核心时间规则均实测通过：普通战斗耗主时间、洞穴战斗暂停主时间、最终 Boss 使用独立计时；中期 Boss 仍暂停主时间并单独记录交锋时长。
- Unity 脚本编译无错误。菜单 `37 MiniGame/Validate Chinese Fonts` 通过，常规/粗体覆盖 841 个非 ASCII 字形。
- 540×960 与 960×540 游戏截图已检查角色朝向、动作可见性、刀光与护甲显示。横屏护甲状态改为整行自适应，避免与攻防数值重叠。
- 验证后退出 Play Mode，恢复 Portrait 540×960，主场景无未保存更改。

实际游戏截图（受控测试角色数值）：

![横刀连破横屏](midboss_skill_pack_images/double_cleave_landscape.png)
![玄甲固守横屏](midboss_skill_pack_images/iron_guard_landscape.png)

竖屏截图：[横刀连破](midboss_skill_pack_images/double_cleave.png)、[玄甲固守](midboss_skill_pack_images/iron_guard.png)。

## 运行与复测

1. 打开 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode，开始第二关；探索到主地图经过第 30 秒时自动进入中期 Boss。
2. 保持战斗时间足够长，可看到两种攻击交替；将 Boss 打至半血，确认架守、护甲吸收、破裂或三秒消失；击败后继续探索。
3. 自动复测：在编辑状态执行 `37 MiniGame/Validate Mid Boss Skill Pack`，自动进入 Play Mode、覆盖结果 JSON/截图并退出。该工具仅编译到编辑器，不会进入玩家构建；请在希望测试的 Game View 横竖屏尺寸下运行。
4. 如需重新绑定素材，只运行 `37 MiniGame/Refresh Xuanjia Mid Boss Presentation`，不必重建整个场景。

## 文件清单

修改：

- `Assets/Scripts/Battle/BattleManager.cs`：接入中期技能更新和统一护甲吸收。
- `Assets/Scripts/Battle/BossV2Definitions.cs`：技能枚举与集中数值。
- `Assets/Scripts/Battle/BattleVfxCue.cs`：双斩反馈标记。
- `Assets/Scripts/GameFlow/GameFlowController.cs`：默认中期 Boss 数值。
- `Assets/Scripts/Runtime/GameTextCatalog.cs`：技能和护甲名称。
- `Assets/Scripts/UI/BattleScreenController.cs`：帧资源、技能状态和战斗反馈入口。
- `Assets/Editor/PrototypeSceneBuilder.cs`：切帧、绑定及定向刷新。
- `Assets/Scenes/MainPrototype.unity`：保存数值和资源引用。
- `docs/gameplay_systems.md`：当前技能规格。
- `ArtSource/Previews/Characters/Bosses/XuanjiaGateWarden/SkillsV2_20260905/design.md`：链接接入状态。

新增（Assets 文件均含 Unity `.meta`）：

- `Assets/Scripts/Battle/BattleManager.MidBoss.cs`：调度、半血护甲与独立命中。
- `Assets/Scripts/UI/BattleScreenController.MidBoss.cs`：动作帧时间及刀光/护甲播放。
- `Assets/Scripts/Debug/MidBossSkillPlayModeProbe.cs`：可重复运行的集成探针。
- `Assets/Art/Generated/Characters/Bosses/XuanjiaGateWarden/spr_boss_xuanjia_gate_warden_double_cleave_left_8f_v01.png`
- `Assets/Art/Generated/Characters/Bosses/XuanjiaGateWarden/spr_boss_xuanjia_gate_warden_iron_guard_left_8f_v01.png`
- `Assets/Art/Generated/Effects/XuanjiaGateWarden/spr_vfx_midboss_double_cleave_6f_v01.png`
- `Assets/Art/Generated/Effects/XuanjiaGateWarden/spr_vfx_midboss_iron_guard_6f_v01.png`
- 本验证说明、结果 JSON、`midboss_skill_pack_images/` 四张游戏截图。

## 未完成的验收

当前达到实际场景集成与受控 Play Mode 验证。尚未完成自然路线下多种构筑的胜率/出场血量平衡统计，不能据此宣称第二关难度已最终调平。
真机性能/触摸、完整动作连续观感和音效听感仍待实玩；生成动作的刀柄、衣摆细节仍可继续精修，不标记最终美术 Approved。
