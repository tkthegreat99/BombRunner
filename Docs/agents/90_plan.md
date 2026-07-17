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
- Facepunch.Steamworks is provided through Facepunch Transport, and Steam Lobby smoke-test support exists for creating a friends-only lobby, opening the Steam invite overlay, joining by `+connect_lobby`, reading lobby member count, and propagating waiting/countdown/starting metadata.
- Netcode for GameObjects and Facepunch Transport are installed for real gameplay synchronization.
- In Steam lobby matches, non-host clients no longer start the local bomb/match authority loop; the next step is replacing the removed temporary P2P snapshot path with NGO network spawn and authoritative state replication.
- `Game` scene now contains scene-placed bridge Views for quick-match waiting status, match feedback, world feedback, and bomb-spawn camera focus.
- `GameLifetimeScope` no longer creates `LocalMatchFeedbackView`, `LocalQuickMatchWaitingView`, or bomb-spawn camera focus objects at runtime; missing View wiring is treated as a scene setup error.
- Localization foundation is in place through `LocalizationService` plus `Assets/BombRunner/Resources/Localization/en.json` and `ko.json`.
- Current localized prototype keys: `quick_match.waiting_count`, `quick_match.countdown`, `quick_match.starting`, `match.feedback.bomb_spawn`, `match.feedback.winner`, and `match.feedback.tag_immune`.
- The current `ko` resource intentionally mirrors English copy until the font/localized glyph strategy is confirmed.
- Target marker, bomb-target link, taunt dash-lock area, explosion decision ring, selected victim marker, and tag immunity feedback now have prefab/View entry points.
- The TextMesh Pro experiment was rolled back; current UI bridge Views stay on `UnityEngine.UI.Text`.

## Active Priority: NGO + FacepunchTransport First Sync
1. Stabilize the package baseline:
   - Keep Facepunch Transport embedded under `Packages/com.community.netcode.transport.facepunch` because the Git package currently has a duplicate `#endregion` compile issue with this project setup.
   - Keep Steamworks.NET removed; Facepunch.Steamworks comes from the Facepunch Transport package.
   - Confirm Unity Console is clear after package import and script reload.
2. Add an NGO session bootstrap:
   - Create a scene or service-owned `NetworkManager` setup using Facepunch Transport.
   - Steam lobby owner starts `NetworkManager.StartHost()`.
   - Non-owner lobby members set `FacepunchTransport.targetSteamId` to the lobby owner Steam ID and call `NetworkManager.StartClient()`.
   - Keep Steam Lobby responsible only for discovery, invite, ownership, and lobby metadata.
3. Replace Steam-lobby manual player spawning with network spawning:
   - Add `NetworkObject` to the player prefab.
   - Register the player prefab in NGO network prefabs.
   - Host spawns one network player per connected client.
   - Preserve local single-player fallback through the existing local spawn path.
4. Implement minimum player movement sync:
   - First target is "two Steam users can see each other move in the same map."
   - Keep local input only on the owning client.
   - Send movement intent or position through NGO in a Host-authoritative shape.
   - Avoid syncing bomb, target, items, and taunt until basic player spawn/motion is stable.
5. Move authority decisions onto NGO after movement works:
   - Host confirms bomb target, explosion victim, downed state, item pickup/throw/hit, target transfer, taunt risk, and match end.
   - Use the existing `IMatchAuthorityService` boundary as the migration seam.
   - Keep presentation and local feedback separate from authoritative results.

## Next
1. Implement the NGO session bootstrap:
   - Add the runtime object/component that owns `NetworkManager` and Facepunch Transport startup.
   - Wire Host/Client startup from `ISteamLobbyService` and `SteamMatchNetworkSessionService`.
   - Add clear logs for lobby owner Steam ID, local Steam ID, Host/Client decision, and connect attempt.
2. Implement network player spawn and local ownership:
   - Add required NGO components to the player prefab.
   - Route local input to the owned player only.
   - Disable local-only dummy spawning for Steam lobby network matches.
3. Validate with the friend-test build:
   - Host creates Steam lobby and invites friend.
   - Client accepts invite and joins lobby.
   - Host starts as NGO Host; client starts as NGO Client.
   - Both machines see both players.
   - Local movement from each machine is visible on the other.
4. Keep local prototype regression checks:
   - Enter local quick match from the menu.
   - Confirm local player plus dummy players still spawn.
   - Confirm bomb loop still starts and downs only one victim per explosion.
5. Expand item play from the local Slow-item prototype:
   - Move temporary primitive pickup/projectile visuals into prefabs when art direction is ready.
   - Add item spawn timing, respawn rules, and map spawn points.
   - Add more throw item effects such as knockback, puddle, banana peel, or bumper.
   - Add more hit behaviors such as consume, drop, pierce, bounce, or split.
   - Split Host/Master item pickup, throw, collision, and effect confirmation into network-ready boundaries.
6. Expand Taunt from prototype to proper gameplay module:
   - Data asset or settings section for taunt balance.
   - Area visualization.
   - Hold cancel/after-delay behavior.
   - Non-guaranteed bomb reaction variants such as target swap, anger, or speed burst.
7. Promote bridge Views into prefab or final HUD/world-feedback Views:
   - Keep `LocalMatchFeedbackView` as a bridge, not final UI architecture.
   - Replace the Overlay Canvas bridge with final HUD/prefab presentation when the final UI direction is known.
   - Avoid adding new runtime-created feedback objects.
8. Add focused validation:
   - C# build after runtime changes.
   - Manual Unity playtest checklist for local loop feel.
   - Later, EditMode/PlayMode tests around target selection, explosion victim selection, and state transitions.
