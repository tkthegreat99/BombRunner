# Boom Runner Architecture Notes

## Current Runtime Shape
- `ProjectLifetimeScope` registers global app services.
- `GameLifetimeScope` registers local game-scoped services and scene references.
- `StageManager` currently starts the local prototype flow.
- `PlayerSpawnService` spawns local and dummy players.
- `BombSpawnService` spawns a local prototype bomb.
- `BombTargetService` owns the current target state for the local prototype.

## Intended Boundaries
- Match flow should move toward a `MatchFlowService` or equivalent service.
- Bomb lifecycle should move toward a service that can spawn, resolve explosion, despawn/pool, and respawn bombs.
- Balance values should move toward ScriptableObject data assets, starting from `GameBalanceSettings` if a single asset is enough.
- UI should use scene-placed or prefab View classes, not runtime-assembled UI.

## Authority Rules
- Host/Master confirms target changes, tag immunity, bomb phase randomization, explosion victim, downed state, taunt effects, item pickup/throw/hit, and status effects.
- Clients send input and play local presentation, then reconcile to authoritative results.
- Local prototype code may simulate Host/Master authority, but comments should state the future authority boundary.

## Temporary Code Policy
- `MainMenuQuickStartController`, `DashCooldownLogView`, `LocalPlayerCameraFollow`, and local-only match prototypes are allowed for fast validation.
- Temporary code must not become the final network, camera, UI, or match flow architecture.
- Temporary comments should explain purpose and replacement timing.
