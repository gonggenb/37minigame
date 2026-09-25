# 主角武学贴身特效

在原有像素技能动作上增加运行时前后两层贴身效果，复用现有角色帧与绘制工具；不重新生成光栅素材。

| 武学触发 | 主角自身效果 |
| --- | --- |
| 剑气诀 / 武器追加剑气 | 前一动作帧形成短残影，绕腰气流与随剑扫出的青白弧线 |
| 无影连环剑 | 两道较淡残影，强调连续快速出手 |
| 毒砂掌 / 装备施毒 | 玉绿色掌心聚气、绕掌环、沿手臂流动的气劲和少量上升光点 |
| 破甲掌 | 同一推掌动作，使用暖金掌劲与环形光点 |
| 惊鸿一式 | 金色脚底蓄势环、向上聚拢气流、随剑下劈的贴身斩光 |
| 修罗血域 / 血战八方触发 | 朱红蓄势与重斩，挥砍后脚边短促扩散 |

显示由 `PlayerAttackVisualSequence` 对应的出招快照驱动。敌方出手和周期毒伤不会覆盖当前颜色或招式。
贴身层与当前动作共用未缩放播放进度：4% 后显现，22% 达到主要强度，65% 后淡出，94% 消失。
动作取消、结束或被下一招替换时不保留旧贴身层；飞往敌人的原有独立特效继续按原规则结束。

三版普通攻击已有图内特效，不再叠加这层武学特效。贴身效果裁剪到战斗舞台，后层在人物后，
前层在人物前、敌人和文字信息前；不创建粒子 GameObject、Texture2D 或材质，不使用随机数。
所有表现均为既有伤害结算后的视觉反馈，不改变伤害时点、攻速、武学概率或战斗时间。

## 文件

新增：
- `Assets/Scripts/UI/BattleScreenController.HeroSelfVfx.cs` 及 `.meta`：贴身特效绘制、配色和生命周期。
- 本文档。

修改：
- `Assets/Scripts/UI/BattleScreenController.HeroAttacks.cs`：记录与清理出招特效快照。
- `Assets/Scripts/UI/BattleScreenController.cs`：在角色前后插入舞台裁剪内的绘制调用。
- `Assets/Scripts/Debug/HeroAttackPlayModeProbe.cs`：贴身特效生命周期、敌方出手/毒伤防覆盖、破甲和连环剑真实触发、横竖屏截图。
- `docs/hero_attack_pack.md`：最新接入说明。
- `docs/validation/hero_attacks/`：更新测试报告与截图。

## 运行和验收

无需新增 GameObject、挂载脚本、拖入资源或绑定 Inspector。打开 `MainPrototype` 进入 Play，
学习对应武学并碰怪观察；青钢剑的每三次命中剑气也会触发自身剑气效果。

完整可重现检查：编辑态执行 `37 MiniGame/Validate Hero Attack Pack Play Mode`。
该检查使用真实 DoAttack 结算和受控战斗夹具，在 960×540 与 540×960 下保存截图，
结束后退出 Play 并恢复 Game View；不保存夹具场景。

报告：[playmode_report.json](../hero_attacks/playmode_report.json)。
截图：`SwordQi_*`、`VenomPalm_*`、`BloodCleave_*` 为出手，`Self_*_charge_*` 为蓄势，
`Self_ArmorBreak_*` 与 `Self_SwiftCombo_*` 为破甲和连环剑。

最终手机性能、触控观感与用户引擎内美术批准需单独验收；本轮不重建 WebGL 包。

## 2026-09-21 本轮验证

- Unity 6000.5.4f1 编译成功；实际 Play Mode **429 项检查通过**，运行错误为零。
- 普攻随机三版与原武学选择回归通过；本次新增自身效果的蓄势、出手、结束、取消、敌方攻击与毒伤防覆盖验证通过。
- 破甲掌、无影连环剑使用实际结算触发，分别显示金色掌劲和双残影；惊鸿、修罗血域在横竖屏分别实测金/红重斩。
- 高速出手仍仅一层当前自身效果；普通战斗继续主时间，洞穴暂停主时间，最终 Boss 独立计时，真实协程检查通过。
- 已目视核对三类技能及破甲/连环剑的实际 Game View 截图：手部效果贴合出掌，重斩贴近出刀侧，未遮住血条、主计时或战报。
- 本轮报告快照：[playmode_report.json](playmode_report.json)。实际横屏局部对比：[self_effects_preview.png](self_effects_preview.png)，从左到右为剑气、毒掌、惊鸿。
- 当前状态为 `InEngineQA`；无新增光栅资产、无手动绑定、未宣称手机性能验收或最终 `Approved`。
