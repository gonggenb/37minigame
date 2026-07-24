# 武侠美术生产流水线

## 1. 文档目的

本文件是项目美术资源的生产与验收入口，统一角色帧动画、武学图标、道具图标、环境资源的规格和状态定义。

美术总方向以 [2.5D 武侠绘本美术规范](art_style_guide.md) 为准：

- 角色：清晰、克制的像素武侠动画。
- 图标：剪影明确的手绘武侠图形，使用宣纸、墨线、朱红、暗金和玉色作为视觉语言。
- 场景：低多边形、哑光手绘材质。
- 玩法信息永远比装饰细节更醒目。

## 2. 一套尺寸规则

### 2.1 角色帧动画

| 项目 | 统一规格 |
| --- | --- |
| 单帧画布 | 256 × 256 px |
| 图集排列 | 单行横向，宽度 = 帧数 × 256，高度 = 256 |
| 标准 8 帧图集 | 2048 × 256 px |
| 透明背景 | 必须 |
| 基准朝向 | Right；Left 在 Unity 中使用 `flipX` |
| 脚底基准线 | 距画布底部 32 px |
| Unity Pivot | `(0.5, 0.125)`，即脚底中心 |
| Pixels Per Unit | 160 |
| Sprite Mesh Type | Full Rect |
| Filter Mode | Point |
| Mip Maps | Off |
| Compression | None |
| Wrap Mode | Clamp |

所有帧使用同一个角色缩放比例和脚底锚点。角色大小差异通过 Unity 世界缩放表达，不通过改变像素密度表达。

### 2.2 武学与道具图标

| 阶段 | 尺寸 | 用途 |
| --- | --- | --- |
| AI 生成母版 | 256 × 256 px | 保留轮廓和绘制余量 |
| Unity 交付图标 | 128 × 128 px | 项目内正式 PNG |
| 武学选择卡显示 | 64 × 64 px | 三选一、详情卡 |
| HUD / 快捷栏显示 | 48 × 48 px | 战斗与主地图 HUD |
| 极小列表显示 | 32 × 32 px | 仅允许简单图标使用 |

图标主体必须位于中央 192 × 192 px 安全区，四边至少留 32 px 透明边距。不得把名称、等级、稀有度文字烘焙进图标。

Unity 图标导入设置：

- Texture Type：Sprite (2D and UI)。
- Sprite Mode：Single。
- Mesh Type：Full Rect。
- Filter Mode：Bilinear。
- Mip Maps：Off。
- Compression：None。
- Wrap Mode：Clamp。
- Alpha Is Transparency：On。
- Max Size：128。

### 2.3 头像与特殊图

| 类型 | 交付尺寸 |
| --- | --- |
| 角色 / Boss 头像 | 256 × 256 px |
| 装备大图 | 256 × 256 px |
| UI 九宫格边框 | 64 × 64 px 或 128 × 128 px |
| 地表无缝贴图 | 1024 × 1024 px |

## 3. 图标视觉语言

### 3.1 武学图标

图标只画“动作或力量来源”，稀有度和等级由独立 UI 边框表达。

| 武学类型 | 主色建议 | 形状提示 |
| --- | --- | --- |
| 外功 | 朱红、铁灰 | 刀剑、掌印、破风斜线 |
| 内功 | 玉绿、暖白 | 气旋、经脉、护体圆环 |
| 身法 | 青蓝、银灰 | 残影、足迹、流云 |
| 心法 | 紫墨、米白 | 经卷、印记、阴阳结构 |
| 绝学 | 暗金、朱红 | 放射形、独特印章轮廓 |

颜色不能是唯一分类依据；每类必须同时具有不同的轮廓或角标。

### 3.2 道具图标

| 道具类型 | 识别重点 |
| --- | --- |
| 消耗品 | 单个瓶、丹药、药草，轮廓简单 |
| 装备 | 完整武器或护具，不画人物持握 |
| 材料 | 2–3 个成组的小物件 |
| 秘籍 | 经卷、竹简或书册，保留明显封面结构 |
| 货币 | 铜钱或元宝，不与普通材料共用轮廓 |

稀有度统一由外部边框表示：

- 普通：灰白。
- 精良：玉绿。
- 稀有：青蓝。
- 绝品：紫墨。
- 传说：暗金。

## 4. 帧动画规格

### 4.1 MVP 动作表

| 动作 | 帧数 | FPS | 循环 | 节奏要求 |
| --- | ---: | ---: | --- | --- |
| Idle | 8 | 8 | 是 | 呼吸、衣摆轻动，不改变脚底位置 |
| Run | 8 | 12 | 是 | 两次清晰踏步，身体起伏不超过 4 px |
| Attack | 8 | 12 | 否 | 2 帧蓄力、3 帧出招、1 帧命中、2 帧收势 |
| Hurt | 4 | 12 | 否 | 快速后仰，最后回到可衔接姿态 |
| Death | 8 | 10 | 否 | 清楚倒地，最后一帧保持 |

当前生产优先级为 `Idle → Run → Attack`。`Hurt` 和 `Death` 在战斗反馈需要时再补，不阻塞核心闭环。

### 4.2 方向规则

- 主地图和战斗界面的第一版只生产 Right 朝向。
- Left 由 `SpriteRenderer.flipX` 获得。
- 不单独生成 Left，避免服装、武器和脸部细节漂移。
- 只有俯视移动确实需要时才增加 Down / Up；方向资源必须整组补齐，不能只补单个动作。

## 5. 角色动画生产流程

### 5.1 先批准种子帧

每个角色必须先有一张经过游戏内尺寸验证的种子帧。它负责锁定：

- 身材比例和头身比。
- 发型、脸部、服装、武器。
- 调色板。
- 朝向与脚底位置。
- 像素块密度和描边粗细。

没有批准种子帧，不进入批量动画生成。

### 5.2 整条生成，不逐帧生成

一次生成完整横向条带。禁止默认逐帧独立生成，因为独立生成容易出现身高、武器、衣服、颜色和脸部漂移。

标准生成提示词模板：

```text
Use case: production Unity sprite sheet for a 2.5D wuxia roguelite.

Edit the approved transparent seed-frame reference into one horizontal <N>-frame animation strip.
Canvas size: exactly <N*256> × 256 px.
Each slot: exactly 256 × 256 px.

Character invariants:
- exactly the same character and right-facing direction
- same face, hairstyle, outfit, weapon and body proportions
- same restrained wuxia palette and crisp pixel clusters
- same pixel density and silhouette family
- feet aligned to the shared ground line 32 px above the canvas bottom

Animation:
- <describe frame-by-frame action beats>

Output constraints:
- transparent background
- exactly one character in each slot
- no scenery, shadows, labels, borders or poster composition
- no cropping across slot boundaries
- production sprite asset, not concept art
```

### 5.3 统一归一化

生成结果必须经过同一批归一化：

1. 按 256 px 等宽切分。
2. 检测每格非透明像素范围。
3. 使用整条动画的联合边界计算一次缩放，不能逐帧单独缩放。
4. 所有帧对齐到 `(128, 32)` 脚底锚点。
5. 必要时把第一帧锁回已批准的种子帧。
6. 检查越界像素、透明杂点和半透明脏边。
7. 输出条带和逐帧预览图。

### 5.4 动画预览

资源进入 Unity 前必须至少检查：

- 1× 原始像素预览。
- 4× 最近邻放大预览。
- 8 FPS / 12 FPS 动图预览。
- 游戏实际摄像机距离下的 Play Mode 预览。

## 6. 图标生产流程

1. 从武学或道具配置确定 `asset_id`、类型、核心识别物和主色。
2. 先生成 256 × 256 透明母版，每批使用同一模板和同一视觉参考。
3. 删除背景、文字、边框、光斑和无意义装饰。
4. 将主体统一到 192 × 192 安全区。
5. 缩小到 128 × 128，检查 64、48、32 px 显示效果。
6. 单独叠加 Unity UI 中的分类角标、稀有度框和冷却遮罩。
7. 通过灰度和色弱检查，确认不依赖颜色才能识别。

图标提示词模板：

```text
Use case: production Unity UI icon for a wuxia roguelite.
Create one centered <skill/item description> icon on a transparent 256 × 256 canvas.
Style: stylized hand-painted wuxia picture-book icon, clear ink-cut silhouette,
restrained palette, subtle rice-paper brush texture inside the subject only.
Keep the subject inside a centered 192 × 192 safe area.
No text, frame, rarity border, scenery, character portrait, watermark or cast shadow.
The icon must remain readable at 48 × 48 px.
```

## 7. 文件和命名规范

### 7.1 目标目录

```text
ArtSource/
  References/
  Raw/
  Normalized/
  Previews/

Assets/Art/Generated/
  Characters/
    Player/
    Enemies/
    Bosses/
  Icons/
    Skills/
    Items/
    Equipment/
  Environment/
```

`ArtSource` 保存种子帧、生成原图和预览，不由 Unity 导入；只有通过归一化的交付文件进入 `Assets/Art/Generated`。

### 7.2 文件名

```text
spr_<角色>_<动作>_<方向>_<帧数>f_<版本>.png
ico_skill_<武学ID>_<版本>_128.png
ico_item_<道具ID>_<版本>_128.png
ico_equipment_<装备ID>_<版本>_128.png
tex_env_<区域>_<材质>_<用途>_<尺寸>.png
```

示例：

```text
spr_player_idle_right_8f_v01.png
spr_boss_bandit_attack_right_8f_v02.png
ico_skill_jianqi_v01_128.png
ico_item_healing_pill_v01_128.png
tex_env_mainmap_grass_albedo_1024.png
```

Unity 子 Sprite 使用：

```text
spr_player_idle_right_f00
spr_player_idle_right_f01
...
spr_player_idle_right_f07
```

ID 使用稳定英文小写蛇形命名；显示名称“剑气诀”“回春丹”等只存在于配置和本地化文本中。

## 8. 生产清单与状态

每项资源至少记录：

| 字段 | 示例 |
| --- | --- |
| `asset_id` | `skill_jianqi` |
| `asset_type` | `skill_icon` / `character_strip` |
| `owner_id` | `player` |
| `action` | `idle` |
| `direction` | `right` |
| `frame_count` | `8` |
| `frame_size` | `256` |
| `fps` | `8` |
| `loop` | `true` |
| `seed_path` | 已批准种子帧路径 |
| `prompt_version` | `v01` |
| `status` | 见下表 |

统一状态：

```text
Briefed -> SeedApproved -> Generated -> Normalized -> Imported -> InEngineQA -> Approved
```

只有 `Approved` 资源可以称为“正式资源”。“已生成”“已导入”和“已在 Play Mode 验证”必须分别记录。

当前首批图标：

| 批次 | 数量 | 目录 | 状态 |
| --- | ---: | --- | --- |
| 核心武学 | 6 | `Assets/Art/Generated/Icons/Skills/` | `InEngineQA` |
| 局内装备 | 5 | `Assets/Art/Generated/Icons/Equipment/` | `InEngineQA` |

- 生成母版保留在 `ArtSource/Raw/Icons/`。
- 图标切分与安全区归一化使用 `Tools/ArtPipeline/normalize_icon_sheet.py`。
- 武学三选一卡显示 `64 px` 级图标；悬停时显示类型、当前实际属性效果和说明。
- 装备背包与穿戴栏按装备稳定 ID 查找图标，稀有度颜色仍由 UI 独立绘制。

## 9. Unity 验收门槛

### 9.1 角色动画

- 每格严格为 256 × 256。
- 帧数、顺序、命名、FPS 和循环设置正确。
- 脚底不沉入地面，动画切换时脚底跳动不超过 2 px。
- 身高、头身比、武器长度和服装颜色无明显漂移。
- 左右翻转不会让关键动作失去可读性。
- 攻击帧在战斗尺寸下能看清蓄力、命中和收势。
- SpriteRenderer 不发生非等比拉伸。

### 9.2 图标

- 透明背景无白边或黑边。
- 64 px 和 48 px 下可以立即识别。
- 32 px 下至少保留类型轮廓。
- 没有烘焙文字、等级、稀有度框和冷却效果。
- 同批图标的主体面积、墨线重量和明暗对比接近。
- 颜色改变后仍能通过轮廓区分类别。

### 9.3 场景

- 保持低多边形、哑光和低反射。
- 地表使用无缝贴图和统一世界纹理尺度。
- 场景对比度低于角色、敌人、奖励和 UI。
- 不引入写实照片材质或高亮 PBR 资源。

## 10. 批量生产顺序

第一批正式化按以下顺序推进：

1. 锁定主角种子帧和 `Idle / Run / Attack` 三条动画。
2. 锁定一个普通敌人和一个 Boss，验证同一流水线能否复用。
3. 生产首批 6 个核心武学图标。
4. 生产药品、铜钱、修为、秘籍、武器五类基准道具图标。
5. 在三选一、HUD、战斗界面和结算界面做实际尺寸测试。
6. 通过后再批量扩充敌人、武学和道具，避免先生成大量无法统一的资源。

## 11. 当前实施边界

- 本文件确定的是后续正式资源规范，不自动重切现有第三方素材。
- 当前已有部分角色条带仍使用旧 Pivot；迁移时必须逐项进行 Play Mode 脚底检查。
- 当前 `SpriteFrameAnimator` 只覆盖 Idle / Move 循环；Attack、Hurt、Death 的非循环播放控制仍需后续实现。
- 本次不改变玩法、战斗、计时和场景绑定。

## 12. 战斗打击反馈资源

### 12.1 命中特效

- 单帧固定为 `256 × 256`，横向排列，透明背景。
- 基础命中使用 `6` 帧，按 `20–24 FPS` 非循环播放。
- 所有帧保持同一中心点，主体不得贴边，颜色以米白、淡金、朱红小面积点缀为主。
- 命名采用 `spr_vfx_<用途>_<帧数>f_v<版本>.png`。
- 当前基准资源：`Assets/Art/Generated/Effects/spr_vfx_wuxia_impact_6f_v01.png`。
- 原始生成图保留在 `ArtSource/Raw/VFX/`，去背景、归一化和引擎导入分开记录。
- 透明图归一化脚本：`Tools/ArtPipeline/normalize_vfx_strip.py`，同一条特效统一缩放并保持中心锚点。

### 12.2 战斗音效

- 短音效统一使用 `WAV / PCM 16-bit / mono / 44.1 kHz`。
- 每次普通攻击由“挥砍 + 轻命中”两层组成；暴击将轻命中替换为重命中；闪避使用独立空气掠过音。
- 峰值不超过 `-1 dBFS`，单条建议控制在 `0.1–0.35 秒`，避免长混响遮挡连续攻击。
- 命名采用 `sfx_combat_<动作>_v<版本>.wav`。
- 可重复生成脚本：`Tools/ArtPipeline/generate_combat_sfx.py`。
- Unity 中优先播放已绑定 WAV；资源缺失时由 `BattleFeedbackAudio` 生成短促程序化音色作为回退。

### 12.3 Unity 验收

- 命中特效必须随伤害事件触发，不能只跟攻击动作计时。
- 普通命中、暴击、玩家受击和闪避必须能听出差异。
- 屏幕震动、红边、伤害字、特效和音效使用未缩放时间，暂停或慢速状态下不能卡住。
- 连续攻击时不得创建新的 GameObject、材质或磁盘资源；运行期只复用已导入资源。
