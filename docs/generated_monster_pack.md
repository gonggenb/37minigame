# 生成怪物包 v01

## 内容

| 怪物 | 视觉 ID | 地图角色 | 攻击动作 |
| --- | --- | --- | --- |
| 墨鬃妖狼 | `ink_wolf` | 西林快速普通敌人 | 8 帧扑咬 |
| 岩甲山魈 | `stone_ape` | 北岭高血高防精英 | 8 帧双拳砸地 |
| 青竹机关傀 | `bamboo_puppet` | 东郊均衡普通敌人 | 8 帧短枪突刺 |

本批资源现已替换主地图和洞穴内全部 TinySwords 战斗单位展示，不增加地图
遭遇总密度，也不增加冲锋、眩晕、弹道或范围伤害等新机制。TinySwords
黑色武者动画仅保留给最终 Boss。

## 展示替换分配

| 新展示 | 当前覆盖遭遇 |
| --- | --- |
| 青竹机关傀 | 山贼喽啰、流寇、南坡恶徒、东郊流寇、紫衣毒客 |
| 墨鬃妖狼 | 青衣快剑、南矿毒刃、墨鬃妖狼 |
| 岩甲山魈 | 黑风刀客、玄衣刀客、边城黑衣客、岩甲山魈、洞穴敌人 |

宝箱、药草等 TinySwords 世界道具不属于怪物展示，本次保留。

## 资源规格

- 每个怪物包含 `Idle` 与 `Attack` 两条动画。
- 每条动画 8 帧，最终图为 `2048x256` 横向 Sprite Sheet。
- 单帧 `256x256` RGBA，统一脚底线 `y=224`。
- Unity：160 PPU、Point、Clamp、无 Mipmap、Uncompressed。
- Sprite Pivot：`(0.5, 0.125)`，对应像素坐标 `(128, 32)`。
- 原图统一朝右；战斗界面右侧敌人通过 `flipHorizontally` 水平翻转，朝向左侧玩家。
- 原始提示词见 `ArtSource/Raw/Monsters/prompts.md`。
- 可重复构建脚本见 `ArtSource/tools/assemble_monster_strips.py`。

## Unity 接入

`PrototypeSceneBuilder` 负责：

1. 首次导入时先建立 Multiple Sprite 导入模式。
2. 按 `256x256` 写入 8 个稳定 SpriteRect。
3. 将三组 Idle 帧用于主地图 SpriteRenderer。
4. 将三组 Idle / Attack 帧注册到 `BattleScreenController`。
5. 检测脚底 Pivot，在地图上将 SpriteVisual 高度偏移设为 0。

## 当前验收

- 六张最终图均解析为 8 个 Sprite。
- 三个地图敌人均使用 8 帧 Idle，脚底局部高度为 0。
- 三个战斗视觉 Profile 均为 8 帧 Idle + 8 帧 Attack。
- Play Mode 已进入岩甲山魈普通战斗并推进攻击序列。
- 普通战斗期间主地图倒计时继续下降。
- 地图上的 TinySwords 单位 SpriteRenderer 数量为 0。
- 洞穴敌人使用岩甲山魈，洞穴期间主地图倒计时保持暂停。
- 最终 Boss 继续使用 TinySwords 黑色武者，Boss 时间独立推进。

状态：`v01 候选，已接入并通过技术验收，等待玩法手感确认`。
