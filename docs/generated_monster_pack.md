# 生成怪物包 v01 + v02

## 第二关小怪扩展（2026-09-09）

第二关平川山镇的 40 个普通遭遇采用新老混合：22 个点位保留原有小怪，18 个点位加入新怪。
铁獠山猪、赤练蛇妖、斗笠刀匪、酒葫芦恶僧、灯笼怨灵、菌甲小妖各 3 处。
入口与外围加入山猪、菌妖，镇区与营地加入刀匪、恶僧，洞区和北关加入怨灵、蛇妖；各区同时保留老怪。
老怪从原遭遇模板恢复名称、地图待机与战斗视觉配置。10 个精英及中期/最终 Boss 保留原配置。

每种包含 8 帧待机与 8 帧攻击，单帧 256×256，条带 2048×256，160 PPU，
Point、Full Rect、无压缩/无 Mipmap、脚底 Pivot `(0.5,0.125)`。战斗中朝向左侧玩家。
蛇妖、怨灵和菌妖的待机条带由两个原始中立姿势按 8 格循环排列，保留已认可造型；
不是八张独立呼吸绘制。部分重生成候选有造型漂移或伪透明背景，未用于交付。
小怪仍复用普通自动攻击动作。后续玩法优化已为第二关普通山猪加入首击重撞、蛇妖加入短时毒伤、老机关类加入开场护甲；
详见 `docs/validation/route_combat_review/README.md`。不增加位移、弹道或范围伤害。
点位、原有战斗数值、修为、铜钱与三条计时规则保持不变。

资源位于 `Assets/Art/Generated/Characters/Enemies/LevelTwoPack/`；
源图及提示词位于 `ArtSource/Raw/Monsters/LevelTwoPack/`；
归一化工具为 `Tools/ArtPipeline/prepare_level2_monster_pack.py`。

在 MainPrototype 中运行 `37 MiniGame/Apply Level 2 Monster Pack` 可重复导入和绑定；
平川山镇重建工具也会自动应用同一分布。该菜单拒绝在其他关卡或 Play Mode 中应用。
此批不修改关卡1/关卡3，也不替换共用遭遇模板。

验证入口：`37 MiniGame/Validate Level 2 Monster Pack Play Mode`。
当前实测状态及截图以 `docs/validation/level2_monster_pack/README.md` 为准。

## 内容

| 怪物 | 视觉 ID | 地图角色 | 攻击动作 |
| --- | --- | --- | --- |
| 墨鬃妖狼 | `ink_wolf` | 西林快速普通敌人 | 8 帧扑咬 |
| 岩甲山魈 | `stone_ape` | 北岭高血高防精英 | 8 帧双拳砸地 |
| 青竹机关傀 | `bamboo_puppet` | 东郊均衡普通敌人 | 8 帧短枪突刺 |
| 青芦刀螳 | `reed_mantis` | 西道/北坡高速普通敌人 | 8 帧双刃斩击 |
| 铜甲石蟾 | `bronze_toad` | 北岭/东村重甲普通敌人 | 8 帧重压冲撞 |
| 赤砂毒蝎 | `crimson_scorpion` | 南坡/矿道毒击普通敌人 | 8 帧钳击与尾刺 |

v01 资源已替换主地图全部 TinySwords 与 CraftPix 小怪展示，并用于洞穴中的
基础守卫展示；v02 新增三套怪物并在内圈/中圈补充 6 个普通遭遇。它们不增加冲锋、眩晕、弹道或范围伤害等
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
3. 将六组 Idle 帧用于主地图 SpriteRenderer。
4. 将六组 Idle / Attack 帧注册到 `BattleScreenController`。
5. 检测脚底 Pivot，在地图上将 SpriteVisual 高度偏移设为 0。

## 当前验收

- 十二张最终图均解析为 8 个 Sprite。
- 主地图 36 个普通/精英遭遇均使用六套生成怪物的 8 帧 Idle，脚底局部高度为 0。
- 六个正式视觉 ID 与三个兼容视觉 ID 均使用 8 帧 Idle + 8 帧 Attack。
- Play Mode 已进入岩甲山魈普通战斗并推进攻击序列。
- 普通战斗期间主地图倒计时继续下降。
- 地图上的 TinySwords 与 CraftPix 小怪 SpriteRenderer 数量为 0。
- 洞穴敌人使用岩甲山魈，洞穴期间主地图倒计时保持暂停。
- 最终 Boss 保持独立 Boss 资产，不受本次小怪映射影响，Boss 时间独立推进。

状态：`v01 候选保持；v02 已接入并通过静态技术验收，等待 Play Mode 动画与玩法手感确认`。

## 墨鬃妖狼帧修复

- 狼的原始扑击条带中，部分姿势跨过了等宽分格边界。现在先使用
  `Tools/ArtPipeline/repack_generated_sprite_strip.py` 按完整角色连通区域重排，
  再交给 `ArtSource/tools/assemble_monster_strips.py` 统一缩放和切片。
- 重排时只保留当前角色的连通像素，避免相邻帧的狼头、爪子或杂点落入本帧。
- Idle 使用 `1,2,3,2,1,8,5,8` 的往返顺序，去掉动作跨度过大的抬爪和甩尾跳变。
- Idle 与 Attack 仍保持 8 帧、单帧 256 × 256、脚底线 `y=224` 和 Pivot
  `(0.5, 0.125)`。
