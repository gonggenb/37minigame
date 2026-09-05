# 妖狐阶段音乐差异增强

用户试听发现 70% / 35% 血量变化时音乐差异不够明显。旧版主音源增益为 0.38，加层共用普通战斗的 0.20 增益，加层素材 RMS 本身又比主曲低约 1.5 dB，合成后加层约低于主曲 7 dB；音色也以相似的拨弦与鼓点为主。

## 本次调整

| 阶段 | 主要听感变化 | 主曲增益 | 阶段层增益 |
| --- | --- | --- | --- |
| 100%–70% | 原有笛声、拨弦主题 | 0.38 | 0 |
| 70%–35% | 低沉弓弦、低音簧管质感、半拍重鼓成为主导 | 0.2584 | 0.50 |
| 35% 以下 | 高音号角主旋律、十六分音符快拨与滚鼓成为主导 | 0.1596 | 0.62 |

70% 与 35% 分别新增 1.2 秒非旋律性锣鼓重音，仅阶段首次进入时触发。单次大伤害跨过两个阈值时，战斗仍依次处理原有阶段；音乐省略临时妖甲阶段的重音，只播放狂暴重音。较快连续换阶段使用同一音源替换重音，不叠加播放。

主曲仍为 160 BPM，阶段层保持 12 秒 / 8 小节的同一和声周期，未使用提高播放速度或改变音高来制造狂暴。普通战斗与洞穴仍使用原来的 0.20 加层音量。

## 文件和接入

- 修改 `Assets/Scripts/Audio/MainMapMusicController.cs`：独立 Boss 阶段混音参数、主曲让位、转阶段单次重音、跨阈值重音处理；重音通过 Resources 自动加载。
- 修改 `Tools/Audio/generate_boss_duet.py`：重编两个阶段层、生成转阶段重音及 36 秒对比试听；`--phases-only` 仅更新本次相关素材。
- 更新 `Assets/Audio/Generated/Music/stem_boss_fox_demon_moonfire_armor_12s_v04.wav` 和 `stem_boss_fox_demon_moonfire_frenzy_12s_v04.wav` 的内容，保持名字与 GUID，沿用场景现有绑定。
- 新增 `Assets/Resources/Audio/BossTransitions/stg_fox_armor_transition_v01.wav`、`stg_fox_frenzy_transition_v01.wav` 与 Unity 自动生成的 `.meta`。
- 新增本说明、`boss_phase_mix_2026-09-06.json`、`boss_phase_mix_playmode_2026-09-06.json`、`boss_phase_mix_preview.json`、`boss_phase_mix_preview_36s.wav`。

当前 Editor 场景存在未保存编辑，本次没有保存、重建或重新绑定场景。无需手动创建 GameObject、挂脚本、拖入 AudioClip 或 Prefab。脚本新增参数使用初始化默认值；已在 Editor 和 Play Mode 核对实际值。

## 试听与复测

`boss_phase_mix_preview_36s.wav` 为离线混音对比：0–12 秒开场，12–24 秒妖甲，24–36 秒狂暴。为公平比较，三段使用相同的主曲前 12 秒；不含战斗音效，不是 Unity 实录。稳态 RMS 约为 -25.3 / -19.2 / -17.3 dBFS，含转场提示的峰值约 -4.66 dBFS，无削波。

打开 `Assets/Scenes/MainPrototype.unity` 并进入 Play Mode。开始关卡2，通过 F1 进入最终 Boss；入场提示结束后分别点击 `Boss 至 70%`、`Boss 至 35%`，每段至少听 5 秒。重新开始一局后可直接点 35%，核对仅有一次狂暴重音。

生成命令：`python3 Tools/Audio/generate_boss_duet.py --phases-only`，依赖 NumPy。

## 验证边界

实际 MainPrototype Play Mode 检查了混音值、阶段状态、重音计数、跨阈值与重开恢复，详见 JSON。首次重开检查因工具调用间隔超过 30 秒，截图式状态采样时已自然进入中期 Boss；即时检查通过。跨双阈值初测发现两次重音，已据此增加省略过渡妖甲重音的处理并复测。保留原始失败记录用于说明修复过程。

本次没有修改战斗数值、阶段血量阈值或计时代码。普通战斗继续扣主时间、洞穴暂停、最终 Boss 独立计时的原有分支保持；本次 Play Mode 再次观察最终 Boss 主时间为 0、Boss 时间独立增长。普通战斗和洞穴的完整运行计时证据沿用本日上一轮 BGM 验证。

人工完整试听、战斗音效遮蔽下的听感，以及手机扬声器最终混音验收仍待确认。运行检查不能代替听感批准。未新增运行时中文 UI 文案。
