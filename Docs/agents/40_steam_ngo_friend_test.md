# Steam NGO Friend Test Notes

## 2026-07-21 Current Result
- Steam Lobby friend invite flow exists for the Windows friend-test build.
- Lobby owner acts as NGO Host.
- Non-owner lobby member acts as NGO Client and connects to the lobby owner Steam ID through Facepunch Transport.
- `PrototypePlayer` now has NGO components required for first movement sync.
- Each network player shows a temporary world-space nameplate above the character.
- The first sync target is intentionally narrow: both machines should see each other spawn and move.
- Bomb, target transfer, items, taunt, explosion authority, and match win logic stay disabled in Steam NGO mode until player movement is stable.

## Test Build
- Build output folder: `Builds/FriendTest_NGO`
- Share zip: `Builds/BombRunner_FriendTest_NGO.zip`
- The zip must contain `BombRunner.exe` and `steam_appid.txt`.
- Both players must run the same fresh zip. Download folders such as `BombRunner_FriendTest_NGO (1)` can easily hide stale builds.

## Manual Test Flow
1. Both players start Steam and log into different Steam accounts.
2. Both players fully quit any older BombRunner build.
3. Host extracts the newest zip and runs `BombRunner.exe`.
4. Host presses Enter/Space on the main menu.
5. Host sends the Steam invite from the overlay.
6. Client runs the same build and accepts the invite.
7. Client should not press Enter after accepting the invite.
8. Waiting UI should converge to `2/8` on both machines.
9. Host countdown starts, then NGO Host starts.
10. Client starts as NGO Client.
11. Both machines should see the local and remote player nameplates and movement.

## Expected Logs
- Steam init: `Facepunch Steam initialized`
- Lobby state refresh: `Facepunch Steam lobby state updated`
- Host role: `NGO Steam bootstrap: StartHost result=True`
- Client role: `NGO Steam bootstrap: StartClient result=True`
- Spawn: `Network player spawned`
- Local ready: `NGO Steam bootstrap: local player ready`

## Known Fixes From Today
- Host stuck at `1/8` while client saw `2/8`:
  - Added explicit `RefreshLobbyState()` polling during Steam waiting.
  - Added lobby member data touch after join to help Steam emit member updates.
- `NetworkManager.NetworkConfig` null:
  - Runtime-created `NetworkManager` now creates a `NetworkConfig` before NGO startup.
- `SteamClient.Init already initialized`:
  - `SteamworksClientService` owns SteamClient lifetime.
  - Facepunch Transport now reuses an existing SteamClient instead of initializing it again.
- Camera lookup concerns:
  - Nameplate facing uses the camera transform injected through `NetworkPlayerRuntimeSettings`.
  - No `Camera.main` polling and no `Find` lookup in the nameplate update path.

## Next Session Priority
1. Capture both Host and Client logs from the same fresh zip.
2. Confirm remote client connects to Host after countdown.
3. Confirm both players see both nameplates.
4. Confirm owner-only input moves the correct player on both machines.
5. If remote player does not spawn, inspect `FacepunchTransport` connect/disconnect logs first.
6. If remote movement jitters, add a small send-rate limit or input change threshold before expanding gameplay sync.
