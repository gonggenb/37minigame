# 生成怪物包 v01

## 内容

| 怪物 | 视觉 ID | 地图角色 | 攻击动作 |
| --- | --- | --- | --- |
| 墨鬃妖狼 | `ink_wolf` | 西林快速普通敌人 | 8 帧扑咬 |
| 岩甲山魈 | `stone_ape` | 北岭高血高防精英 | 8 帧双拳砸地 |
| 青竹机关傀 | `bamboo_puppet` | 东郊均衡普通敌人 | 8 帧短枪突刺 |

本批资源现已替换主地图全部 TinySwords 与 CraftPix 小怪展示，并用于洞穴中的
基础守卫展示；不增加地图遭遇总密度，也不增加冲锋、眩晕、弹道或范围伤害等
新机制。旧 `rat`、`rider`、`ballista` 视觉 ID 仅作为玩法数据兼容别名，分别映射
到墨鬃妖狼、青竹机关傀、岩甲山魈，不再直接加载 CraftPix 图片。

## 展示替换分配

| 新展示 | 当前覆盖遭遇 |
| --- | --- |
| 青竹机关傀 | 山贼喽啰、流寇、南坡恶徒、东郊流寇、紫衣毒客 |
| 墨鬃妖狼 | 青衣快剑、南矿毒刃、墨鬃妖狼 |
| 岩甲山魈 | 黑风刀客、玄衣刀客、边城黑衣客、岩甲山魈、洞穴敌人 |

兼容视觉 ID 的风格收敛：

| 旧视觉 ID | 统一展示家族 | 定位依据 |
| --- | --- | --- |
| `rat` | 墨鬃妖狼 | 小体型、高攻速 |
| `rider` | 青竹机关傀 | 中体型、均衡近战 |
| `ballista` | 岩甲山魈 | 重型、高防、慢攻 |

宝箱、药草等 TinySwords 世界道具不属于怪物展示，本次保留。

## 资源规格

- 每个怪物包含 `Idle` 与 `Attack` 两条动画。
- 每条动画 8 帧，最终图为 `2048x256` 横向 Sprite Sheet。
- 单帧 `256x256` RGBA，统一脚底线 `y=224`。
- Unity：160 PPU、Point、Clamp、无 Mipmap、Uncompressed。
- Sprite Pivot：`(0.5, 0.125)`，对应像素坐标 `(128, 32)`。
- 战斗右侧的生成小怪统一使用水平翻转。兼容 ID（`blue`、`rat`、`rider`、
  `ballista`）必须与其实际复用的妖狼、机关傀、山魈素材保持相同翻转值，不能按旧素材设置。
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
- 主地图 30 个普通/精英遭遇均使用三套生成怪物的 8 帧 Idle，脚底局部高度为 0。
- 三个正式视觉 ID 与三个兼容视觉 ID 均使用 8 帧 Idle + 8 帧 Attack。
- Play Mode 已进入岩甲山魈普通战斗并推进攻击序列。
- 普通战斗期间主地图倒计时继续下降。
- 地图上的 TinySwords 与 CraftPix 小怪 SpriteRenderer 数量为 0。
- 洞穴敌人使用岩甲山魈，洞穴期间主地图倒计时保持暂停。
- 最终 Boss 保持独立 Boss 资产，不受本次小怪映射影响，Boss 时间独立推进。

状态：`v01 候选，已接入并通过技术验收，等待玩法手感确认`。

## 墨鬃妖狼帧修复

- 狼的原始扑击条带中，部分姿势跨过了等宽分格边界。现在先使用
  `Tools/ArtPipeline/repack_generated_sprite_strip.py` 按完整角色连通区域重排，
  再交给 `ArtSource/tools/assemble_monster_strips.py` 统一缩放和切片。
- 重排时只保留当前角色的连通像素，避免相邻帧的狼头、爪子或杂点落入本帧。
- Idle 使用 `1,2,3,2,1,8,5,8` 的往返顺序，去掉动作跨度过大的抬爪和甩尾跳变。
- Idle 与 Attack 仍保持 8 帧、单帧 256 × 256、脚底线 `y=224` 和 Pivot
  `(0.5, 0.125)`。
