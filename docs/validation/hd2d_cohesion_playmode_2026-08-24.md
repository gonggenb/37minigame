# HD-2D 融合与横竖屏 Play Mode 验证（2026-08-24）

## 变更范围

- 3D 场景实体统一为低反射、低饱和、低对比的哑光材质，降低像素角色与写实高光之间的割裂。
- 2D 风景切片统一加入降对比与雾色融合，继续保留真实前、中、后景深度。
- 主角、敌人和 Boss 改为镜头平面朝向，并增加跟随桥面视觉抬升的椭圆接触阴影。
- 响应式镜头使用横屏偏移 `(7.8, 7.2, -14)` / `34°`，竖屏偏移 `(9.4, 10.2, -18)` / `40°`，共同使用 `0.72` 的注视高度。
- 新增菜单 `37 MiniGame > Apply HD-2D Cohesion Pass`，可在主场景中重复应用融合配置。

## Unity Editor 证据

- `MainPrototype` 成功应用融合流程：249 个 MeshRenderer 使用统一风格材质，51 个战斗角色配置接触阴影，76 个风景 Sprite 配置空气透视材质。
- 横屏截图：`Assets/Screenshots/HD2D/hd2d_cohesion_landscape_v01.png`。
- 竖屏截图：`Assets/Screenshots/HD2D/hd2d_cohesion_portrait_v01.png`。
- 桥面截图：`Assets/Screenshots/HD2D/hd2d_cohesion_bridge_landscape_v01.png`，确认角色与接触阴影会跟随桥面视觉高度。
- Unity 全量脚本编译为 0 error / 0 warning。
- WebGL Development Build 输出到 `Temp/CodexValidation/hd2d-cohesion-webgl`，构建成功。
- `37 MiniGame > Validate Chinese Fonts` 通过：常规体与粗体覆盖 774 个非 ASCII 字形。
- `37 MiniGame > Validate UI Safe Areas` 通过：4 组横竖屏与异形屏安全区均位于逻辑画布内，并保留 44 × 44 触摸空间。

## 三条时间规则 Play Mode 回归

1. 普通战斗：主地图时间从 `60` 下降到 `47.943`，战斗仍在进行，确认倒计时继续流逝。
2. 隐藏洞穴：进入 `CaveRunning` 后主地图时间持续为 `60`，确认倒计时暂停。
3. 最终 Boss：主地图时间保持 `0`，Boss 独立计时从 `0` 增长到 `7.699`，确认不受主地图 60 秒限制。

## 验证边界

- 当前状态是 `InEngineQA`：已经有 Unity Play Mode、横竖屏截图、规则回归和 WebGL 构建证据。
- 尚未在目标手机上完成旋转、刘海/圆角、触摸操作、性能和长时间连续试玩，因此不能标记为 `Approved`。
- 桥附近的前景树在个别镜头位置会遮住屏幕下方一小块区域，未遮挡玩家主体；后续真机试玩时继续观察是否需要局部移位。
