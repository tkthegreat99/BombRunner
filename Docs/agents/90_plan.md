# Development Plan

## Active Priority: Local Core
1. Keep bootstrap and scene transition working.
2. Keep local player movement and dash responsive.
3. Add a readable test map with wall/player collision.
4. Build quick-match waiting playground prototype.
5. Show waiting player count UI from room/matchmaking state when available.
6. Add lightweight waiting-room throwable item prototype.
7. Spawn a bomb and chase the current target.
8. Use randomized bomb phase durations and phase speeds.
9. Implement tag and 3-second tag immunity.
10. Resolve explosion by downing only the closest alive player in range.
11. Spawn a new bomb and select a new target after each explosion.
12. Prevent player overlap with collision/separation.
13. End match when one survivor remains.

## Today
- Refresh AGENTS.md and agent docs from the revised design.
- Align the local prototype with the revised core rules:
  - 3-second tag immunity.
  - Randomized bomb phase durations.
  - Closest-one explosion down rule.
- Run a C# build.

## Next
- Add local bomb respawn and survivor-count match end.
- Add data asset for balance values.
- Add downed crawling state and movement restrictions.
