# 战斗背景地面构图修正

2026-09-22，Unity 6000.5.4f1，MainPrototype Editor Play Mode。

## 改动

旧背景地面后缘约在画面自上而下 55%–75%，其中山隘最低。竖屏角色脚点在屏幕中段，裁切后容易落在山谷、墙面或地面后缘之外。

使用内置 image_gen 对六张背景分别重生成：保持客栈、竹院、山隘、林地、采石场和血月寺院的主题、配色及绘画风格，把地面后缘提升至约 30%–40%，中央战斗区域始终为连续平地。角色站位、脚点、阴影和玩法代码未修改。

替换 `Assets/Art/Generated/Backgrounds/` 内以下文件，保留文件名、GUID、场景引用及原有导入配置：

- `bg_battle_central_inn_v01.png`
- `bg_battle_east_bamboo_v01.png`
- `bg_battle_north_pass_v01.png`
- `bg_battle_west_forest_v01.png`
- `bg_battle_south_quarry_v01.png`
- `bg_boss_bloodmoon_temple_v01.png`

新增 `ArtSource/Raw/BattleGrounding/`：六张 v02 生成原图、Before/ 六张旧图、完整提示词 `prompts_v02.md`、SHA-256 清单 `manifest_v02.json`。本目录新增验证说明、计时记录和十二张 Play Mode 截图。未新增或修改脚本、Prefab 或场景；工作区已有的其他修改保持原样。

生成原图为 1672×941，保留原生像素，不再缩放加工。当前 Web 平台既有导入上限为 1024，Unity 实际贴图为 1024×576；此轮保持该移动端配置。运行时文件与生成原图 SHA-256 一致，六张均不同于旧图。

## 验证

- Generated：六张完成，原图与提示词已保存。
- Normalized：不需要透明处理或切图；保留生成尺寸，未重采样。
- Imported：六张纹理成功导入，MainPrototype 的五张普通背景及 Boss 引用均有效；三个关卡原有 GUID 引用保持不变。
- InEngineQA：六张背景各检查 540×960 和 960×540，共十二张真实 Editor Play Mode 截图。逐张确认主角和对手脚下为连续地面，背景不会在两人之间形成悬崖、墙面或水面；阴影继续使用原有运行时绘制。
- Unity Console 查询：0 条 error。
- 三条时间规则在当前 Play Mode 受控夹具中通过：普通战斗主时间减少，CaveRunning 且战斗活动时主时间不变，BossBattle 主时间不变而独立时间增加。数值见 `timing_checks.txt`。通过现有方法/阶段与反射设置夹具，并未覆盖整条自然进入洞穴的用户流程。
- 截图使用临时高气血、停止战斗协程和冻结时间的视觉夹具，普通背景通过运行时临时切换逐张检查；它们不是自然跑局或平衡数据。计时检查在截图冻结前执行。
- 已退出 Play Mode，恢复原 Game View 尺寸和后台运行设置，删除临时 QA 分辨率；未保存测试状态到场景。
- Approved：尚未标记；手机真机、不同安全区及用户最终视觉验收仍待完成。

## 运行与复核

打开 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode，开始第二关并碰怪，重复进入战斗查看随机背景。通过现有调试入口进入最终 Boss，检查血月寺院。Game View 分别选择 540×960 和 960×540，观察待机与攻击时脚底、阴影以及背景地面的关系。

无需新建 GameObject、挂载脚本或手动拖入任何 Inspector / Prefab / UI 引用。未修改运行时中文文案。三条核心计时规则保持不变。

## 截图

普通背景：`bg_battle_<主题>_v01_portrait.png`、`bg_battle_<主题>_v01_landscape.png`。

Boss：`boss_portrait.png`、`boss_landscape.png`。
