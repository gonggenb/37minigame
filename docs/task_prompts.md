# 后续任务提示词模板

## 1. 通用任务模板

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

## 2. 最小核心闭环任务模板

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

## 3. 自动战斗任务模板

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

## 4. 隐藏洞穴任务模板

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

## 5. Boss 战任务模板

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

## 6. Debug 工具任务模板

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

## 7. UI 验证任务模板

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

## 8. 美术资源生产任务模板

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
