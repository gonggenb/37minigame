# 全特效游戏内复查与修正 · 2026-09-25

状态：Unity Editor Play Mode / InEngineQA。使用 MainPrototype、真实战斗结算与渲染、受控属性与输入夹具；不是自然路线平衡测试，也不替代手机真机验收。

## 本轮修正

1. **暂停不同步**：旧主角动作、外放、绝学、飘字和状态光效按真实时间继续播放。暂停 0.7 秒后剑气消失、角色回到待机；暂停 1.4 秒后绝学结束。改为 Unity 游戏时间，统一暂停、恢复及慢动作；绝学展示冷却和 HUD 触发高亮同步。Boss 伤害时点、冷却及数值未调整。
2. **毒发遮挡**：缩小并降低毒雾、破甲爆点的强度，把持续雾气下移，移除毒发的方形瞄准框，改为脚边气波。保留毒层、毒发数字及紫绿状态识别。
3. **中期 Boss 落点**：震岳斩原爆点悬空；现在按六帧各自的接触点定位到玩家前方地面，朝向玩家。连斩缩小并下移，玄甲护罩按该 Boss 的真实底边脚点放置。
4. **护盾受击过亮**：收小护盾爆点，保留护体轮廓，避免整个人物被金色爆光盖住。
5. **修罗血域大叉号**：以现有 C1 赤金重斩六帧动画替换过长的两条直线，收紧受击气弧；保留施法者血轮、角色动画和绝学节奏。
6. **竖屏毒层越界**：大型 Boss 旁的毒层徽标限制在安全区内，混合构筑的高毒层仍能完整显示。

本轮复用已有正式纹理，没有新增候选原画或占位素材；没有更换整体画风。

[前后对比（左旧右新）](comparison.png)。对比来自实际 Game View，背景和伤害数值因夹具随机状态不同可能有差异；不是同一帧像素差分。

## 覆盖范围

| 类别 | 游戏内检查 | 处理 |
| --- | --- | --- |
| 三款普攻、剑气/掌劲/重斩 | 横竖屏、普通/高攻速、起手/命中/收势、缓存与取消 | 保留素材，统一暂停 |
| 贴身剑弧、掌心聚气、重斩蓄力 | 随当前动作、敌方攻击不覆盖、无累积旧特效 | 保留 |
| 五套绝学 | 八帧角色动作、核心背景、释放层、满重触发、普通攻击不中断 | 修正暂停、冷却与血域释放层 |
| 毒伤、破甲、护盾、反震、治疗、暴击、闪避、血气 | 真实 DoAttack / ApplyPoisonTick 触发及多时点截图 | 收敛毒发与护盾；其余保留 |
| 重撞、毒咬/余毒、机关护甲/碎甲 | 实际特性状态驱动，横竖屏截图 | 保留 |
| 震岳斩、双段连斩、玄甲及碎甲 | 实际 Boss 技能推进与命中状态 | 对齐脚点及落点 |
| 狐火、妖甲及碎甲、狂暴及尾焰 | 三次分时命中、70%/35%阶段、横竖屏 | 保留 |
| 加速仙气拖尾 | 真实移动+临时加速，停步停止发射 | 保留 |
| 混合构筑 | 10 攻速、毒/护盾/反震/绝学叠加、Boss 妖甲、横竖屏 | 修正毒层越界；外放实例上限仍为3 |

## 验证证据

- [主角完整回归](hero_playmode_report.json)：758 项通过，运行时错误为空。包含普通战斗主计时继续、洞穴主计时暂停、最终 Boss 独立计时。
- [专项最终回归](after/playmode_report.json)：42 项通过，findings 与 runtimeErrors 为空。覆盖真实技能触发、暂停4.1秒不消耗绝学展示冷却、恢复、取消、真实高攻速混合构筑、两种方向的移动拖尾。
- 最终编译控制台无错误；随包中文字体校验与 `git diff --check` 通过。
- 横屏 960×540、竖屏 540×960；`before/` 与 `after/` 为这次在编辑器 Play Mode 中截取。
- 主角动作与五套绝学的完整帧证据继续位于 `../hero_attacks/`、`../hero_ultimates/` 和 `../hero_ultimate_poses/`，本轮已重新生成。
- 初次专项报告记录了暂停失败；地图夹具曾因 MobileInputController 清空注入输入而提前结束。隔离真实触摸输入轮询后，真实 PlayerController 移动及拖尾发射通过，不把这个夹具问题记作产品故障。
- 检查完成退出 Play Mode，保留 MainPrototype 编辑场景，不保存临时敌人、属性、输入与 Game View 尺寸。

## 文件

修改运行时代码：

- `Assets/Scripts/UI/BattleScreenController.cs`：通用特效时间、毒雾、护盾与毒层安全区。
- `Assets/Scripts/UI/BattleScreenController.HeroAttacks.cs`、`.HeroSkillVfx.cs`、`.CombatReadability.cs`：动作、外置、贴身及怪物反馈时间。
- `Assets/Scripts/UI/BattleScreenController.Ultimates.cs`：绝学时间与血域斩击。
- `Assets/Scripts/UI/BattleScreenController.MidBoss.cs`：震地、连斩、护甲锚点。
- `Assets/Scripts/Battle/BattleManager.cs`、`.Ultimates.cs`：展示事件时间、展示冷却；不改伤害机制。
- `Assets/Scripts/UI/PrototypeHUDController.cs`：武学触发高亮同步游戏时间。

修改验证与文档：

- `Assets/Scripts/Debug/HeroAttackPlayModeProbe.cs`、`.Ultimates.cs`：采样时钟与正式渲染一致。
- `docs/battle_vfx_catalog.md`：同步当前表现。
- 既有主角验证目录：重新执行后的报告与截图。

新增：

- `Assets/Scripts/Debug/CombatVfxAuditProbe.cs` 与 `.meta`：仅编辑器的可重复专项探针。
- 本目录的说明、报告、对比图和前后截图。

## 运行与复测

无需创建 GameObject、挂脚本、绑定 Inspector 或拖入 Prefab。

1. 打开 `Assets/Scenes/MainPrototype.unity`，正常 Play 即使用修正后的特效。
2. 退出 Play，运行 `37 MiniGame/Validate Hero Attack Pack Play Mode` 做主角完整回归。
3. 退出 Play，运行 `37 MiniGame/Validate All Combat VFX Play Mode` 做全类特效专项回归。两菜单分别运行，探针结束会自行退出 Play。
4. 检查报告 success、error、runtimeErrors，以及横竖屏效果。夹具的高气血与伤害显示仅用于长时间观察，不是正式平衡参数。

本轮未执行 WebGL 重新构建与 iOS/Android 真机触摸、发热和性能验收；仍保持 InEngineQA。
