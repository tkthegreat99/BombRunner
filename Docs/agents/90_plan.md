# Development Plan

## Current Status: Local Quick Match, Localization, and Prefab Feedback Baseline
- Bootstrap and Game scene transition are in place.
- `SceneFlowService` carries the requested `MatchMode` into the Game scene.
- `MatchMode.LocalInstantMatch` starts the local match loop immediately.
- `MatchMode.LocalQuickMatchWaiting` runs `LocalQuickMatchWaitingService` before match initialization.
- The quick-match waiting prototype shows participant fill, countdown, and match-start status through `LocalQuickMatchWaitingView`.
- In waiting mode, the local player is spawned before the waiting countdown, so local movement and dash can be checked in the waiting space.
- Local player movement, dash, and camera follow are usable for prototype play.
- Local player plus two dummy players spawn into the test stage.
- The bomb selects an alive target, drops in, waits for a start countdown, activates, chases, phases through randomized Calm/Warning/Overdrive durations, and explodes.
- Explosion resolution downs only the closest alive player in range.
- Downed players remain on the map, move slowly, are excluded from target/winner candidates, and now behave more like heavier separation obstacles.
- After each explosion, the previous bomb despawns, a new target is selected from alive players, and a new bomb spawn sequence starts.
- The local match ends when one survivor remains.
- Tag transfer and 3-second tag immunity are implemented.
- Local Taunt prototype exists: hold input creates a dash-lock area, disables the taunter's movement/dash, and can pull bomb target risk onto the taunter when held near the bomb.
- Local prototype item core exists: players can pick up one item, throw it to a limited range, apply Slow or Stun on hit, and resolve hit behavior per item type.
- Local authority now routes item pickup/throw/hit, target transfer, taunt-risk target changes, explosion victim/downed application, next target selection, and bomb phase duration randomization through `IMatchAuthorityService`.
- Early multiplayer session scaffolding exists through `IMatchNetworkSessionService`; the current implementation maps Steam Lobby ownership through `SteamMatchNetworkSessionService` and falls back to local Host authority without a lobby.
- Steamworks.NET is installed and Steam Lobby smoke-test support exists for creating a friends-only lobby, opening the Steam invite overlay, accepting invite joins, reading lobby member count, and propagating waiting/countdown/starting metadata.
- `Game` scene now contains scene-placed bridge Views for quick-match waiting status, match feedback, world feedback, and bomb-spawn camera focus.
- `GameLifetimeScope` no longer creates `LocalMatchFeedbackView`, `LocalQuickMatchWaitingView`, or bomb-spawn camera focus objects at runtime; missing View wiring is treated as a scene setup error.
- Localization foundation is in place through `LocalizationService` plus `Assets/BombRunner/Resources/Localization/en.json` and `ko.json`.
- Current localized prototype keys: `quick_match.waiting_count`, `quick_match.countdown`, `quick_match.starting`, `match.feedback.bomb_spawn`, `match.feedback.winner`, and `match.feedback.tag_immune`.
- The current `ko` resource intentionally mirrors English copy until the font/localized glyph strategy is confirmed.
- Target marker, bomb-target link, taunt dash-lock area, explosion decision ring, selected victim marker, and tag immunity feedback now have prefab/View entry points.
- The TextMesh Pro experiment was rolled back; current UI bridge Views stay on `UnityEngine.UI.Text`.

## Active Priority: Unity Validation and Prototype Cleanup
1. Playtest the local quick-match waiting path in Unity:
   - Enter quick-match mode from the menu and verify `Joining 1 / 8`.
   - Verify participant fill, countdown, and `Match starting` status.
   - Move and dash the local player during the waiting-room countdown.
   - Confirm the match loop starts after waiting status hides.
2. Playtest the scene-placed local feedback Views:
   - Bomb spawn warning text appears in the overlay warning slot.
   - Bomb start countdown appears in the center overlay slot.
   - Match-end result appears in the overlay result slot.
   - Explosion decision ring and selected victim marker appear from the feedback prefab.
   - Tag immunity feedback appears above the rejected receiver.
   - Target marker, bomb-target link, and taunt dash-lock area track their targets.
   - Missing View references produce clear errors or warnings instead of runtime UI construction.
3. Remove remaining prototype presentation internals when art/pooling direction is clear:
   - Move fallback-created line renderers and materials into prefabs where practical.
   - Decide which repeated feedback needs pooling.
   - Keep world feedback under explicit View ownership.
4. Continue play-feel checks in the closed local loop:
   - Tag distance versus separation radius.
   - Downed-player blocking and pushing feel.
   - Countdown input lock, including dash/taunt cancellation.
   - Camera focus on bomb spawn and snap-back when play resumes.
   - Bomb drop timing before activation.
5. Tune `GameBalanceSettings` instead of hard-coding values:
   - Tag distance and immunity duration.
   - Bomb phase durations, speeds, drop, and spawn countdown.
   - Alive/downed separation radius, strength, and downed push weight.
   - Taunt radius, bomb-risk hold time, and bomb-risk distance.
6. Keep local authority behavior Host/Master-shaped even before networking is selected.
7. Keep the current multiplayer layer Netcode-free until Steam Lobby smoke tests pass and the concrete transport path is chosen.

## Next
1. Run a Unity Editor smoke test for the full local quick-match loop and record any scene/prefab wiring issues.
2. Expand item play from the local Slow-item prototype:
   - Move temporary primitive pickup/projectile visuals into prefabs when art direction is ready.
   - Add item spawn timing, respawn rules, and map spawn points.
   - Add more throw item effects such as knockback, puddle, banana peel, or bumper.
   - Add more hit behaviors such as consume, drop, pierce, bounce, or split.
   - Split Host/Master item pickup, throw, collision, and effect confirmation into network-ready boundaries.
3. Expand Taunt from prototype to proper gameplay module:
   - Data asset or settings section for taunt balance.
   - Area visualization.
   - Hold cancel/after-delay behavior.
   - Non-guaranteed bomb reaction variants such as target swap, anger, or speed burst.
4. Promote bridge Views into prefab or final HUD/world-feedback Views:
   - Keep `LocalMatchFeedbackView` as a bridge, not final UI architecture.
   - Replace the Overlay Canvas bridge with final HUD/prefab presentation when the final UI direction is known.
   - Avoid adding new runtime-created feedback objects.
5. Add focused validation:
   - C# build after runtime changes.
   - Manual Unity playtest checklist for local loop feel.
   - Later, EditMode/PlayMode tests around target selection, explosion victim selection, and state transitions.
6. Start real networking behind the existing seams:
   - Choose the concrete Unity multiplayer stack and package version.
   - Add Netcode for GameObjects and a Steam transport after Steam Lobby smoke tests pass.
   - Add request/confirm RPCs only after scene/prefab ownership and player identity mapping are defined.
