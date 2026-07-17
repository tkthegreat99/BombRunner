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
- `BombTargetService` owns current target state application and target-change notification for the local prototype.
- `IMatchAuthorityService` is the Host/Master confirmation boundary. `LocalMatchAuthorityService` currently approves immediately for the local prototype.
- `IMatchNetworkSessionService` is the early multiplayer session boundary. `SteamMatchNetworkSessionService` maps Steam Lobby ownership to Host/Client authority and falls back to `LocalHost` when no Steam lobby exists.
- `SteamworksClientService` currently owns Facepunch Steam client init, callback pumping, and shutdown while keeping the old service name temporarily.
- `SteamLobbyService` owns Facepunch Steam quick-match lobby creation, invite overlay, command-line `+connect_lobby` handling, lobby member count, and simple lobby match-state metadata.
- Netcode for GameObjects and Facepunch Transport are installed for the next multiplayer integration step.
- `LocalMatchFlowService` owns the local bomb spawn countdown, activation, explosion response, respawn loop, and one-survivor match end.
- `LocalMatchFeedbackView` is an Overlay Canvas bridge View for bomb spawn warning, bomb start countdown, match result display, explosion decision feedback, and tag rejection feedback.
- `LocalQuickMatchWaitingView` is an Overlay Canvas waiting-room status View. It must be wired to a scene or prefab UI Text instead of creating runtime UI.
- `LocalWorldFeedbackView` owns prefab-based target marker, bomb-target link, and taunt dash-lock area presentation for the local prototype.
- `LocalBombSpawnCameraFocusView` replaces the previous ad-hoc bomb-spawn camera focus GameObject.

## Intended Boundaries
- Match flow should move toward a `MatchFlowService` or equivalent service.
- Bomb lifecycle should move toward a service that can spawn, resolve explosion, despawn/pool, and respawn bombs.
- Balance values should move toward ScriptableObject data assets, starting from `GameBalanceSettings` if a single asset is enough.
- UI should use scene-placed or prefab View classes, not runtime-assembled UI.
- Player-facing UI text should stay behind `LocalizationService` and resource tables. Views should bind localization keys and formatted arguments, not own final display strings.

## Authority Rules
- Host/Master confirms target changes, tag immunity, bomb phase randomization, explosion victim, downed state, taunt effects, item pickup/throw/hit, and status effects.
- Current local authority confirms random next-target selection, bomb phase duration randomization, explosion victim selection, downed-state application, item pickup/throw/hit, target transfer, and taunt-risk target changes.
- Steam Lobby currently coordinates friend testing only up to lobby/waiting/countdown flow. Real-time gameplay synchronization should move through Netcode for GameObjects with Facepunch Transport.
- Clients send input and play local presentation, then reconcile to authoritative results.
- Local prototype code may simulate Host/Master authority, but comments should state the future authority boundary.

## Temporary Code Policy
- `MainMenuQuickStartController`, `DashCooldownLogView`, `LocalPlayerCameraFollow`, and local-only match prototypes are allowed for fast validation.
- Temporary code must not become the final network, camera, UI, or match flow architecture.
- Temporary comments should explain purpose and replacement timing.
- Prototype feedback Views may keep local fallback renderer/material creation only as an implementation detail until final art and pooling are chosen.
- Do not add new hard-coded player-facing UI strings; add localization keys and formatted arguments instead.
