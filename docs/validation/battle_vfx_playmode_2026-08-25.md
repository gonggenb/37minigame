# 战斗 VFX Play Mode 验证（2026-08-25）

## 已验证

- 新图集均为 `1536 × 256`、6 帧、透明背景，并以 Point / 256 PPU / 无 Mipmap / 无压缩导入。
- `MainPrototype` 的 `BattleScreenController` 成功绑定：通用命中 6 帧、剑气 6 帧、毒雾 6 帧。
- 竖屏 Play Mode 可见剑气、紫绿中毒染色、毒雾循环和破甲爆点：
  `Assets/Screenshots/VFX/battle_vfx_sword_poison_portrait_v01.png`。
- VFX 事件探针：
  - 剑毒：`BasicHit, SwordQi, PoisonApplied, ArmorBreak`。
  - 铁壁：`BasicHit, ShieldImpact, Retaliation`。
  - 残影：`Dodge, ShadowDodge`。
  - 毒发：`PoisonTick, PoisonMist, ArmorBreak, Heal`。
  - 血域：`BasicHit, CriticalHit, Heal, BloodPower, BloodBurst`。
- WebGL Development Build 成功：`Temp/CodexValidation/battle-vfx-webgl`，`75.04 MB`，`19.0s`，0 error；构建工具记录的 2 条 warning 均为“Build succeeded”状态日志，不是编译或资源警告。
- 中文字体覆盖与 4 组 UI Safe Area 校验通过。

## 三条时间规则回归

1. 普通战斗：主时间由 `60.000` 降至 `58.268`，战斗保持进行。
2. 洞穴战斗：主时间在 `50.000` 保持不变，战斗保持进行。
3. Boss 战：主时间保持 `0.000`，Boss 独立时间由 `0.000` 增至 `1.673`。

## 验证边界

- 当前为 `InEngineQA`，不是 `Approved`。
- 竖屏已保存游戏内截图；横屏采用同一逻辑坐标和按角色尺寸缩放的 VFX Rect，并通过既有 Safe Area 校验，仍需目标手机横竖屏连续试玩确认亮度、重叠和触摸观感。
- AI 母版经过黑底转透明、像素化和统一 6 帧归一化；未直接把概念图作为 Unity 交付资源。
