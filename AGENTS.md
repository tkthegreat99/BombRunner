# Repository Guidelines

## Project Structure & Module Organization
- Unity URP project for Boom Runner, a top-down 3D multiplayer party survival game.
- Runtime code lives under `Assets/BombRunner/Scripts/`.
- Scenes live under `Assets/BombRunner/Project/Scenes/`.
- Prefabs, art, audio, and data live under `Assets/BombRunner/Prefabs/`, `Assets/BombRunner/Art/`, `Assets/BombRunner/Audio/`, and `Assets/BombRunner/Data/`.
- Current source of truth for game design is `Docs/BombRunner_수정 기획.txt`.
- Agent-facing summaries and plans live under `Docs/agents/`.

## Documentation
- Read `Docs/agents/00_overview.md` before changing gameplay rules.
- Read `Docs/agents/10_architecture.md` before changing service boundaries, scene flow, DI, or authority logic.
- Read `Docs/agents/30_commands.md` before running validation commands.
- Read `Docs/agents/90_plan.md` before choosing the next development task.

## Gameplay Direction
- Do not use the old fixed 15-second bomb, full-party wipe, Last Stand, ms-survival, star-score, or respawn match rules.
- Current match loop: all players start alive, the bomb chases one target, each explosion downs only the closest alive player in range, a new bomb spawns, and the match ends when one survivor remains.
- Downed players stay on the map as slow crawling obstacles; they are not target candidates and cannot win.
- Taunt is a risky hold action that creates a dash-lock area; target changes caused by taunt are a risk, not a guaranteed reward.
- Numbers such as tag range, tag immunity, bomb phase durations, speeds, explosion radius, and downed movement should move toward data assets, not hard-coded values.

## Coding Style & Naming Conventions
- C# private fields and private properties must not use leading underscores.
- Prefer `var` when the right-hand side makes the type obvious.
- Avoid LINQ in `Update`, network ticks, collision checks, round flow, and frequently called gameplay logic.
- Do not add Unity Coroutine usage. Use UniTask/async/await with `CancellationToken`.
- Korean comments are allowed and should end as noun phrases, for example `인스턴스 생성.`.
- Temporary prototype code is allowed only when clearly commented with purpose and replacement timing.

## Architecture Rules
- Preserve existing behavior unless the design document explicitly supersedes it.
- Keep gameplay authority decisions Host/Master-oriented even in local prototypes.
- Clients may request or visualize; Host/Master confirms target changes, explosion victims, downed state, item hits, taunt effects, and bomb phase randomness.
- Use VContainer for services, managers, factories, match flow, and pools. Do not add arbitrary singletons.
- Use R3 for reactive state/UI when introduced, and dispose subscriptions.
- Use DOTween for UI animation when introduced, and kill tweens on disable/dispose.
- Avoid runtime UI construction. Use prefabs, scene-placed views, or explicit View classes.
- Prefer pooling for repeated SFX, VFX, indicators, items, projectiles, puddles, players, bombs, and map blocks.

## Commit Message & PR Guidelines
### Commit Message Format
- Use concise Korean or English imperative messages.
- Examples: `feat: add local bomb down rule`, `docs: refresh Boom Runner agent plan`.

### Atomic Commits
- Separate docs-only updates from gameplay/runtime changes when practical.
- Do not include unrelated Unity asset churn unless the task requires it.

## Agent Operation & Safety Rules
- Start by inspecting current structure and relevant files.
- Keep changes scoped and compatible with Unity serialization.
- Do not revert user changes or unrelated dirty worktree files.
- Do not use destructive git commands unless explicitly requested.
- Run `dotnet build Assembly-CSharp.csproj -nologo -v minimal` after C# changes when possible.
- If Unity scene or prefab wiring is required but cannot be safely inferred, document the TODO instead of inventing hidden dependencies.

## Language Policy
- Reply to the user in Korean unless they ask otherwise.
- Code identifiers stay in English.
- User-facing in-game Korean text is acceptable when it appears in the design.

## User Interaction
- Be direct about what changed, what was verified, and what remains.
- When blocked by missing Unity Inspector data, say exactly which scene/prefab/object needs wiring.
