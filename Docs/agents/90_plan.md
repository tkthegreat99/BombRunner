# Development Plan

## Current Status: Local Quick Match Flow and Scene-Placed Feedback Started
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
- `Game` scene now contains a scene-placed Overlay Canvas bridge for quick-match waiting status plus bomb spawn warning, bomb start countdown, and match-end result.
- `GameLifetimeScope` no longer creates `LocalMatchFeedbackView` or `LocalQuickMatchWaitingView` at runtime; missing View wiring is treated as a scene setup error.
- Localization is now a product requirement: player-facing UI strings should move to localization keys before the UI becomes final.

## Active Priority: Waiting Room and Feedback View Validation
1. Playtest the local quick-match waiting path in Unity:
   - Enter quick-match mode from the menu and verify `입장 중 1 / 8`.
   - Verify participant fill, countdown, and `매치 시작` status.
   - Move and dash the local player during the waiting-room countdown.
   - Confirm the match loop starts after waiting status hides.
2. Playtest the scene-placed local feedback View:
   - Bomb spawn warning text appears in the overlay warning slot.
   - Bomb start countdown appears in the center overlay slot.
   - Match-end result appears in the overlay result slot.
   - Missing View references produce clear errors or warnings instead of runtime UI construction.
3. Add localization foundation for prototype UI:
   - Define baseline languages: Korean and English.
   - Add a localization table/resource format and lookup service.
   - Replace hard-coded quick-match waiting, countdown, match start, match result, bomb warning, and tag immunity strings with localization keys.
   - Support formatted values such as `입장 중 {0} / {1}` and winner names.
   - Add fallback behavior for missing language/key.
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

## Next
1. Implement localization foundation before adding more final UI text:
   - Localization service/table.
   - Korean/English resources.
   - Localized UI helper or View binding pattern.
   - Prototype UI string migration.
2. Replace remaining temporary local feedback objects with scene-placed or prefab View hierarchy:
   - Target marker and bomb-target connection.
   - Explosion decision ring and selected victim marker.
   - Tag immunity rejection notice.
   - Dash locked/disabled feedback for taunt areas.
3. Strengthen downed-player obstacle play:
   - Low/downed collider profile or dedicated stomp area.
   - Downed animation/crawl presentation.
   - Survivor stumble or shove feedback when stepping on downed players.
   - Bomb temporary acceleration when crossing a downed player.
4. Expand Taunt from prototype to proper gameplay module:
   - Data asset or settings section for taunt balance.
   - Area visualization.
   - Hold cancel/after-delay behavior.
   - Non-guaranteed bomb reaction variants such as target swap, anger, or speed burst.
5. Promote bridge Views into prefab or final HUD/world-feedback Views:
   - Keep `LocalMatchFeedbackView` as a bridge, not final UI architecture.
   - Replace the Overlay Canvas bridge with final HUD/prefab presentation when the final UI direction is known.
   - Avoid adding new runtime-created feedback objects.
6. Add focused validation:
   - C# build after runtime changes.
   - Manual Unity playtest checklist for local loop feel.
   - Later, EditMode/PlayMode tests around target selection, explosion victim selection, and state transitions.
