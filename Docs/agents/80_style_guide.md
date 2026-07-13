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
- Do not add new final player-facing UI strings as hard-coded literals. Use localization keys, with prototype literals clearly treated as temporary.

## Localization
- Korean and English are the baseline languages for Steam release.
- UI Views should request localized text through a service/table instead of owning final display copy.
- Dynamic values such as player counts, countdowns, and player names should use formatted localization arguments.
- Missing translations should fall back safely to the default language or visible key.

## Gameplay Data
- Do not scatter balance constants through gameplay code.
- Move tunable values toward data assets or serialized settings.
- Keep target/tag/explosion/downed rules consistent with `Docs/BombRunner_수정 기획.txt`.
