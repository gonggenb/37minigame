# 角色透明边缘修复 · 2026-09-21

用户确认采用脚本逐像素清理，保持原图。最终交付未使用生成式试修图。

## 修改范围

- `Assets/Resources/Characters/HeroAttacks/`：12 张主角攻击/绝学图集。
- `Assets/Resources/Characters/HeroEightDirections/`：8 张待机单帧、8 张跑步图集。
- `Assets/Resources/OpeningDialogue/portrait_hero_v01.png`、`portrait_fox_v01.png`，及对应 `ArtSource/Normalized/OpeningDialogue/` 母版。
- 共 30 张运行时 PNG、168 个动画帧。每文件路径、前后 SHA-256、改动像素数见 `pixel_report.json`。
- 新增 `Tools/ArtPipeline/clean_character_alpha_edges.py`、原图备份 `ArtSource/Raw/AlphaCleanup/`、归一化输出 `ArtSource/Normalized/AlphaCleanup/` 和本目录验证资料。
- `.meta`、场景、Prefab、材质、玩法及运行时 C# 均未修改。不需要创建 GameObject、挂脚本或 Inspector 绑定。

## 处理方式

1. 主角帧动画只修正暗色发丝附近的小块浅灰边缘，保留原 Alpha；不按颜色全局抠除白色，避免误删白衣、眼睛、剑刃和技能光效。
2. 基础普攻第六帧的剑气内部有明确棋盘格残底。用坐标种子和面积/边界断言锁定该块，清除 1,317 个残底像素，保留青色剑气轮廓。
3. 主角立绘在已检查的发丝和蓝色肩部轮廓区域使用附近原画颜色去浅色边；狐妖使用更窄、更保守的边缘范围，保留白毛。
4. 两张立绘 Alpha 完全不变；其余动画除指定棋盘块外 Alpha 完全不变。未进行重绘、缩放、重新排列或裁切。
5. 检查了 66 张角色 PNG 的深色底接触表 `audit_before.jpg`；普通怪与 Boss 未发现同样明确的棋盘块，未套用主角清理规则。白毛、刀刃等亮色不视为缺陷。

## 验证结果及边界

- 静态检查通过：30 张画布尺寸不变，168 帧可见范围不变，所有修改均在边缘或指定残底区域。
- 30 个 `.meta` 文件逐字节一致，GUID、Sprite ID、切片、脚点、PPU、导入设置保持原值。
- 重复运行 `--apply` 后 PNG 哈希和修改时间均不变，不会累积腐蚀边缘。原图和输出哈希都存于备份清单；检测到未知版本时停止，要求先复核新图。
- Unity 6000.5.4f1 重新导入成功；实际加载到 28 张角色资源的 168 个 Sprite，全部为 256×256、Pivot `(128,32)`、160 PPU、Point 过滤。
- Editor Play Mode 开场对话与基础普攻分别验证 `960×540`、`540×960`，见四张 `opening_*` / `attack_*` 截图。普攻为固定第六帧的受控战斗夹具，测试属性仅在 Play Mode 中生效。
- 普通战斗主时间继续、洞穴战斗主时间暂停、Boss 独立计时均通过，见 `playmode_report.json`；控制台错误为 0。退出 Play Mode 后恢复 Game View 尺寸和后台运行设置，场景未变脏。
- `before_after_8fps.gif` / `before_after_12fps.gif` 为基础普攻和左向跑步的逐帧对照；编号 PNG 同时提供深色、浅色背景对照。
- 状态：所示开场与普攻场景已完成 `InEngineQA`；其他修过的动作完成像素检查及导入验证。未逐个技能完成实际战斗视觉验收，未重新构建 WebGL、未进行手机真机验收，未标记 `Approved`。

## 重现与查看

从项目根目录执行：

```sh
uv run --with pillow --with numpy --with scipy python Tools/ArtPipeline/clean_character_alpha_edges.py
uv run --with pillow --with numpy --with scipy python Tools/ArtPipeline/clean_character_alpha_edges.py --apply
```

第一条只生成对照与候选图；第二条才更新运行资源与立绘归一化母版。
重新执行旧角色/立绘生成脚本后，需要再执行这一步。若源图内容已改变，不能直接复用旧基线；应重新审查对应清理区域。

Unity 直接打开 `Assets/Scenes/MainPrototype.unity` 进入 Play Mode，查看开场立绘和战斗角色；也可从 `BootMenu` 选择教学关查看序章。现有基础普攻通常随机选用三个变体，原基础第六帧在本次受控夹具中单独验证。

回退单张素材时，从 `ArtSource/Raw/AlphaCleanup/` 下同相对路径复制原 PNG 回去即可，保留 `.meta`；立绘同时恢复其归一化母版。
