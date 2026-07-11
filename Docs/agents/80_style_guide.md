# Style Guide

## C#
- No leading underscore for private fields.
- Use `var` when the assigned type is obvious.
- Avoid LINQ in hot gameplay paths.
- No new Coroutine usage.
- Use UniTask with cancellation for delays, loops, and cooldowns.
- Keep comments short and useful.

## Unity
- Prefer serialized references, prefabs, ScriptableObjects, and scene-placed Views.
- Do not dynamically assemble HUD, popup, result, or loading UI in code.
- Prototype primitives are allowed for gameplay validation when collider and logic structure are kept replaceable.

## Gameplay Data
- Do not scatter balance constants through gameplay code.
- Move tunable values toward data assets or serialized settings.
- Keep target/tag/explosion/downed rules consistent with `Docs/BombRunner_수정 기획.txt`.
