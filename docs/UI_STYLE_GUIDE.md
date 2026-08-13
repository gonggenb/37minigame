# 一炷江湖 UI 视觉规范

> 文档状态：`Approved Direction`
>
> 适用项目：一分钟武侠 Roguelite
>
> 规范职责：定义所有 UI 最终应该长什么样；`AGENTS.md` 定义 Codex 应该怎样开发。
>
> 参考边界：以《八方旅人 / OCTOPATH TRAVELER》的古典、厚重、精致和克制的视觉气质为参考之一，只提炼视觉语言，不复制其具体界面、图标、纹样、布局或美术素材。

## 1. 规范地位与使用方式

本文件是项目 UI 的长期视觉约束。除非用户明确要求修改整体 UI 风格，否则所有 HUD、窗口、图标、UI Prefab、UI 材质和 AI UI 素材提示词都必须遵守本文件。

当规则冲突时，按以下优先级处理：

1. `AGENTS.md` 与用户最新明确指令：决定开发边界、技术路线和玩法硬规则。
2. 本文件：决定 UI 的视觉方向、组件表现和验收标准。
3. `docs/ui_orientation_guidelines.md`：决定横竖屏、安全区和触摸布局。
4. `docs/art_style_guide.md` 与 `docs/art_production_pipeline.md`：决定角色、场景、图标生产和资源状态。
5. 单个页面的临时需求：只能在以上边界内变化。

任何 UI 任务开始前必须：

1. 读取本文件。
2. 将任务归类为 HUD、窗口、装备、武学、奇遇、洞穴、Boss、地图或结算。
3. 优先复用已有 Token、Prefab、Sprite、Font、Material、Shader 和 UI Component。
4. 缺少正式资源时允许使用占位资源，但资源名、GameObject 名或代码注释中必须带 `PLACEHOLDER_UI`。
5. 同时检查 `960 × 540` 横屏与 `540 × 960` 竖屏；不复制业务状态和交互逻辑。
6. 分开报告 `占位`、`已导入`、`InEngineQA` 和 `Approved`，不得把静态截图当成完整 Play Mode 验收。

修改本规范应作为独立任务进行，并记录修改原因、受影响组件和迁移范围。单个页面不得通过局部实现悄悄改变全局风格。

## 2. 视觉定位

### 2.1 一句话方向

以可触摸的江湖器物承载高压短局信息：暗木与黑铁构成骨架，旧纸与墨色承载内容，黄铜、玉色和朱红建立层级，暖光赋予冒险感。

### 2.2 核心关键词

`东方武侠`、`HD-2D 气质`、`像素幻想`、`古典纸张`、`暗色木质`、`黑铁`、`黄铜`、`暖色灯光`、`低饱和`、`高对比信息层级`。

整体观感应当是：古典、厚重、精致、克制、有冒险感。

### 2.3 视觉原则

- UI 看起来应像游戏世界中的器物、卷册、铜牌或机关，而不是覆盖在游戏上的现代 App。
- 东方元素负责身份：宣纸、墨纹、竹简、玉、印章、云纹与剑纹。
- 古典冒险界面的结构负责质感：深色承托、精细边框、克制高光、明确层级。
- 玩法信息优先于装饰。装饰不得降低文字、数值、轮廓或交互状态的可读性。
- 重要信息主要通过明度、面积、位置和动效建立权重，不通过提高饱和度制造噪声。

### 2.4 禁止方向

- 现代手游式大面积高饱和渐变、发光描边和促销角标。
- 二次元卡牌式过度华丽框体或人物立绘挤占玩法画面。
- 霓虹科技、网页后台、纯扁平化、大量圆角卡片或玻璃拟态。
- Unity 默认 Button、默认灰色渐变框或未经设计的系统控件直接进入正式界面。
- 大量纯白、纯黑、高亮蓝、高饱和紫和荧光色。
- 为每个系统重新发明颜色、字体、按钮、品质框或 Tooltip。
- 直接复刻参考作品的具体 UI、图标、纹样、构图或资产。

## 3. 设计基础与层级

### 3.1 基础单位

以 `4` 个逻辑单位为最小步进，以 `8` 个逻辑单位为主要间距单位。推荐序列：

`4 / 8 / 12 / 16 / 24 / 32 / 48 / 64`

- 内边距优先使用 `8 / 12 / 16 / 24`。
- 同组信息间距小于跨组信息间距。
- 正式框体的圆角半径为 `0–4`；优先直角或短边轻微切角。
- 线宽采用 `1 / 2 / 4` 三档。暗金细线通常为 `1–2`，结构边框为 `2–4`。
- 手机常用触摸区域不得小于 `44 × 44` 个逻辑单位。

### 3.2 固定视觉层级

所有主要界面保持以下顺序：

```text
背景层 Background
  -> 环境压暗 / 模糊 / 纸张底色
装饰层 Ornament
  -> 木纹、墨纹、云纹、铜角、分隔线
容器层 Container
  -> Panel、Slot、卡片、标题栏
信息层 Information
  -> 文本、数值、图标、条形状态
交互层 Interaction
  -> Button、Tab、选中、焦点、Tooltip
```

视觉优先级必须保持：

```text
核心玩法信息
  > 当前操作信息
  > 奖励与成长信息
  > 辅助说明
  > 装饰信息
```

同一屏不得让标题、按钮、奖励、说明和装饰同时达到最高亮度。

## 4. 色彩规范

### 4.1 核心色板

下表是建议的正式 Token。后续实现应集中到 `UIStyleTokens` 或等价主题配置，不应继续在各页面散落近似色值。

| Token | 色值 | 主要用途 |
| --- | --- | --- |
| `ui.bg.ink` | `#0E1112` | 全屏暗幕、最深内容底 |
| `ui.bg.brown` | `#1B1512` | 木质底色、暖暗背景 |
| `ui.surface.wood` | `#2A211B` | 深木外框、标题底 |
| `ui.surface.iron` | `#292F30` | 黑铁面板、按钮底 |
| `ui.surface.raised` | `#343A38` | 悬起卡片、Hover 提亮 |
| `ui.surface.paper` | `#D8CBA5` | 纸张内容区，不作整屏纯底 |
| `ui.text.primary` | `#E9DFC3` | 主文字，暖米白 |
| `ui.text.secondary` | `#A9ADA3` | 次要文字 |
| `ui.text.disabled` | `#6F746E` | 禁用与低权重文字 |
| `ui.accent.brass` | `#B58A46` | 默认重点、边框与选中 |
| `ui.accent.gold` | `#D1AA5A` | 少量最高级高光、关键奖励 |
| `ui.accent.jade` | `#4E8B73` | 正向状态、恢复、内功与暂停外的安全状态 |
| `ui.accent.inkgreen` | `#3E6252` | 低强度绿色背景 |
| `ui.state.warning` | `#B06F34` | 时间警告、风险提升 |
| `ui.state.danger` | `#963A31` | 低血、最后 20 秒、失败风险 |
| `ui.state.paused` | `#536F7A` | 洞穴中主时间暂停 |
| `ui.state.info` | `#667B82` | 中性提示，不使用高亮蓝 |

纯白 `#FFFFFF` 和纯黑 `#000000` 只允许用于小面积高光、阴影计算或临时混合，不作为正式大面积色块。

### 4.2 色彩使用规则

- 默认比例建议为 `70%` 深色中性材质、`20%` 纸色与文字、`10%` 铜/玉/朱红等强调色。
- 一个页面同时只保留一个主强调色；危险、成功等状态色只在必要区域出现。
- 提高重要性时先提高明度和局部对比，再考虑饱和度。
- 文本与实际承载底色的对比度目标：正文至少 `4.5:1`，大字至少 `3:1`。
- 半透明 HUD 必须有稳定的暗色承托或文字阴影，不能假设地图背景始终足够暗。
- 颜色不是唯一编码。状态还应通过轮廓、图标、纹样、文字或动画区分。
- 禁用状态降低明度和对比，不仅降低 Alpha；禁用文本仍须可读。

### 4.3 时间颜色

主地图时间必须沿用项目既定的三段压力表达：

| 剩余时间 | 颜色 | 反馈 |
| --- | --- | --- |
| `60–41 秒` | 墨玉绿 | 稳定，无持续动画 |
| `40–21 秒` | 黄铜 / 暖金 | 节点钟声，短暂金属高光 |
| `20–0 秒` | 暗朱红 | 低频脉冲与边缘示警，禁止高饱和频闪 |
| 洞穴暂停 | 暗蓝灰 | 显示“主时间暂停”和暂停符号，进度停止 |
| Boss 战 | 暗金 + 独立计时 | 不再使用主地图倒计时语义 |

## 5. 材质规范

### 5.1 材质角色

| 材质 | 用途 | 表现方式 |
| --- | --- | --- |
| 深色木质 | 外框、页签基座、主菜单 | 低对比木纹、哑光、避免照片级纹理 |
| 黑铁 | 按钮、结构边、战斗信息 | 冷暗、轻微磨损、边缘高光克制 |
| 黄铜 / 暗金 | 重点边线、角件、计时器 | 只占小面积，不做整面金色 |
| 羊皮纸 / 旧纸 | 说明、地图、事件正文 | 暖灰米色、轻微纤维和旧化，不能影响文字 |
| 皮革 | 背包、装备页局部 | 暗褐、细压纹、少量缝线 |
| 玉石 | 正向状态、高级东方识别 | 低饱和青绿、小面积半透高光 |
| 石材 | Boss、洞穴或遗迹局部 | 冷灰、低反射，不和主内容争对比 |

### 5.2 标准面板材质堆叠

```text
环境压暗幕（需要时）
  -> 深木或黑铁外框
  -> 1–2 px 暗金内线
  -> 纸张或深色内容区
  -> 极弱内阴影 / 边缘磨损
  -> 信息与交互
```

- 禁止只用一块 `Texture2D.whiteTexture` 染色矩形作为正式大面板。
- 正式面板优先使用可九宫格拉伸的 Sprite；边角、纹样和切角不得随尺寸拉伸变形。
- 纹理对比必须低于文字、图标和状态条；纸张噪点在缩放后不得造成闪烁。
- 高光应表现铜器或铁器的边缘，不使用大面积柔亮外发光。

## 6. 字体规范

### 6.1 字体角色

| 角色 | 用途 | 当前策略 |
| --- | --- | --- |
| `Display` | 主标题、Boss 名、重大阶段标题 | 未来选择授权清晰的古典中文标题字体；未批准前使用当前粗体，不临时换字库 |
| `Heading` | 窗口标题、卡片名、按钮 | `Noto Sans CJK SC Bold` 作为当前可运行基线 |
| `Body` | 正文、说明、Tooltip | `Noto Sans CJK SC Regular` |
| `Numeric` | 倒计时、气血、伤害、价格 | 当前与 Heading 共用；须使用等宽数字或稳定数字宽度设置 |

字体文件必须有清晰授权并随构建打包。不得依赖操作系统字体，也不得因为某个页面“更有感觉”而单独换字体。

### 6.2 字级与排版

以逻辑字号定义，不把下面数值直接当作固定屏幕像素：

| Token | 建议字号 | 用途 |
| --- | ---: | --- |
| `type.caption` | `11–12` | 标签、角标、次要状态 |
| `type.body` | `14` | 正文、列表说明 |
| `type.bodyStrong` | `16` | 关键说明、按钮 |
| `type.heading` | `20–22` | 窗口标题、卡片主名 |
| `type.display` | `28–40` | 主菜单、Boss 标题 |
| `type.timer` | `28–48` | 核心倒计时，按布局调整 |

- 手机正文推荐不低于 `14`，极小角标不得承载关键内容。
- 标题可增加少量字距；正文保持自然字距和 `1.35–1.5` 倍行高。
- 同一行尽量只承担一个信息层级；数值和单位之间保持固定间距。
- 避免全屏大量居中文本。正文默认左对齐，数值列按位对齐。
- 中文不得截断、挤压或靠缩小到不可读来适配；应改写文案、换行或调整布局。
- 关键交互信息不能只放在 Hover Tooltip 中。

## 7. Panel 与窗口规范

### 7.1 Panel 类型

| 组件 | 材质与用途 |
| --- | --- |
| `UI_Panel_Default` | 深木 / 黑铁结构，通用暗色内容区 |
| `UI_Panel_Paper` | 木或铜框 + 旧纸内容区，用于地图、事件、说明 |
| `UI_Panel_Combat` | 黑铁、暗红小面积强调，用于敌方与战斗即时信息 |
| `UI_Panel_Boss` | 更厚结构边、暗金角件、石材或暗红纹理；不可只放大普通面板 |
| `UI_Panel_Overlay` | 全屏压暗和输入拦截，不承担内容本身 |

### 7.2 标准窗口结构

```text
WindowRoot
├── BackdropBlocker
├── Frame
│   ├── OrnamentLayer
│   ├── Header
│   │   ├── Title
│   │   ├── ContextValue（可选）
│   │   └── CloseButton（可选）
│   ├── Divider
│   ├── ContentViewport
│   ├── PrimaryActions
│   └── HelpText
└── TooltipAnchorLayer
```

- 信息顺序固定为：标题 → 主要内容 → 主要操作 → 辅助说明。
- 标题区只用小面积装饰，不使用巨型 Banner。
- 一屏只突出一个主要操作；次要操作降低面积或明度。
- 弹窗宽度和高度由内容与安全区决定，不强行填满屏幕。
- 窗口使用 `24` 的主内边距，紧凑窗口可用 `16`；卡片间距优先 `8–12`。
- 横屏允许分栏；竖屏改为纵向、分页或滚动，不得简单等比压缩横屏窗口。

## 8. Button、Tab 与交互规范

### 8.1 Button 外观

- 形状：矩形，短边可做 `2–6` 单位切角；不用大圆角胶囊。
- 正常态：黑铁或暗木底，1–2 px 暗铜边，暖米白文字。
- Hover / Focus：边缘提亮并出现极轻金属反光；不能只改变文字颜色。
- Pressed：整体下压 `1–2` 单位或缩放至 `0.98`，背景略暗。
- Selected：暗金内线或印章式角标持续显示。
- Disabled：降低明度、移除高光，保留清楚轮廓和可读文字。
- Danger：暗红只用于边线、图标或小面积底色，不把整屏按钮做成鲜红。

### 8.2 标准组件

| 组件 | 用途 | 最小逻辑尺寸 |
| --- | --- | --- |
| `UI_Button_Primary` | 页面唯一主要操作 | `120 × 44` |
| `UI_Button_Secondary` | 返回、取消、辅助操作 | `104 × 44` |
| `UI_Button_Icon` | 设置、关闭、快捷入口 | `44 × 44` |
| `UI_Tab` | 同层页面切换 | 高度 `40` |
| `UI_Toggle` | 设置项 | 点击区高度 `44` |

- 不同组件共享同一交互状态语言。
- 键鼠需要清晰 Focus；移动端不能依赖 Hover。
- 点击反馈采用颜色、轻微缩放和短音效的组合，不使用 Bounce 或持续跳动。
- 正式页面不得直接使用 Unity / IMGUI 默认 Button Skin。

## 9. Icon、Slot 与品质规范

### 9.1 Icon 生产规则

- AI 母版为 `256 × 256` 透明画布；Unity 正式交付为 `128 × 128`。
- 主体位于中央 `192 × 192` 安全区，四边至少留 `32 px` 透明边距。
- 实际验收尺寸为 `64 / 48 / 32 px`；`32 px` 只允许轮廓简单的图标。
- 视觉语言为像素绘制或像素化处理、明确墨切轮廓、克制材质、高光不超过主体的小面积。
- 识别顺序固定为：轮廓 > 材质 > 细节。
- 图标本体不烘焙文字、等级、品质框、冷却遮罩、选中状态或背景场景。
- 同批资源必须统一主体占比、视角、墨线重量、色温和高光方向。
- 颜色不可作为唯一分类依据；武学类型还要有不同形状或角标。

详细生产、命名与导入设置继续遵守 `docs/art_production_pipeline.md`。

### 9.2 Slot 结构

```text
UI_ItemSlot
├── FrameByQuality
├── Icon
├── CategoryCorner
├── StackOrRank
├── CooldownMask（按需）
├── StateOverlay
└── SelectionFocus
```

同一结构派生 `UI_EquipmentSlot` 和 `UI_SkillSlot`，不得为每个页面复制一套实现。

### 9.3 品质表达

允许保留灰白、玉绿、青蓝、紫墨、暗金的辅助识别，但颜色不能是唯一差异：

| 品质 | 边框与材质复杂度 | 光效 |
| --- | --- | --- |
| 普通 | 单层铁边，无纹样 | 无 |
| 精良 | 双线或单枚玉色角件 | 无或静态微光 |
| 稀有 | 双线 + 一组简化云纹 | 极弱呼吸高光 |
| 绝品 | 更完整角件 + 小面积印纹 | 低频金属扫光 |
| 传说 | 独特轮廓 + 暗金 / 玉石组合 | 克制粒子与扫光 |

高品质增加结构和材质，不单纯提高亮度、饱和度或外发光强度。品质变化不能改变图标主体比例。

## 10. HUD 规范

### 10.1 总原则

地图、玩家、怪物和场景是主要视觉主体。HUD 必须紧凑、贴近安全区边缘，并通过按需展开承载次要信息。

HUD 信息优先级：

1. 60 秒主时间或 Boss 独立阶段时间。
2. 玩家气血和即时危险状态。
3. 当前主要武学、冷却与限时 Buff。
4. 等级、修为、铜钱与装备摘要。
5. 辅助说明与状态日志。

### 10.2 时间母题

“时间”采用怀表、沙漏或日晷式视觉母题，优先发展为项目最稳定的 UI 记忆点。

- 倒计时必须同时提供数值和进度，不只显示一个大数字。
- 推荐结构：小型古铜表盘 / 刻度框 + 中央秒数 + 横向或环形压力进度。
- `40 秒`、`20 秒`节点在视觉和声音上各反馈一次。
- 最后 20 秒允许低频脉冲和屏幕边缘示警，但不得持续震动、频闪或遮挡战斗。
- 普通战斗继续显示并缩短同一主时间。
- 洞穴将同一计时器切换为蓝灰暂停态，进度与节点音效同步停止。
- Boss 战使用独立计时器，不沿用主时间进度、红色压力段或丧钟语义。

### 10.3 角色 HUD

- 气血条保留分段、受击残影与低血脉冲，但避免纯鲜红大面积填充。
- 武学图标显示品类与重数；冷却使用独立遮罩，恢复或触发时短暂按流派色高亮。
- 限时 Buff 显示图标、剩余秒数和叠层；同类独立计时 Buff 合并显示总效果，并以最近到期层计时。
- 默认只显示本局决策所需信息；完整属性放入角色窗口。
- 横屏 HUD 靠四角，中心尽量留给路线与战斗；竖屏优先占用顶部安全区并保留右侧快捷操作栏。

## 11. 各类型界面规则

### 11.1 战斗与 Boss

- 战斗双方信息应有明确阵营差异，但保持同一组件体系。
- 敌方使用紧凑暗红黑铁卡；保留名称、等级、气血、受击残影和关键异常状态。
- 玩家构筑集中在技能栏，中央交锋区只保留招数、时间与瞬时状态，底部只保留战报和战斗效果。
- Boss 界面通过更厚框体、独立暗金纹样和阶段标记增强重量，不靠夸张缩放普通卡片。

### 11.2 背包、装备与商店

- 背包和装备偏皮革、暗木、竹简标签；物品详情可使用小块纸张内容区。
- 货架卡必须先突出图标、名称、效果和价格，折扣是次级状态，不使用促销式高饱和标签。
- 已装备、不可购买、售罄和选中必须具有不同轮廓或图标状态。
- 商店横屏可多列，竖屏改双列或单列滚动；虚拟摇杆和地图 HUD 在模态商店开启时必须隐藏或降到不可交互背景层。

### 11.3 武学、奖励与 Tooltip

- 三选一首先表现流派、武学名称、当前重数变化和实际效果；故事说明次之。
- 卡片选中时使用边框、印章角标和轻微抬升，不大幅放大。
- Tooltip 采用暗色或纸张小面板，结构为名称 → 类型/品质 → 主要效果 → 条件/说明。
- 移动端必须支持点击或长按查看，不能只依赖鼠标悬停。

### 11.4 奇遇、洞穴、地图与结算

- 奇遇和地图优先使用旧纸、墨线和印章，保持简明而非复杂传统纹样堆叠。
- 洞穴界面可增加石材和玉色，但仍复用全局按钮、Slot 与字体。
- 结算界面突出胜负、Boss 结果和本局构筑摘要；“再入江湖”是唯一主按钮。
- 弹窗必须有输入拦截和明确关闭方式，不能让底层高频操作继续显示为可交互状态。

## 12. 动效与反馈规范

UI 动效用于表达层级、状态和材质，不用于制造持续热闹。

| 动效 | 建议时长 | 使用场景 |
| --- | ---: | --- |
| 淡入 / 淡出 | `120–220 ms` | Tooltip、提示、局部内容 |
| 短距滑入 | `180–280 ms` | 窗口、奖励卡 |
| 轻微缩放 | `100–160 ms` | 按下、选中，范围 `0.98–1.02` |
| 金属扫光 | `450–700 ms` | 高品质获得、重要确认，仅播放一次 |
| 低频脉冲 | `800–1400 ms` | 低血、最后 20 秒，不同步叠加过多 |

- 缓动优先使用 ease-out 进入、ease-in 退出。
- 避免 Bounce、大幅缩放、持续跳动、强烈震动和多层粒子爆发。
- 页面打开后，装饰动画不应持续抢夺核心玩法信息。
- 支持减少动效策略：关闭非必要扫光、粒子和脉冲，保留状态变化本身。
- 动画完成前后交互状态必须一致；不得因为方向切换重置选择或计时。

## 13. 高清框架与像素内容的边界

- 场景：像素角色 + 低多边形手绘场景。
- 角色：像素动画，保持清晰边缘与统一像素密度。
- 图标：像素或像素化处理，可保留有限手绘质感。
- UI 框架：高清材质、九宫格边框和清晰装饰。
- 字体：高清字体，优先可读性。

不要求整套 UI 完全像素化。高清框架不得用模糊滤镜破坏像素角色和图标，像素图标也不得因非整数缩放出现抖动或糊边。

## 14. 东方武侠元素

允许使用：木、铜、玉、竹简、宣纸、墨纹、简化山水、云纹、剑纹和印章。

- 每个组件最多选择一项主纹样和一项材质点缀。
- 云纹、剑纹和山水只作为低对比边角、分隔或底纹，不铺满正文区域。
- 印章适合选中、完成、稀有或确认语义，不作为所有按钮的通用装饰。
- 玉只用于正向、稀有或内功相关识别，避免整页青绿。
- 东方元素负责身份，不能牺牲触摸范围、数值对齐和状态清晰度。

长期重复母题：

| 母题 | 对应玩法 | 使用位置 |
| --- | --- | --- |
| 怀表 / 沙漏 / 日晷 | 60 秒压力 | 主时间 HUD、节点提示、结算用时 |
| 地图 / 羊皮纸 | 路线探索 | 地图、奇遇、路线提示、结果摘要 |
| 剑 / 玉 / 铜 / 墨 | 武侠身份 | 武学、装备、品质、边角纹样 |

## 15. Unity UI 组件库建议

### 15.1 推荐目录

```text
Assets/UI/
├── Theme/
│   ├── UIStyleTokens.asset
│   └── UITheme_Default.asset
├── Fonts/
├── Sprites/
│   ├── Panels/
│   ├── Buttons/
│   ├── Frames/
│   ├── Ornaments/
│   └── States/
├── Materials/
├── Prefabs/
│   ├── Primitives/
│   ├── HUD/
│   ├── Windows/
│   └── Slots/
└── PLACEHOLDER_UI/
```

这是未来迁移目标，不要求当前任务立即重建目录。

### 15.2 推荐组件清单

```text
UI_Button_Primary
UI_Button_Secondary
UI_Button_Icon
UI_Panel_Default
UI_Panel_Paper
UI_Panel_Combat
UI_ItemSlot
UI_EquipmentSlot
UI_SkillSlot
UI_Tooltip
UI_Timer
UI_HealthBar
UI_ProgressBar
UI_Tab
UI_Toggle
UI_Popup
UI_StatusBadge
UI_BuffSlot
```

### 15.3 典型 Prefab 结构

```text
UI_Button_Primary
├── BackgroundNineSlice
├── MetalEdge
├── HighlightSweep
├── Label
└── FocusMarker

UI_Timer
├── ClockFrame
├── TickMarks
├── Progress
├── TimeValue
├── PhaseLabel
└── WarningEffect

UI_HealthBar
├── Frame
├── Track
├── DamageTrail
├── Fill
├── SegmentMarks
├── ValueLabel
└── DangerPulse

UI_Tooltip
├── BackgroundNineSlice
├── Header
├── TypeAndQuality
├── EffectText
└── DescriptionText
```

### 15.4 技术落地原则

- 视觉 Token、Sprite 和状态映射集中管理；页面只请求语义角色，不自行定义近似色。
- 横竖屏布局可分两个 Root，但共用 Controller、数据和事件。
- 公共控件只维护一个源 Prefab，页面使用 Prefab Variant 或组合，不复制后改名。
- 九宫格 Sprite 必须验证边角不变形；Material 不应在运行时为每个实例重复创建。
- 正式化迁移按组件逐步进行，不进行一次性大范围重构。

## 16. 占位资源规则

现有资源不能满足需求时允许占位，但必须满足：

- 文件、GameObject、Prefab 或代码注释中出现 `PLACEHOLDER_UI`。
- 占位图使用低饱和中性底和清晰斜纹 / 水印，不伪装成正式美术。
- 占位资源不得覆盖或改名为正式资源；替换时保留引用检查。
- 任务完成报告必须列出所有仍在使用的 `PLACEHOLDER_UI`。
- 未经 Play Mode 横竖屏验收的资源不得标记为 `Approved`。

## 17. AI UI 美术生成规则

### 17.1 固定风格前缀

所有 UI 美术生成 Prompt 必须继承以下基础方向，不得每次重新定义风格：

```text
Production UI asset for a dark eastern martial arts fantasy roguelite,
HD-2D inspired visual language without copying any existing game asset,
aged parchment, dark wood, black iron, antique brass, subtle gold ornament,
restrained ink and jade accents, low saturation, warm lighting,
high value contrast, handcrafted adventure interface, readable silhouette.
```

### 17.2 对象与生产约束

在固定前缀后增加具体对象，例如 `weapon`、`skill`、`inventory`、`shop`、`timer`、`boss`、`event`，并明确：

- 资产类型与用途。
- 精确画布尺寸和透明背景要求。
- 九宫格安全边界或图标 `192 × 192` 主体安全区。
- 观察距离与最小验收尺寸。
- 是否允许纹理、边框、粒子与高光。
- 禁止文字、Logo、水印、角色立绘、场景和多余物件。

通用负面约束：

```text
No modern mobile app UI, no neon sci-fi, no glassmorphism,
no oversized rounded cards, no saturated gradients, no flat web dashboard,
no text, logo, watermark, copyrighted motif, or direct imitation of an existing UI.
```

### 17.3 图标 Prompt 模板

```text
Use case: production Unity UI icon for a one-minute wuxia roguelite.
<固定风格前缀>
Create one centered <对象与动作> icon on a transparent 256 × 256 canvas.
Keep the subject inside a centered 192 × 192 safe area.
Prioritize silhouette, then material, then detail.
The icon must remain recognizable at 64, 48, and 32 px.
No text, frame, rarity border, cooldown overlay, scenery, watermark, or cast shadow.
<通用负面约束>
```

### 17.4 九宫格框体 Prompt 模板

```text
Use case: production nine-slice Unity UI frame for <Panel/Button/Slot>.
<固定风格前缀>
Front-facing orthographic asset, transparent background, symmetrical geometry.
Keep all corners and ornaments inside fixed corner safe zones.
Leave the center and edge stretch regions visually quiet and tile-safe.
Use <dark wood / black iron / parchment> with restrained antique brass accents.
No text, icon, character, scenery, watermark, baked shadow, or asymmetrical lighting.
<通用负面约束>
```

生成结果仍须经过透明边缘、九宫格拉伸、实际尺寸、横竖屏和 Unity Play Mode 检查，不能因 Prompt 符合规范就直接标记正式资源。

## 18. 当前项目 UI 审计（2026-08-13）

本节最初基于仓库代码、资源和既有 Play Mode 截图建立静态审计；后续迁移状态单独记录在 `18.0`，未标记完成的问题仍是迁移清单。

### 18.0 第一、二批迁移状态

2026-08-13 已开始主地图 HUD 第一批迁移：

- `ArtSource/Previews/UI/MainHUD/hud_mainmap_visual_mockup_v01.png`：`Generated`，只作为视觉方向参考，不直接作为运行时贴图。
- `WuxiaUiTheme`：已集中语义色并接入正式 `Panel_Default`、`Panel_Paper`、`Panel_Boss`、`Button` 多状态和 `Slot` 九宫格 Sprite；正式资源缺失时才回退到带 `PLACEHOLDER_UI` 标记的程序材质。
- `PrototypeHUDController`：主地图、升级选择、Boss 引导和结算已统一九宫格面板、按钮与 Slot 语言；独立表盘计时器仍是 `PLACEHOLDER_UI`，等待正式计时器 Sprite。
- `BattleScreenController`：普通战斗、洞穴战斗和 Boss 战已按 `Combat / Paper / Boss` 语义选择统一框体；竖屏角色与交战区域放置在屏幕中段。
- 竖屏紧凑化：主地图 HUD 宽度与顶部堆叠高度已再次收紧，修为、铜钱和构筑栏改为低矮 `Compact Surface`；战斗顶部标题、双方状态、交锋提示与底部战报同步减高。高频图标按钮仍保留至少 `44 × 44` 触摸区，压缩的是装饰和次级文字，不压缩关键交互。
- `CaveRoomController`：洞穴暂停提示、商人窗口、商品卡与按钮已接入统一框体和 Slot。
- `MobileInputController`：横屏摇杆缩小，并在商店模态界面打开时隐藏和停止输入。
- `WuxiaResponsive`：WebGL 模板按浏览器视口铺满 Canvas，并用 CSS `safe-area-inset-*` 避开移动浏览器异形屏区域。
- 当前状态：Unity 编译通过；Editor Play Mode 已完成竖屏完整一局回归（普通战斗期间主时间继续、时间归零后结束当前战斗再进 Boss、Boss 独立计时、结算）及洞穴战斗暂停主时间检查；四组模拟 Safe Area 校验通过；Development WebGL 构建通过，并在浏览器 `540 × 960`、`960 × 540` 视口完成加载、Canvas 尺寸和首屏交互检查。
- 批准边界：上述正式九宫格均为 `InEngineQA`，尚未标记 `Approved`；仍需 iOS / Android 真机刘海安全区、触摸手势及发布配置 WebGL 验证。

### 18.1 已符合或可保留的基础

- `PrototypeHUDController` 已采用暗底、暗金、玉色、纸色和朱红的低饱和方向，可作为 Token 迁移起点。
- 主地图 HUD 已将气血、武学和主时间组织到同一信息组，倒计时具有较高视觉权重。
- `TimePressureBarRenderer` 和 `tex_ui_timebar_frame_v01.png` 已提供分段时间条与独立框体基础。
- `RuntimeChineseFont` 和 `Resources/Fonts` 已建立随构建打包的简体中文 Regular / Bold 基线。
- 武学、装备和内容图标已经使用独立资源，并有 `128 / 64 / 48 / 32 px` 生产与可读性规则。
- 横竖屏和 Safe Area 已有独立规范及响应式代码基础，应继续复用。

### 18.2 需要逐步统一的问题

| 优先级 | 问题 | 证据与影响 | 后续建议 |
| --- | --- | --- | --- |
| `P1` | 缺少单一 UI Token 来源 | HUD、战斗、洞穴脚本各自硬编码近似但不同的颜色、字号和状态色 | 先建立 `UIStyleTokens`，逐组件迁移，不一次重构全部页面 |
| `P1` | 正式按钮迁移尚未覆盖所有交互类型 | Primary / Secondary 与调试按钮已使用正式九宫格；Icon、Tab 仍主要依赖统一样式或程序图标 | 下一批制作 `Button_Icon`、`Tab` 正式 Sprite，并逐页替换 |
| `P1` | 大面板已完成第一轮材质化，但高级窗口层次仍少 | HUD、战斗、洞穴、Boss、升级和结算已有深木 / 黑铁 / 纸张九宫格；商店、角色与装备仍需细化装饰层 | 保持现有三套框体，按窗口语义增加少量可复用装饰，不新增页面私有框体 |
| `P1` | 60 秒母题尚未形成 | 当前倒计时清晰，但主要仍是文字 Chip 与横条，缺少怀表 / 沙漏 / 日晷的持续识别 | 在不改变计时逻辑的前提下，先替换 `UI_Timer` 视觉组件 |
| `P1` | 模态窗口和底层操作层级冲突 | 现有商店截图左下仍显示大尺寸虚拟摇杆，破坏窗口层级且可能误示可交互 | 模态窗口开启时隐藏或禁用摇杆和底层 HUD，并保留输入拦截 |
| `P1` | 缺少真正的共享 UI 组件库 | 当前主要由多个大型 IMGUI Controller 绘制，项目未形成可复用 UI Prefab 体系 | 新 UI 从组件库开始；旧 IMGUI 在需要改动时渐进迁移 |
| `P2` | 横屏 HUD 与商店占用仍偏大 | 竖屏主地图和战斗 HUD 已完成紧凑化；横屏左上角色卡仍形成较大信息组，商店仍接近全屏 | 横屏默认态继续折叠次级构筑；商店改为清晰模态窗口并减少无效留白 |
| `P2` | 字体只有功能层级，缺少批准的标题角色 | 当前 Noto Sans CJK 可读，但主标题、Boss 名和正文的气质区分主要依靠字号与粗细 | 保留当前字体保证构建，单独评估并授权一款 Display 字体 |
| `P2` | 状态主要依赖颜色和细线 | 品质、流派、选中和危险状态尚未全面加入纹样、角标与边框复杂度 | 按 Slot 结构补充形状、角件和状态图标 |
| `P2` | 部分图标在运行时程序绘制 | 设置、主页等图标由代码临时生成，形状语言与批量美术图标不完全统一 | 后续替换为同批正式图标，替换前标记 `PLACEHOLDER_UI` |
| `P2` | 视觉常量和组件状态重复实现 | HUD、战斗、洞穴分别维护自己的 Label、Button 和 Panel 方法 | 先统一状态规范，再抽公共 Renderer / Prefab，避免边做边分叉 |
| `P3` | 资源状态仍需统一登记 | 部分图标与 UI 资源为 `InEngineQA`，不等于 `Approved` | 建立 UI 资源清单，分别记录 Imported、InEngineQA、Approved |
| `P3` | 可访问性验收未形成固定流程 | 现有实现重视尺寸，但灰度、色弱、减少动效和键盘 Focus 尚未统一记录 | 将对应检查加入每个主要 UI 的验收清单 |

### 18.3 建议迁移顺序

1. 建立语义 Token 与统一按钮状态，不改变页面布局。
2. 制作三套基础九宫格：`Panel_Default`、`Panel_Paper`、`Button`。
3. 将主时间做成首个正式组件 `UI_Timer`，保持三条核心时间规则不变。
4. 统一 `UI_HealthBar`、`UI_SkillSlot`、`UI_BuffSlot`，再收紧主地图 HUD。
5. 统一设置、升级、角色、装备与商店窗口。
6. 最后处理 Boss、结算的高级框体和高品质动效。

## 19. 每个 UI 的验收清单

- 已读取本规范，并标明 UI 类型。
- 复用了现有 Token、Prefab、Sprite、Font、Material 或说明了不能复用的原因。
- 没有 Unity 默认 Button、临时高饱和渐变、大圆角卡片和未经批准的字体。
- 信息层级符合“核心玩法 > 当前操作 > 奖励 > 说明 > 装饰”。
- 颜色不是状态的唯一识别方式。
- 图标在目标尺寸下轮廓清楚，未烘焙文字、品质与冷却。
- 横屏 `960 × 540`、竖屏 `540 × 960`、Safe Area 和 `44 × 44` 触摸区通过检查。
- 中文没有截断、重叠和不可读缩小。
- 模态窗口正确拦截输入并处理底层 HUD / 摇杆。
- 动效克制，减少动效策略可用，不存在频闪和持续抢焦点。
- 所有占位资源均带 `PLACEHOLDER_UI`，并在任务报告中列出。
- 普通战斗继续主时间、洞穴暂停主时间、Boss 独立计时三条规则没有被 UI 改动破坏。
- 已区分静态检查、Unity 导入、Play Mode 横屏验证、Play Mode 竖屏验证和最终批准状态。
