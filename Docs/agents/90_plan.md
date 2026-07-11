# Development Plan

## Current Status: Local Minimum Loop Closed
- Bootstrap and Game scene transition are in place.
- Local player movement, dash, and camera follow are usable for prototype play.
- Local player plus two dummy players spawn into the test stage.
- The bomb selects an alive target, drops in, waits for a start countdown, activates, chases, phases through randomized Calm/Warning/Overdrive durations, and explodes.
- Explosion resolution downs only the closest alive player in range.
- Downed players remain on the map, move slowly, are excluded from target/winner candidates, and now behave more like heavier separation obstacles.
- After each explosion, the previous bomb despawns, a new target is selected from alive players, and a new bomb spawn sequence starts.
- The local match ends when one survivor remains.
- Tag transfer and 3-second tag immunity are implemented.
- Local Taunt prototype exists: hold input creates a dash-lock area, disables the taunter's movement/dash, and can pull bomb target risk onto the taunter when held near the bomb.

## Active Priority: Play Feel Stabilization
1. Playtest the closed local loop in Unity:
   - Tag distance versus separation radius.
   - Downed-player blocking and pushing feel.
   - Countdown input lock, including dash/taunt cancellation.
   - Camera focus on bomb spawn and snap-back when play resumes.
   - Bomb drop timing before activation.
2. Tune `GameBalanceSettings` instead of hard-coding values:
   - Tag distance and immunity duration.
   - Bomb phase durations, speeds, drop, and spawn countdown.
   - Alive/downed separation radius, strength, and downed push weight.
   - Taunt radius, bomb-risk hold time, and bomb-risk distance.
3. Keep local authority behavior Host/Master-shaped even before networking is selected.

## Next
1. Replace temporary local feedback objects with scene-placed or prefab View hierarchy:
   - Target marker and bomb-target connection.
   - Bomb spawn warning/countdown.
   - Match-end result display.
   - Dash locked/disabled feedback for taunt areas.
2. Strengthen downed-player obstacle play:
   - Low/downed collider profile or dedicated stomp area.
   - Downed animation/crawl presentation.
   - Survivor stumble or shove feedback when stepping on downed players.
   - Bomb temporary acceleration when crossing a downed player.
3. Expand Taunt from prototype to proper gameplay module:
   - Data asset or settings section for taunt balance.
   - Area visualization.
   - Hold cancel/after-delay behavior.
   - Non-guaranteed bomb reaction variants such as target swap, anger, or speed burst.
4. Move remaining temporary runtime visuals out of services:
   - `LocalMatchFeedbackView` is a bridge, not final UI architecture.
   - Avoid letting TextMesh/runtime-created feedback become permanent.
5. Add focused validation:
   - C# build after runtime changes.
   - Manual Unity playtest checklist for local loop feel.
   - Later, EditMode/PlayMode tests around target selection, explosion victim selection, and state transitions.
