# 手机浏览器竖屏计时器出框修复

## 修改

`Assets/Scripts/UI/PortraitUiLayout.cs` 中的共享 `WuxiaUiComponents.Timer` 原先在已有 GUI 缩放矩阵上调用 `GUIUtility.RotateAroundPivot` 绘制 60 根刻度。高分辨率缩放时，刻度的旋转中心与表盘逻辑坐标不一致；修复前的 Editor 截图可见刻度偏移到表盘内部，而非沿圆周排列。

现在使用 `parent * Translate(center) * Rotate(angle) * Translate(-center)`，先在表盘逻辑坐标内旋转，再统一应用上层缩放和偏移。绘制结束通过 `finally` 恢复原矩阵。探索与普通/洞穴战斗的竖屏表盘共用修复，不修改 UI 风格、数字格式或计时逻辑。

修改文件只有 `PortraitUiLayout.cs`；新增本验证记录。无需 GameObject、组件、Inspector 或 Prefab 手动绑定。

## 验证

- Unity 6000.5.4f1 编译通过，无 Console error；`git diff --check` 通过。
- 在 Editor 调用实际 `TimerTickMatrix`：6 个缩放倍率（0.7 / 1 / 1.44375 / 2 / 2.183333 / 3）、3 个中心、96/102 两种直径、60 根刻度，验证 8,640 个角点都位于表盘内部且旋转中心不漂移；包含平移后的父矩阵。
- 既有 4 组横竖屏/刘海 Safe Area 校验通过。
- 非 Development WebGL 构建成功：`Builds/WebGLTimerFix`，75.01 MB，35.04 秒，0 error、3 warning。构建沿用项目中文字体前置校验。工具链提示为 Firefox 旧版本 WebGPU 及 JS stackTrace 弃用警告。
- 实际通过 localhost 加载上述构建，在浏览器 390×844 竖屏完成首页、关卡选择、序章、教学与关卡2；30/60 秒数字和刻度都在表盘内。
- 切至 1080×1920 实际 Canvas（逻辑 UI 缩放 2 倍），观察关卡2剩余 49 秒时，数字居中、剩余刻度沿表盘内圈排列，无偏移出框。浏览器报告 DPR=1；这是高分辨率画布验证，不冒充物理高 DPR 手机实测。
- 切至 960×540 横屏，进入真实中期 Boss 战，计时标题正常，浏览器 error 日志为空。
- Editor 测试结束退出 Play Mode，恢复测试前 Game View 选择与后台运行设置，未保存场景。

三条时间规则的代码检查：普通地图/普通战斗仍递减主时间，洞穴阶段不递减主时间，最终 Boss 仍独立累加 `bossBattleTime`。本轮只修改 UI 绘制，没有重新执行洞穴和最终 Boss 的完整计时实玩回归。

## 运行与未完成

部署 `Builds/WebGLTimerFix` 的全部内容（index.html 位于发布目录根部），或使用 `Builds/WebGLTimerFix.zip`。本地可用 HTTP 服务打开，不能双击 file:// 运行 WebGL。替换旧部署后重新加载页面再验证。

尚未在用户实际手机、Safari/微信内置浏览器或原线上部署复验；未发布到外部网站。浏览器截图检查与数学边界校验不代替这些设备验证。
