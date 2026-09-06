# 下一关衔接与五秒加载页

## 行为与修复边界

第一关通关总结点击“下一关”，以及教学 HUD 点击“跳过”，现在直接显示加载页；“难度飙升！！！”合入加载页，取消需要再次点击的中间提示。加载完成自动进入 MainPrototype 的三选一起手武学，确认武学后才开始 60 秒探索。

原代码在教学衔接时先进入 Ready，再等待难度弹窗确认。当前 checkout 的 Editor 中，调用旧流程并确认弹窗后能正常进入关卡2，本轮未复现用户所述的旧版本返回首页现象，不能据此声称已确定原线上故障根因。本次直接移除这段首页共用的 Ready 中间步骤，并以真实下一关点击和场景回归验证新流程。

LevelSequence 将同步 LoadScene 改为共享异步加载，自动开局意图只在当前运行期间保留并由目标场景 Start 消费一次，不再依赖 PlayerPrefs 临时跳转键；教学完成解锁仍持久保存。加载请求期间重复下一关、主页或其他场景请求均不覆盖目标。

LevelLoadingScreen 自动创建、跨场景保留，复用首页山景、共享九宫格、主题色和 RuntimeChineseFont。加载页至少显示 5 个真实秒，结合场景读取进度显示单调递增的进度条与百分比；完成场景激活及 Start 初始化后显示 100% 再关闭。加载期间冻结玩法时间、隐藏底层 HUD/战斗界面并屏蔽输入。未新增长期占位美术，主题资源缺失时仍沿用既有 PLACEHOLDER_UI 回退。

适用范围：序章后载入教学、关卡选择载入关卡2、教学下一关/跳过、教学返回主页的跨场景切换。同场景返回主页保持即时，首次浏览器下载 Unity 引擎仍使用 WebGL 原有启动加载页。

## 本次文件

修改：
- Assets/Scripts/GameFlow/LevelSequence.cs
- Assets/Scripts/GameFlow/GameFlowController.cs（仅增加加载守卫和改写教学衔接；保留工作区既有剧情改动）
- Assets/Scripts/UI/PrototypeHUDController.cs
- Assets/Scripts/UI/BattleScreenController.cs（仅增加加载期间的 Update/OnGUI 守卫；保留工作区既有剧情改动）
- Assets/Scripts/UI/MobileInputController.cs
- docs/project_core.md、docs/gameplay_systems.md、docs/unity_tech.md

新增：
- Assets/Scripts/UI/LevelLoadingScreen.cs 与 .meta
- Assets/Scripts/Debug/LevelTransitionPlayModeProbe.cs 与 .meta（仅 UNITY_EDITOR，运行真实场景切换的回归探针）
- 本记录、level_loading_2026-09-06.json、level_loading_images/ 四张截图

无新增场景、Prefab、GameObject 或 Inspector 手动绑定；加载器会自动创建运行时 GameObject。

## 验证

- Unity 6000.5.4f1 编译成功，最终 Console 无 error/warning。
- 执行 37 MiniGame/Validate Chinese Fonts：893 个非 ASCII 字形通过，常规/粗体覆盖完整，无需更新字体子集。
- 既有 37 MiniGame/Validate UI Safe Areas 四组校验通过。
- LevelTransitionPlayModeProbe 在实际 MainPrototype/TutorialLevel 场景完成 31 项断言，记录于同名 JSON。使用受控教学胜利及战斗配置，不是完整自然路线试玩或平衡验收。
- 6 次实际异步场景切换耗时 5.55–5.71 秒：均检查进度单调递增、出现中间值与 100%、加载期间主时间不变、目标场景正确。
- 覆盖：未解锁关卡2拒绝加载、关卡选择进入关卡2、第一关通关下一关、重复下一关/主页请求、暂停教学后跳过、一次性自动开局标记、明确返回主页、最终关下一关禁用。
- 实测三条时间规则：普通战斗主时间减少、洞穴战斗主时间保持、最终 Boss 独立时间增加且主时间保持。
- 原生 Unity 窗口鼠标实际点击横屏结算页“下一关”：即时出现 9% 加载页，结束后直接显示关卡2起手选择，主时间 60。
- 已检查实际 Play Mode 截图：540×960 竖屏和 960×540 横屏，加载页及到达关卡后的界面清楚，没有文字截断或重叠。截图位于 level_loading_images/。
- 测试结束退出 Play Mode，恢复原 Game View 选择、runInBackground=false 和教学解锁值，未保存场景测试状态。
- git diff --check 通过。

## 运行与复测

打开 Assets/Scenes/MainPrototype.unity，进入 Play Mode；选择第一关、阅读序章、进入教学。通关总结点击下一关，应显示至少五秒的加载页并自动进入第二关武学选择；也可通过教学“跳过”快速复测衔接。加载期间连续点击或按 Esc 不应显示首页或操作底层界面。

自动回归：在 MainPrototype Play Mode 中创建临时对象并挂载 LevelTransitionPlayModeProbe；探针结束写出 JSON 并退出 Play Mode。不要保存此测试对象到场景。

未完成：手机真机触摸、Safari/微信内置浏览器及原线上部署复验；未发布到外部网站。横竖屏 Editor 通过不代表最终设备 Approved。

## WebGL 交付

- 非 Development WebGL 构建成功：Builds/WebGLLevelLoading，83.1 MB，37.41 秒，0 error、2 warning；未绕过中文字体构建前置校验。
- 可分发压缩包：Builds/WebGLLevelLoading.zip，index.html 位于压缩包根部。部署时替换完整包并重新加载页面；本地需 HTTP 服务，不能双击 file:// 启动。
- 本轮没有覆盖现有 WebGL Builds、connectwebgl.zip 或外部部署。新包尚未进行浏览器及手机端实玩验证；上述点击及画面验收来自 Unity Editor。
