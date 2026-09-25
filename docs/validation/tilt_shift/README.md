# 轻移轴相机

2026-09-21，Unity 6000.5.4f1，Built-in Render Pipeline。

## 行为

- 保留原有斜俯视、横竖屏 FOV、角色跟随及望气灵物扩视规则。
- `CameraFollow.Awake` 自动补挂 `TiltShiftEffect`，三关现有跟随相机共用，不修改场景文件。
- 默认强度 0.7、中央清晰带半高 0.22、过渡带 0.22、参考 540 短边模糊半径 2.4。
  焦点取跟随目标的视口高度，保持相机移动与扩大视野后的角色区域清晰。
- 只在 `MainMapRunning` 启用。普通战斗、洞穴、中期/最终 Boss、选择页及结算直接禁用图像效果组件。
- 设置页横竖屏共用“移轴景深”开关，保存键 `WuxiaRoguelite.TiltShift.v1`。关闭时不运行模糊 Pass。
- 通过 `ImageEffectOpaque` 在不透明场景后处理，再绘制透明角色、标记、特效及 IMGUI。
  透明水面、透明远景切片也会保留清晰；这是确保当前像素角色轮廓清楚的取舍。
  这是屏幕带状柔焦，不是依赖深度纹理的真实光学景深。
- 两张半分辨率临时纹理、两次分离模糊及一次原图合成。纹理逐帧归还，材质停用时释放。
  Shader 放在 Resources 中，随构建打包；不支持时回退原图并停用效果。
- 不增加 UI 素材或占位资源。沿用现有 UI 主题和 `RuntimeChineseFont`。

## 文件

修改：
- `Assets/Scripts/Camera/CameraFollow.cs`
- `Assets/Scripts/UI/PrototypeHUDController.cs`
- `Assets/Scripts/UI/PortraitHudViews.cs`
- `docs/gameplay_systems.md`

新增（Assets 文件包含 Unity .meta）：
- `Assets/Scripts/Camera/TiltShiftEffect.cs`
- `Assets/Resources/Camera/TiltShift.shader`
- `Assets/Scripts/Debug/TiltShiftPlayModeProbe.cs`
- 本目录说明、验证报告与截图。

## 运行和调节

无需新建 GameObject、Prefab 或手动绑定引用。打开 `MainPrototype`，Play 后开始探索即可。
按 Esc 或设置按钮切换“移轴景深”；移动、返回洞穴、碰怪，观察人物和提示清晰度。
Play Mode 的 Main Camera 上可调 `TiltShiftEffect` 的强度、清晰带、过渡带和半径；
临时 Inspector 修改不会在退出 Play 后保存，长期默认值由脚本统一维护。

## 验证

菜单：`37 MiniGame/Validate Tilt Shift Play Mode`，从 MainPrototype 的非运行状态启动。
测试使用受控遭遇及冻结时刻，生成横屏 960×540、竖屏 540×960 开/关与设置截图；
以真实 Camera.Render 对比中央带与边缘，覆盖 1× / 4× MSAA，检查普通战斗、洞穴、Boss 计时。
测试结束恢复设置偏好、Game View 尺寸、时间倍率和后台运行状态，不保存测试场景。

本轮结果：
- Unity 编译通过；Shader 编译消息为空；最终 Console 0 条 error。
- 已执行 `37 MiniGame/Validate Chinese Fonts`，常规体与粗体覆盖 971 个所需非 ASCII 字形。
- `playmode_report.json`：18 项检查通过，0 运行时错误，当前验证设备为 macOS Metal。
- 960×540 中央带平均开关差值 0.0018/255、边缘 5.7715/255；540×960 中央 0.0032/255、边缘 4.1835/255。
  1× 与 4× MSAA 的两组渲染均通过：中间保持原图，边缘确有柔焦变化。
- 已查看两种方向的开/关及设置截图：新增开关无重叠，UI 未被虚化。
- 普通战斗消耗主时间、实际洞穴战斗暂停主时间、返回地图恢复效果、最终 Boss 独立计时通过。
- 测试结束后已退出 Play Mode，活动场景仍为 MainPrototype，场景无未保存改动；移轴偏好恢复原值。
- 截图来自受控冻结画面，角色属性是现有测试配置，不是平衡或自然路线样本。

截图：`landscape_off.png`、`landscape_on.png`、`portrait_off.png`、`portrait_on.png`、
`landscape_settings.png`、`portrait_settings.png`。

发布前仍需手机 WebGL 的 GPU 帧耗时、动态转向、刘海安全区和长期视觉舒适度验收。
Editor 通过不代表真机性能或最终美术批准。

API 依据：[Unity OnRenderImage 文档](https://docs.unity3d.com/cn/6000.0/ScriptReference/Camera.OnRenderImage.html)。
