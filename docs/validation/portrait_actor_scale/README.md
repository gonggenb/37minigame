# 竖屏战斗角色尺寸调整

2026-09-10，Unity 6000.5.4f1，MainPrototype 实际 Play Mode。

- 修改 `Assets/Scripts/UI/BattleScreenController.cs`：竖屏玩家与敌人的绘制尺寸乘以 1.25；保持原有战斗地面锚点，Boss 额外受顶部可用空间限制。
- 横屏走原有尺寸计算；地图角色、战斗数值、攻击节奏与三条时间规则未修改。
- 无新增运行时组件、中文文案或场景绑定。无需手动绑定。

## 实际画面检查

- 540×960：新怪铁獠山猪、旧怪竹傀儡、岩甲山魈精英、最终九尾妖狐均进入真实战斗并截图。
- 960×540：精英战斗横屏回归，角色尺寸分支未改变。
- 所检查竖屏截图中角色无屏幕边缘裁切，未覆盖顶部血条或底部战报；攻击和受击反馈正常显示。
- Unity 编译通过，检查 Console 得到 0 条 error。检查后退出 Play Mode，恢复原 Game View 尺寸与后台运行设置。
- 截图使用临时高血量及延长倒计时，以便持续观察攻击动画；这些测试数据未保存进场景。

截图：`boar_portrait.png`、`puppet_portrait.png`、`elite_portrait.png`、`boss_portrait.png`、`elite_landscape.png`。

## 试玩

打开 MainPrototype，Game View 选择 9:16（例如 540×960），进入第二关并碰怪；切回横屏可对照。当前完成的是编辑器画面检查，手机真机上的大小感受仍需试玩确认。
