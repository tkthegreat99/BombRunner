# Boom Runner Overview

## Detected Facts
- Unity URP project with C# runtime code under `Assets/BombRunner/Scripts`.
- DI uses VContainer.
- Async gameplay timing uses UniTask.
- Input uses Unity New Input System.
- Current prototype has local player spawning, dummy spawning, dash, target toss, local bomb spawn, and temporary bomb chase logic.

## Current Design Source
- `Docs/BombRunner_수정 기획.txt` is the active design source.
- The previous plan is superseded where it conflicts with the revised design.

## Core Game
- Top-down 3D multiplayer party survival.
- Players avoid a living bomb that chases a visible target.
- The target can tag another living player to pass the target state.
- Tag immunity lasts 3 seconds and only blocks target transfer. It is not a shield.
- The bomb has Calm, Warning, and Overdrive phases with randomized durations.
- No numeric timer UI.
- On explosion, only the closest alive player inside the explosion range is downed.
- Downed players remain in the map as slow crawling obstacles.
- The match ends when only one alive player remains.

## Removed Old Rules
- Fixed 15-second bomb lifetime.
- Full-party wipe.
- Last Stand.
- Millisecond survival winner calculation.
- Star score and mid-match respawn.

## Open TODOs
- Final networking stack is not confirmed in code.
- Actual voice chat provider is not selected.
- Pooling and UI systems are still prototype-level.
