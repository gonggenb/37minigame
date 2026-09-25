# 战斗与构筑信息优化

2026-09-10，Unity 6000.5.4f1。范围为护盾/阴影、武学收益提示、第二关小怪特性反馈。

## 实现

- 矩形护盾改为贴身淡金气劲，护盾受击时短暂增强；脚下横条改为柔和椭圆阴影。
  两张 96×96 程序纹理只创建一次，并在组件销毁时释放，无额外资源引用。
- 新增六种小怪使用与角色图集一致的底部 1/8 脚点补偿；保留原有横竖屏尺寸策略和 Boss 上限。
- 横竖屏武学卡片共用只读解释器，显示本次收益、已有武学配合与选后秘传差距。
  金钟罩按当前防御计算护盾增量；攻速、吸血、闪避展示当前值到选择后的值。
  铁布衫展示即时气血与防御增益。其他武学使用目录中的下一重效果。
- 秘传提示读取当前双流派总重数，对照实际 2+2 / 4+4 门槛；达到门槛时直接预告解锁或升重。
  只显示一组最接近的关联秘传，不代表唯一组合。预览不调用 ApplyMartialArt，不消耗奖励。
- 重撞在首击冷却最后 0.55 战斗秒内收势并显示蓄力提示，出手后提示消退；不延迟真实攻击。
- 毒咬仅在命中并真实施毒时提示，玩家身旁显示余毒次数及毒发气劲。
- 机关护甲显示护体状态，真实耗尽时触发碎片与破甲提示；再次受伤不重复破甲。
  特性事件计数在取消/新战斗时清空，Boss 不使用普通怪特性反馈。

## 文件

新增：
- Assets/Scripts/UI/BattleScreenController.CombatReadability.cs（及 Unity .meta）
- Assets/Scripts/UI/MartialArtChoiceInsight.cs（及 Unity .meta）
- 本目录验证记录与截图。

修改：
- Assets/Scripts/UI/BattleScreenController.cs：接入脚点、阴影、护盾和特性绘制。
- Assets/Scripts/UI/PrototypeHUDController.cs、PortraitHudViews.cs：共享收益提示与卡片布局。
- Assets/Scripts/Battle/BattleManager.EnemyTraits.cs、BattleManager.cs：发布施毒、毒发、护甲耗尽的计数；伤害公式不变。
- docs/gameplay_systems.md：补充当前行为。

没有保存测试夹具到场景。无需创建 GameObject、挂载脚本或手动绑定 Inspector。

## 验证结果

- Unity 编译通过，最终 Console 查询 0 条 error。
- 已执行 `37 MiniGame/Validate Chinese Fonts`，随包常规体与粗体校验通过，无需替换字体。
- 15 项有断言的 Play Mode 检查通过，见 logic_checks.txt。覆盖预览不修改构筑、秘传一/二重预告与实际解锁一致、护盾增量等于实际增量、满重、毒咬闪避/命中、暂停毒发、换场清理、护甲恰好归零和重复受伤。
- 复跑现有 `37 MiniGame/Validate Routes Traits and Review Play Mode`：46 项通过，0 运行时错误。
  本轮完整输出保存于 regression/；原任务的历史输出已恢复保留。
- 三条时间规则均在本次回归通过：普通战斗主时间继续、洞穴暂停主时间、最终 Boss 独立计时。
- 540×960 检查选择页、护盾/重撞、毒咬、机关破甲和最终 Boss；960×540 检查选择页与普通战斗。
  横屏秘传进度直接呈现在卡片内，无需悬停；竖屏保留选择后确认及列表滚动。
- 截图包含临时高血量及停住战斗协程的可视化夹具，仅用于布局和特性状态检查，不是平衡样本。
- 完成后退出 Play Mode，恢复原 Game View 尺寸和后台运行设置。手机触摸、安全区及性能仍需真机验收。

## 试玩

打开 MainPrototype，进入第二关。查看起手/升级卡片的本次收益及秘传差距；领悟金钟罩后碰怪查看护体气劲。
寻找山猪观察首击蓄力、蛇妖观察毒咬与余毒、机关怪观察护甲与破甲。切换 9:16 / 16:9 检查两种布局。

截图：choices_portrait.png、choices_landscape.png、heavy_windup_portrait.png、venom_bite_portrait.png、armor_depleted_portrait.png、boss_portrait.png。
