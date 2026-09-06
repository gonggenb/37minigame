# 狐火初现：开场对话 v01

## 剧情

主角追查山下失踪的人，循狐火上山。九尾妖狐试探他的来意，将答案留在血月古刹。
这是单局探索的动机，不新增任务列表、营救玩法、剧情分支或局外养成；失踪者的后续尚未制作。

| 句 | 说话者 | 台词 |
| --- | --- | --- |
| 1 | 旁白 | 暮色压下山道。村口的寻人纸被风卷起，尽头却亮着一簇不肯熄灭的狐火。 |
| 2 | 主角 | 山下失踪的人，最后都来过这里。这一路的狐火，是你布下的？ |
| 3 | 九尾妖狐 | 追了这么远，竟只为几个素不相识的人？ |
| 4 | 主角 | 他们还活着？ |
| 5 | 九尾妖狐 | 想知道，就亲自来血月古刹问我。 |
| 6 | 主角 | 你既肯现身，又何必躲在傀儡后面？ |
| 7 | 九尾妖狐 | 山门不是谁都能过的。莫让你那点侠气，先折在半路。 |
| 8（教学） | 九尾妖狐 | 先过山道，再来寻我。可别连守路的家伙都应付不了。 |
| 8（正式开场） | 九尾妖狐 | 给你六十息准备。找些趁手的本事，再来叩我的山门。 |
| 9 | 主角 | 路我会走，人我会找。到了古刹，你最好有个答案。 |
| 10 | 旁白 | 狐火散入山雾，绯红的身影随之淡去。你收拢衣襟，踏上了通往山门的旧路。 |

正式开场另保留一句出发提示，说明起手武学、普通战斗计时、洞穴暂停与路牌。
教学开场末句操作为“踏入山道”，正式开场为“选择起手武学”，避免此前“点燃主香”与实际流程不一致。
关卡选择直接进入已解锁的关卡2仍沿用原流程，不强行插入开场。

文案唯一运行来源：`Assets/Scripts/Runtime/OpeningDialogueCatalog.cs`。
Boss 名与古刹专名由 `GameTextCatalog` 维护，主角名使用本局角色配置。

## 交互与分层

- 28 字/秒逐字出现，点击对话框或空格/回车先补全当前句，再次操作进入下一句。
- 同一帧的重复输入只处理一次，不自动跳过短句，不新增剧情跳过或自动播放入口。
- 标题缩为左上角小标签；底部共享主题九宫格、姓名牌、独立中文文字。
- 开场前两句只显示主角；妖狐回应时出现；末尾旁白时妖狐离场。
- 说话者较大且明亮，听话者压暗并在后层绘制；压暗 RGB 保持人物不透明。
- 高清对话立绘只用于序章，战斗像素动画保持原样。
- Resources 自动加载，无需场景、GameObject、Prefab 或 Inspector 手动绑定。

## 美术文件与状态

用户确认的方向预览：`ArtSource/Previews/UI/OpeningDialogue/opening_direction_v01.png`。
原概念图：`ArtSource/References/OpeningDialogue/`。
内置 imagegen 生成提示词全集：`ArtSource/Raw/OpeningDialogue/prompts.json`。

| 资源 | 文件 | 状态 |
| --- | --- | --- |
| 古刹背景 | `Assets/Resources/OpeningDialogue/temple_dusk_v01.png` | Imported / InEngineQA |
| 主角立绘 | `Assets/Resources/OpeningDialogue/portrait_hero_v01.png` | Normalized / Imported / InEngineQA |
| 妖狐立绘 | `Assets/Resources/OpeningDialogue/portrait_fox_v01.png` | Normalized / Imported / InEngineQA |

原始 RGB 母版保留于 `ArtSource/Raw/OpeningDialogue/`，未覆盖。
用户明确允许本地 Python 抠图后，用 `Tools/process_opening_portraits.py` 去除棋盘格、修正发束/袖口/狐尾间隙与浅色边缘，输出真实 RGBA PNG。
两张立绘均为 1024 × 1536，Unity 使用 RGBA32、Bilinear、Clamp、无 Mipmap、无压缩、不做非二次幂缩放。
背景保留生成尺寸 1672 × 941，按屏幕方向居中裁切；UI 与中文文字均为独立运行时层。
运行时通过 Resources 自动加载，原像素人物只作为文件缺失回退。

- 透明母版与 alpha 比例记录：`ArtSource/Normalized/OpeningDialogue/`。
- 深浅底色边缘检查图：`ArtSource/Previews/UI/OpeningDialogue/alpha_edge_review_v01.jpg`。
- Unity 游戏内截图：`docs/validation/opening_dialogue_images/`。
- 并排效果预览：`ArtSource/Previews/UI/OpeningDialogue/opening_ingame_v01.png`。
- 可复现环境：Python 3.9、Pillow 11.3.0、NumPy 2.0.2、SciPy 1.13.1。
- 执行：`/tmp/wuxia-opening-tools/bin/python Tools/process_opening_portraits.py`；环境不在仓库内，换机器需先安装上述依赖。

本批次已完成 Editor 横竖屏画面检查与鼠标/空格翻页验证，尚未标为 Approved，仍需手机真机与用户最终视觉验收。

## 运行

打开 MainPrototype，进入 Play Mode，从首页关卡选择进入关卡1，即可重看十句开场。
点击对话框、空格或回车推进。教学加载后仍须点击“开始探索”才开启 30 秒计时。
验证范围见 `validation/opening_dialogue_2026-09-06.md`。
