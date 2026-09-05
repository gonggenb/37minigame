# 九尾妖狐技能帧动画接入

日期：2026-09-06。用户批准技能概念图后，制作并接入「狐火连击、妖甲护体、残血狂暴」。

## 实现行为

| 技能 | 动作和特效 | 战斗同步 |
| --- | --- | --- |
| 狐火连击 | 8 帧收扇/聚势/三次挥扇/收势，6 帧火种/狐首火弹/爆焰 | 动作 1.16 秒；0.45、0.65、0.85 秒命中；各在命中前 0.18 秒发射 |
| 妖甲护体 | 8 帧交臂/九尾合拢/展臂，6 帧凝甲/维持/碎甲 | 70% 气血立即获得最大气血 12% 妖甲；动作 0.80 秒；护甲耗尽触发碎甲 |
| 残血狂暴 | 8 帧下蹲/起身/扬扇展尾/收势，6 帧血焰凝聚/展开/余焰 | 35% 气血立即获得原有攻速与攻击加成；动作 0.80 秒，之后只留低强度尾焰 |

狐火从同一瞬间结算三次伤害改为分时结算。每段保留 32% 攻击、42% 防御计算、独立闪避、护盾吸收、反震与身法收益；开场及各阶段冷却未改。普通自动攻击继续结算，不会把技能动作切回普攻。

阶段规则仍立即生效，但动作在当前技能之后顺序播放。一次跨过两个阈值不会丢失妖甲或狂暴动作。死亡停止未结算的狐火，取消/重开清空动作队列、命中与状态特效。动作时钟使用游戏秒，不受战斗倍率加速，暂停时冻结。

## 资源生产与绑定

使用内置 `image_gen`。完整提示词和选定原始文件映射分别存于：

- `ArtSource/Raw/Characters/Bosses/FoxDemon/Skills_20260906/prompts.json`
- `ArtSource/Raw/Characters/Bosses/FoxDemon/Skills_20260906/sources.json`

角色整条生成，没有逐帧独立生成。生成器初版出现棋盘格、缺帧或相邻角色接触；重新生成后，选定三条母版均能检测出 8 个独立完整角色。以绿色母版抠出角色 alpha；特效保留生成器原有 alpha。

`Tools/ArtPipeline/prepare_fox_boss_skills.py` 负责可重复的提取、统一缩放、脚底对齐、PNG 打包及 8/12 FPS GIF、1× 横向预览和 4× 最近邻检查图。每条只使用一个缩放系数，不逐帧改变身体比例。首尾使用原有待机种子；较高的狂暴姿势通过整条统一缩放和渲染补偿保持站立身高。

原有狐妖图集可见鞋底位于画布 x≈147.5、y=223，偏离 Sprite pivot；本批保持相同可见脚点以避免与既有 Idle/Attack 切换时横跳，导入 pivot 仍为项目规定的 (0.5, 0.125)。渲染补偿同样以这一可见脚点为中心。原有 Idle/Attack 贴图未覆盖。

交付规格：角色 2048×256、8 帧、PPU 160；特效 1536×256、6 帧、PPU 256；每格 256×256、Point、Full Rect、无压缩/无 MipMap、Clamp。

在实际 Unity Editor 执行 `37 MiniGame/Refresh Fox Demon Skill Pack`，切帧并保存 `MainPrototype` 的引用。母版朝右，游戏内 Boss 在右侧，按帧水平翻转面向玩家；投射物根据双方实际位置定向。

无需手动创建 GameObject、挂载脚本或拖拽 Inspector 引用。新增 partial 文件扩展现有组件，不作为额外挂件。刷新菜单只更新狐妖技能资产和引用，保留场景内既有配置。

## 验证

以最终版本的 `portrait.json`、`landscape.json` 为准：实际 MainPrototype Play Mode，固定随机种子、受控属性；不代表自然路线胜率或真机验收。

- [竖屏机器结果](final_boss_skill_pack_2026-09-06/portrait.json)
- [横屏机器结果](final_boss_skill_pack_2026-09-06/landscape.json)
- 覆盖：绑定与切帧、三次独立命中、伤害比例、暂停、阈值、动作排队、普攻不中断动作、破甲、闪避、护盾、首击致死、Boss 起手死亡、取消/重开、自然冷却和普通/洞穴/Boss 三条时间规则。
- 命中时点允许一个 Editor 渲染帧的离散误差；实际观测值保存在报告的 `impactTimes`。
- Unity 脚本编译通过，`Validate Chinese Fonts` 菜单及 `ValidateFromMenu()` 实际调用通过；`git diff --check` 通过。
- 两种方向各通过 27 项检查。竖屏 540×960、横屏 960×540；在低频 Editor 帧率下，命中观测约为 0.50 / 0.70 / 0.90 秒，约 0.05 秒的误差来自帧采样，报告未将其写成精确零误差。
- 结果结算会调用 CancelBattle 清空命中序号，因此死亡测试在结算前记录实际已发生的最大命中数，再检查胜负结果。
- 横竖屏截图检查朝向、脚点、技能动作、护甲轮廓和尾焰；护甲显示在角色前方，避免被白尾遮住。狐妖可见脚点与战斗地面重新对齐，原来的矩形阶段光晕已替换。横屏还按可用舞台高度限制 Boss 大小，为扬扇、尾焰和顶部信息留出空间；攻防与妖甲/异常数值合并为自适应整行，避免文字重叠。

![横屏狐火](final_boss_skill_pack_2026-09-06/landscape_foxfire_flight.png)
![横屏妖甲](final_boss_skill_pack_2026-09-06/landscape_demon_armor.png)
![横屏狂暴](final_boss_skill_pack_2026-09-06/landscape_blood_frenzy.png)

## 运行与复测

1. 打开 `Assets/Scenes/MainPrototype.unity`，进入 Play Mode，开始关卡2；60 秒探索结束后进入狐妖最终战。
2. 也可用 F1 直接进入 Boss，再用现有 70% / 35% 调试入口检查妖甲和狂暴。保留几秒战斗时间可观察三段狐火。
3. 自动复测：编辑状态执行 `37 MiniGame/Validate Final Boss Skill Pack`，按当前 Game View 尺寸运行测试，更新对应方向 JSON 与截图后自动退出。
4. 重新绑定：编辑状态执行 `37 MiniGame/Refresh Fox Demon Skill Pack`。不必重建整个场景。
5. 重新打包素材：使用带 Pillow 与 NumPy 的 Python 运行 `Tools/ArtPipeline/prepare_fox_boss_skills.py`，再执行刷新菜单。

## 本任务文件

修改：`BattleManager.cs`、`BossV2Definitions.cs`、`BattleScreenController.cs`、`PrototypeSceneBuilder.cs`、`MainPrototype.unity`、`docs/gameplay_systems.md`。

新增：

- `Assets/Scripts/Battle/BattleManager.FinalBoss.cs`
- `Assets/Scripts/UI/BattleScreenController.FinalBoss.cs`
- `Assets/Editor/PrototypeSceneBuilder.FinalBoss.cs`
- `Assets/Scripts/Debug/FinalBossSkillPlayModeProbe.cs`（仅编辑器）
- `Assets/Art/Generated/Characters/Bosses/FoxDemon/spr_boss_fox_demon_{foxfire,demon_armor,blood_frenzy}_right_8f_v01.png`
- `Assets/Art/Generated/Effects/FoxDemon/spr_vfx_fox_{foxfire,demon_armor,blood_frenzy}_6f_v01.png`
- 上述 Assets 对应 Unity `.meta`；原图、提示词、映射、归一化工具、预览、验证报告与截图。

工作区原有中期 Boss 等未提交修改保留，不属于本次新增功能。

## 状态与未完成验收

Generated、Normalized、Imported 已完成。实际场景受控运行与横竖屏视觉检查达到 InEngineQA；不标记最终 Approved。

特效起势、维持与消散按战斗状态选帧，角色条带的实际时序以技能调度为准；GIF 的 8/12 FPS 只用于资源预览。

待后续实玩：新三段时间分布下的自然构筑胜率、目标手机性能与触摸、连续动画和音效听感的最终人工验收。本次没有生成新音频，复用已有技能/命中反馈。
