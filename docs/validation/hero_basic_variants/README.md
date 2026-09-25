# 三种随机普通攻击

2026-09-21：用户选定青白剑罡、赤金烈斩、墨影残像三版，接入普通攻击随机播放。

## 行为

- 每次新的普通攻击事件等概率选择三套动画之一，允许相邻两次相同；出手后锁定整套八帧。
- Update 与 OnGUI 重复观察同次攻击不会重新抽取；高攻速仍按原有规则压缩时长或替换旧动作，不积压播放。
- 使用独立 `System.Random`，不消耗 Unity 的玩法随机状态，不改变伤害、暴击、掉落和计时。
- 保留原有武学动作优先级；例如起始青钢剑每三次命中追加剑气，仍切换到剑气专属动作。
- 普攻的特效已包含在角色图集中，不入队独立武学特效；动作结束回到原有待机，取消战斗清空选择。

## 资源与处理

原始候选与提示词：`ArtSource/Previews/HeroAttacks/2026-09-21_basic_vfx_v01/`。

新增三张 `2048 × 256` 透明 PNG 及 Unity 切片元数据，位于 `Assets/Resources/Characters/HeroAttacks/`：

- `spr_hero_attack_basic_jade_crescent_right_8f_v01.png`
- `spr_hero_attack_basic_golden_ember_right_8f_v01.png`
- `spr_hero_attack_basic_ink_afterimage_right_8f_v01.png`

原图由内置 image_gen 生成。本次使用 `Tools/ArtPipeline/prepare_hero_basic_variants.cjs` 做技术归一化：
按审核过的非均匀边界切片、清除 alpha ≤ 25 的近透明生成噪点、每条统一缩放、脚点固定到 y=223。
赤金上方火星与相邻帧靴子使用分段切分边界，避免带入邻帧。入口与收势使用原有普攻种子帧。

青白/赤金标准人物高 112 px，墨影高 94 px，为较宽特效留出画布；显示时以脚底为中心补偿到
原有 136 px 人物比例。阴影和战斗布局不跟随补偿缩放。每帧 256×256、160 PPU、Point、FullRect、
Pivot (0.5,0.125)、无 Mipmap；默认平台无压缩，WebGL 沿用当前项目的移动纹理策略。

归一化数据见 [normalization.json](normalization.json)，原始像素接触表见 [contact_sheet.png](contact_sheet.png)，
以运行时显示比例制作的并排动图见 [display_scale_preview.gif](display_scale_preview.gif)。
`attacks_8fps.gif` / `attacks_12fps.gif` 是未补偿显示比例的技术预览；原始四倍图见 `impact_4x.png`。

## 修改文件

- `Assets/Scripts/Visual/HeroAttackArt.cs`：三版缓存、释放与显示比例。
- `Assets/Scripts/UI/BattleScreenController.HeroAttacks.cs`：每次普攻随机选择、整次锁定、结束状态重置。
- `Assets/Scripts/UI/BattleScreenController.cs`：仅缩放角色绘图区域并保持脚点。
- `Assets/Editor/HeroAttackArtImporter.cs`：导入统计支持新增图集。
- `Assets/Scripts/Debug/HeroAttackPlayModeProbe.cs`：增加三版导入、随机出招、独立随机状态、同次锁定及横竖屏检查。
- `docs/hero_attack_pack.md`：接入说明；原有验证目录更新本轮报告与截图。

## 运行与重现

无需创建 GameObject、挂脚本或绑定 Inspector。打开 `MainPrototype` 进入 Play，碰怪后观察普攻。
保留起始武器时约每三次命中会穿插一次剑气专属动作，属原有装备机制。

需要重制图集时，设置 `NODE_PATH` 为安装 sharp 的模块目录并运行：

```sh
node Tools/ArtPipeline/prepare_hero_basic_variants.cjs
```

然后执行 `37 MiniGame/Art/Reimport Hero Attack Pack`。
在 `MainPrototype` 编辑态执行 `37 MiniGame/Validate Hero Attack Pack Play Mode` 重现集成测试。
测试以临时战斗夹具执行真实 DoAttack；纯普攻抽样暂时卸下起始剑，技能检查重新初始化装备。
不保存测试场景；结束后退出 Play 并恢复 Game View。

验证报告：[`../hero_attacks/playmode_report.json`](../hero_attacks/playmode_report.json)。
专项截图：`../hero_attacks/BasicVariant_0/1/2_landscape/portrait.png`。
WebGL 发布包未重建；手机真机观感与用户最终引擎内美术验收仍需单独完成。

## 本轮实测结果

- Unity 6000.5.4f1 编译通过；Play Mode 380 项检查通过，运行错误为零。
- 240 次真实 DoAttack 纯普攻抽样：青白 97 次、赤金 67 次、墨影 76 次。等概率来自 `Random.Next(3)`，
  样本计数用于证明三套实际触发，不要求每批次数相等。
- 逐次检测动画选择前后的 Unity Random.state，未被动画抽样改变。
- 同一次出手重复调用 TrackHeroAttack 与取帧，所选图集不变；专属武学、取消战斗、非循环结束和高攻速检查通过。
- 实际 Game View 960×540 / 540×960，各三张峰值截图已目视核对；三种特效完整，无图集裁边，未遮住血条和主计时。
- 真实战斗协程验证：普通战斗继续主时间；洞穴战斗主时间不变；最终 Boss 独立计时。
- 当前状态 `Generated → Normalized → Imported → InEngineQA`；候选风格已由用户选定，最终引擎内/手机验收未标 `Approved`。
