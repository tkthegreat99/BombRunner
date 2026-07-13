# Boom Runner Architecture Notes

## Current Runtime Shape
- `ProjectLifetimeScope` registers global app services.
- `GameLifetimeScope` registers local game-scoped services and scene references.
- `SceneFlowService` records the requested local match mode while moving from menu/bootstrap scenes into `Game`.
- `MatchMode` currently separates instant local match startup from local quick-match waiting startup.
- `StageManager` starts the local prototype flow after reading `SceneFlowService.RequestedMatchMode`.
- `LocalQuickMatchWaitingService` simulates a Host-owned waiting-room participant fill and countdown before the local match loop starts.
- `PlayerSpawnService` spawns local and dummy players.
- `BombSpawnService` spawns a local prototype bomb.
- `BombTargetService` owns the current target state for the local prototype.
- `LocalMatchFlowService` owns the local bomb spawn countdown, activation, explosion response, respawn loop, and one-survivor match end.
- `LocalMatchFeedbackView` is an Overlay Canvas bridge View for bomb spawn warning, bomb start countdown, and match result display. Explosion decision and tag rejection visuals are still temporary runtime objects.
- `LocalQuickMatchWaitingView` is an Overlay Canvas waiting-room status View. It must be wired to a scene or prefab UI Text instead of creating runtime UI.

## Intended Boundaries
- Match flow should move toward a `MatchFlowService` or equivalent service.
- Bomb lifecycle should move toward a service that can spawn, resolve explosion, despawn/pool, and respawn bombs.
- Balance values should move toward ScriptableObject data assets, starting from `GameBalanceSettings` if a single asset is enough.
- UI should use scene-placed or prefab View classes, not runtime-assembled UI.
- Player-facing UI text should move behind a localization service/table before the UI becomes final. Views should bind localization keys and formatted arguments, not own final display strings.

## Authority Rules
- Host/Master confirms target changes, tag immunity, bomb phase randomization, explosion victim, downed state, taunt effects, item pickup/throw/hit, and status effects.
- Clients send input and play local presentation, then reconcile to authoritative results.
- Local prototype code may simulate Host/Master authority, but comments should state the future authority boundary.

## Temporary Code Policy
- `MainMenuQuickStartController`, `DashCooldownLogView`, `LocalPlayerCameraFollow`, and local-only match prototypes are allowed for fast validation.
- Temporary code must not become the final network, camera, UI, or match flow architecture.
- Temporary comments should explain purpose and replacement timing.
- `LocalMatchFeedbackView` may keep temporary explosion decision/tag rejection objects until those presentations get dedicated scene-placed or prefab Views.
- Temporary hard-coded Korean UI strings are allowed only for prototype validation and should be replaced with localization keys during the UI pass.
