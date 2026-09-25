# Web 手机内存优化实施与验证

日期：2026-09-10；Unity 6000.5.4f1。针对 iPhone 13 Pro Safari 首次进入关卡时页面异常重载，实施原方案第一、二批。内存峰值仍是待真机日志确认的原因，不能把下述编辑器结果当成 Safari 整页占用或真机修复证明。

## 已实施

- 新增 `BootMenu.unity`，构建首场景从完整第二关改为轻量首页。复用原首页、选关、序章、设置和开屏逻辑，不带地图、怪物和战斗图集引用；首页不预载主角攻击动画、Boss 转场音效和战斗时间提醒音。
- `LevelLoadingScreen` 保留至少五秒的展示，先卸旧场景，再 `Resources.UnloadUnusedAssets`，之后加载目标。空过渡场景和常驻遮罩承接切换；场景激活不再人为挂起。返回首页释放主角共享动画缓存，切换清理武学图标缓存。
- 306 张目标 PNG 添加持久 WebGL 导入档：角色与通用图 ASTC 6×6，背景 ASTC 8×8。背景上限 1024，小型世界物件和 Logo 上限 512，图标上限 256；多帧切片保留原始最大尺寸、帧布局和 pivot。PNG 原图与其他平台导入配置保留。Read/Write 原本关闭，未将其计作新增收益；地表 mip 保留。
- `WebMobileTexturePostprocessor` 依据 `ProjectSettings/WebMobileTexturePolicy.json` 保持导入策略，后续生成素材或重导入也会应用。现有场景生产工具不会把目标纹理改回未压缩。
- 清理第三关未使用的第二关中期 Boss 音乐序列化引用，并在 `BambooValleyLevelBuilder` 中保持此清理；教学中该音乐原本已经为空，其余运行中可用的战斗/洞穴引用保留。
- `WebMemoryDiagnostics` 在 Editor / Development Build 的切换阶段记录 Unity 分配/预留和已加载纹理估计；正式包不扫描全部纹理。浏览器音频、GPU 驱动、宿主页开销不包含在该统计内。

## Unity Web 配置

配置文件：`Assets/Settings/Build Profiles/Web Mobile.asset`。已设为活动构建档，使用全局场景列表；Player Settings 覆盖保存在此 Profile 中，勿仅检查全局 PlayerSettings 得出未生效结论。

| 项目 | 原值 → 当前值 | 目的与边界 |
|---|---|---|
| 第一场景 | MainPrototype → BootMenu | 首页不实例化完整关卡；内置全部关卡仍在启动 data 包中 |
| 纹理子目标 | Generic → ASTC | 面向支持 ASTC 的移动设备；不支持该扩展的浏览器可能回退解压，收益须按设备验证 |
| Web 默认画质 | High → Low | 降低渲染成本；原生平台默认档不变 |
| 移动 DPR | 原生值 → 上限 1.5 | 降低实际画布像素；桌面上限 2，CSS 布局保持 |
| Web 运行时 | 手机 30 / 桌面 60 FPS，AA 关闭，阴影距离至多 20、无 cascades | 真正采用的帧率仍取决于浏览器调度 |
| Initial Memory | 32 → 64 MiB | 减少极小初始堆的早期扩容；64 不是测出的最佳值，须结合真机峰值再调 |
| Growth | Geometric 20% / cap 96 → 10% / cap 32 MiB | 缩小扩容余量，可能增加扩容次数 |
| Maximum Memory | 保留 2048 MiB | 是增长上限，不是启动分配量；不是防止 Safari 重载的保证 |
| Code Optimization | Disk Size with LTO | 缩小代码体积，构建更慢 |
| Development / Profiler / Debugging | 关闭 | 发布包不带探针对象、Profiler 连接或深度分析 |
| Compression / Fallback | 保留 Gzip / 开启 | Unity Play 响应头由平台控制；自托管正确配置 Content-Encoding 后可另评估关闭 fallback |
| Data Caching / Hash filename | 开启 | 改善重复访问缓存；不等于释放运行内存 |
| Threads / Symbols | 关闭线程 / 外置符号 | 避免新增跨源隔离要求，保留符号用于诊断 |
| WebAssembly 2023 | 保留开启 | 更旧 Safari 的兼容性需另做构建验证 |

此包是移动 ASTC 档，不宣称已覆盖所有桌面 GPU。如果要同时服务不支持 ASTC 的终端，应增加经验证的 ETC2/桌面构建与格式选择；本轮未引入多包路由。

## 实测证据

- `texture_baseline.json` / `texture_optimized.json`：MainPrototype 的 92 张递归依赖 PNG，Unity 编辑器已导入纹理资源统计从 **161,568,357 bytes（154.08 MiB）降至 33,144,241 bytes（31.61 MiB）**，约减少 **79.5%**。不是整关总内存、也不是 iPhone 峰值。
- 纹理格式从 RGBA32 82 / DXT1 9 / RGB24 1，变为 ASTC 6×6 77 / ASTC 8×8 7 / RGBA32 8。保留的 RGBA32 是本轮目标目录以外的小型第三方素材。
- `build_settings.json`：BootMenu 递归静态依赖仅 6 项，PNG/WAV/FBX 均为 0；运行时仍会加载首页图、字体、UI 和菜单音乐，不能称为零资源首页。
- `playmode.json`：**60 项检查通过，9 次切换**，覆盖开屏、首页、锁定关卡、选关、完整序章、教学、暂停后跳过、第二关起手选择、第三关、返回首页和重复切关。检查了单调进度、100% 显示、至少五秒、旧关资源清理先于新关、最终仅一个目标场景。
- 普通战斗主倒计时继续、洞穴暂停、Boss 独立计时均通过 Play Mode 验证；加载页和起手选择不偷跑时间。
- `37 MiniGame/Validate Chinese Fonts` 成功，965 个字形通过；构建入口再次执行字体检查。
- `build_result.txt`：最终 Release 构建成功，0 errors / 0 warnings；17.73 秒为缓存已热的最后一次增量构建耗时，不代表首次完整构建速度。
- `package.json`：data gzip **96.15 MiB**，解压 **127.41 MiB**；全部发布文件 **102.49 MiB**。历史旧包记录为 data gzip 126.92 / 解压 165.09 MiB，约减少 24% / 23%；历史比较不是同提交严格 A/B，不能据此推算运行内存。
- 桌面内置浏览器本地 HTTP 测试：540×960 首页→选关→第二关情报→起手武学→实际地图；960×540 设置→返回首页正常，所读取的浏览器 error/warn 列表为空。该浏览器不是 iOS Safari；尚未取得设备 ASTC 扩展列表，画面正常不能独立证明没有纹理解压回退。
- `Builds/WebMobile.zip` 已生成，ZIP 校验通过。最终包 hash 和逐文件字节数见 `package.json`；本轮未替换线上 Unity Play。

## 使用方法与待验收

1. Unity 停止 Play Mode，选择 `37 MiniGame/Web Mobile/Apply Mobile Configuration` 可重应用配置。已经生成场景和引用，无需手动 Inspector 绑定。
2. 选择 `37 MiniGame/Web Mobile/Build Release`，输出到 `Builds/WebMobile`。从 BootMenu 进入 Play Mode 可走完整入口；三个关卡仍可独立调试。
3. 将发布目录内的 `index.html`、`Build`、`TemplateData` 等完整打包上传 Unity Play；不要只替换 data 文件。
4. iPhone 13 Pro Safari 冷/热启动各 5 次，并连续测试教学→第二关→第三关→首页，检查横竖屏、透明边缘、文字、触摸和音频。采集 Safari Web Inspector 或 WebContent/GPU 终止记录。
5. 若仍出现重载，先做禁加载长音乐的诊断包，再决定音频按阶段加载与关卡分包。两项属于原方案第三、四批，本轮未冒充完成。

参考：[Unity Web 纹理压缩](https://docs.unity3d.com/6000.5/Documentation/Manual/webgl-texture-compression.html)、[Web 内存](https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-memory.html)、[异步激活与队列](https://docs.unity3d.com/6000.5/Documentation/ScriptReference/AsyncOperation-allowSceneActivation.html)。

## 文件范围

新增：`BootMenu.unity`、`Web Mobile.asset`、`WebMobileTexturePolicy.json`、`WebMobileBuild.cs`、`WebMobileRuntime.cs`、`WebMemoryDiagnostics.cs`、仅编辑器使用的 `WebMobilePlayModeProbe.cs`，以及对应 meta 与本目录证据。

修改：`LevelSequence.cs`、`GameFlowController.cs`、`LevelLoadingScreen.cs`、`BattleScreenController.HeroAttacks.cs`、`MainMapMusicController.cs`、`HeroDirectionalAnimation.cs`、`HeroAttackArt.cs`、Web 模板、目标 PNG meta、构建/画质设置、`BambooValleyLevel.unity`、`BambooValleyLevelBuilder.cs` 及技术/玩法文档。仓库原有其他未提交修改保留，不属于本次优化交付。
