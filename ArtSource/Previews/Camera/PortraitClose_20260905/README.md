# 竖屏近景镜头调整

2026-09-05，针对用户反馈“离主角视角太远”。

- 正式主地图竖屏基础偏移：(14, 16.5, -30) → (10.5, 12.4, -22.5)，约缩短 25%。
- 竖屏 FOV：45° → 40°。同一出生位置、同一角色模型边界的视口投影高度 0.094309 → 0.144770，约 1.535 倍。
- 不改变角色模型/碰撞、移动速度、UI 大小、横屏、教学关或独立战斗画面。初始 VisionScale=0.74 和望气扩视机制不变。
- 更新 CameraFollow 默认值、MainPrototype 场景序列化字段、PrototypeSceneBuilder 的三处配置入口；同步 docs/gameplay_systems.md。

`before.png` / `after.png` 是 Unity Play Mode 540×960 实际截图；after 在重新载入已保存参数后复查。素材并未通过图像编辑放大角色。

验证：Unity Console 无 Error，git diff --check 通过。调试辅助运行计时检查：普通战斗 58 → 56.23615；洞穴战斗 56.23615 → 56.23615 且仍在战斗；最终 Boss 主时间 0、独立用时增加到 1.600037。调试状态未保存到场景。

无需手动绑定。打开 MainPrototype、Game View 选择 Portrait 540×960，Play 后选择起手武学即可看近景。已退出 Play Mode。

待用户确认镜头远近手感；本次未做手机真机的长距离跑图与遮挡验收。
