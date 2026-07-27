# 后续任务提示词模板

## 1. 任务分类前置规则

生成或使用任何任务提示词前，必须先选择且只选择一种任务类型：

- 脚本开发任务：只处理脚本、测试脚本、配置文本和开发文档，不调用 Unity MCP。
- Editor 工作任务：只处理 Scene、Prefab、GameObject、Component、Inspector 和资源配置；调用 MCP 前必须审批。
- 测试任务：只通过 Unity MCP 运行和验证当前游戏；调用 MCP 前必须审批，不在测试中修复问题。

如果完整需求跨越多个类型，拆成多个独立提示词，固定按“脚本开发 -> Editor 工作 -> 测试”顺序逐个执行。前一任务结束并汇报后，等待用户发起下一任务，不得在一个提示词中要求全部完成。

## 2. 脚本开发任务模板

```text
Read AGENTS.md first.

Task type: Script Development.
This task only creates, modifies or deletes scripts, test scripts, configuration text and development documents for [本次目标].

Use terminal and file tools only.
Do not call any Unity MCP tool.
Do not modify Scenes, Prefabs, GameObjects, Components or Inspector bindings.
Do not enter Play Mode or run the game.

When finished, list the separate Editor work and test work still required, then stop.
```

## 3. Editor 工作任务模板

```text
Read AGENTS.md first.

Task type: Unity Editor Work.
This task only configures [Scene / Prefab / GameObject / Component / Inspector / resource import target].

Do not modify source scripts.
Do not enter Play Mode or run gameplay tests.

Before any Unity MCP call, request user approval and state:
- why MCP is required
- the MCP tools or capabilities you plan to call
- the target Scene, Prefab, GameObject, Component or asset
- the planned operations, expected result and possible impact

Wait for explicit approval. Before every approved call, tell the user which tool is being called and why. Report the result after the call.
When Editor configuration is complete, stop and wait for a separate test task.
```

## 4. 测试任务模板

```text
Read AGENTS.md first.

Task type: Unity Test.
This task only runs and verifies the current game in [测试场景或测试范围].

Do not modify scripts, Scenes, Prefabs, GameObjects, Components, Inspector bindings or assets.

Before any Unity MCP call, request user approval and state:
- the MCP tools or capabilities you plan to call
- the test scene and test steps
- the states, logs and gameplay rules to observe
- the expected result and possible impact

Wait for explicit approval. Before every approved call, tell the user which tool is being called and why. Report evidence, Console output and failed checks after the call.
If a defect is found, document it and stop. Do not fix it in the same task.
```

## 5. 通用任务模板

```text
Read AGENTS.md first.

This task only focuses on [本次任务目标].

Do not add unrelated gameplay systems.
Do not add real ad SDK.
Do not add real payment SDK.
Do not add backend services.
Do not add account systems.
Do not add online multiplayer.
Do not add leaderboard systems.
Keep the Unity project runnable after changes.

If Unity Editor setup is required, clearly list the GameObjects, scripts, Prefab references, UI references, and Inspector bindings I need to configure manually.
```

## 6. 最小核心闭环任务模板

```text
Read AGENTS.md first.

This task focuses only on running the minimum playable core loop.

The required loop is:

1. Start game.
2. Enter normal map.
3. Start 60-second main timer.
4. Player can move.
5. Enemy can spawn.
6. Player touches enemy and enters auto battle.
7. During normal battle, the 60-second main timer continues.
8. Battle ends and returns to map if player wins.
9. If player loses, enter fail result.
10. When 60 seconds ends, enter final Boss battle.
11. Boss battle is independent and not limited by the 60-second timer.
12. Boss battle ends and enters result screen.
13. Player can restart.

Do not add extra systems beyond this loop.
```

## 7. 自动战斗任务模板

```text
Read AGENTS.md first.

This task focuses only on Auto Battle.

Normal battle rules:

1. Player touches a normal enemy.
2. Game enters battle screen or battle state.
3. Player and enemy attack automatically.
4. Battle continues until one side is defeated.
5. During this battle, the normal map 60-second timer continues.
6. After battle ends, return to the normal map if the player wins.
7. If the player loses, enter result or fail state.

Boss battle rules:

1. Boss battle is a separate final phase.
2. Boss battle does not use the normal map 60-second limit.
3. Boss battle can calculate its own battle time.
```

## 8. 隐藏洞穴任务模板

```text
Read AGENTS.md first.

This task focuses only on Hidden Cave.

Hidden Cave rules:

1. Player can find or trigger a hidden cave entrance on the normal map.
2. Entering the hidden cave pauses the normal map 60-second timer.
3. The cave can contain a simple battle, event, or reward.
4. After cave content ends, return to the normal map.
5. The normal map timer resumes after returning.

Do not turn the cave into a complex dungeon system.
```

## 9. Boss 战任务模板

```text
Read AGENTS.md first.

This task focuses only on Final Boss Battle.

Boss rules:

1. Boss battle starts after the normal map 60-second timer reaches 0.
2. Boss battle is an independent phase.
3. Boss battle does not consume or depend on the normal map 60-second timer.
4. Boss battle can have its own timer or battle duration statistics.
5. Boss victory enters clear result.
6. Boss defeat enters fail result.

Do not change the normal battle or hidden cave time rules.
```

## 10. Debug 工具任务模板

```text
Read AGENTS.md first.

This task focuses only on Debug tools for faster testing.

Add or improve debug functions for:

- Start run.
- Reset run.
- Add player attack.
- Add player HP.
- Add player experience.
- Spawn enemy.
- Spawn hidden cave.
- Enter Boss battle directly.
- Force win.
- Force lose.
- Print current player stats.
- Print current game state.

Debug features must not break normal gameplay.
```

## 11. UI 验证任务模板

```text
Read AGENTS.md first.

This task focuses only on UI readability and validation.

Check or improve UI for:

- Main map timer.
- Player HP.
- Player level or cultivation.
- Battle player HP.
- Battle enemy HP.
- Cave time-paused indicator.
- Boss HP.
- Boss independent timer or duration display.
- Result screen.
- Restart button.

Do not change gameplay rules unless required by a UI bug.
```

## 12. 美术资源生产任务模板

```text
Read AGENTS.md first.
Read docs/art_style_guide.md and docs/art_production_pipeline.md.

This task only produces [角色动画 / 武学图标 / 道具图标 / 场景资源].

Follow the approved wuxia art direction, dimensions, naming, folder structure,
generation prompts, Unity import settings and quality gates.

For character animation:
- start from one approved in-game seed frame
- generate the full horizontal strip in one request
- use 256 × 256 px per frame
- normalize all frames with one shared scale and bottom-center foot anchor
- do not generate frames independently

For icons:
- generate a 256 × 256 transparent master
- deliver a 128 × 128 Unity icon
- verify readability at 64 × 64 and 48 × 48
- do not bake text, rarity borders or cooldown overlays into the icon

Report Generated, Normalized, Imported, InEngineQA and Approved as separate states.
Do not claim the asset is final before Play Mode verification.
Do not modify gameplay or the three core timing rules.
```
