using System;
using BombRunner.Scripts.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Player
{
	// 로컬 플레이어, 더미, 이전 Steam 수동 플레이어 스폰을 담당하는 서비스.
	public sealed class PlayerSpawnService
	{
		private readonly IObjectResolver objectResolver;
		private readonly PlayerSpawnSettings spawnSettings;
		private readonly GameBalanceSettings balanceSettings;
		private readonly GameObject[] dummyPlayers = new GameObject[2];
		private GameObject[] steamPlayers = Array.Empty<GameObject>();
		private GameObject localPlayer;

		public PlayerSpawnService(
			IObjectResolver objectResolver,
			PlayerSpawnSettings spawnSettings,
			GameBalanceSettings balanceSettings)
		{
			this.objectResolver = objectResolver;
			this.spawnSettings = spawnSettings;
			this.balanceSettings = balanceSettings;
		}

		public GameObject SpawnLocalPlayer()
		{
			if (localPlayer != null)
			{
				return localPlayer;
			}

			if (spawnSettings.PlayerPrefab == null)
			{
				Debug.LogError("PlayerSpawnSettings에 PlayerPrefab 연결이 필요합니다.");
				return null;
			}

			// 임시 로컬 검증용 Instantiate. 이후 네트워크 스폰 서비스로 교체.
			localPlayer = objectResolver.Instantiate(
				spawnSettings.PlayerPrefab,
				spawnSettings.SpawnPosition,
				Quaternion.identity);

			ApplySettings(localPlayer);
			ApplyPlayerState(localPlayer, "Local Player");
			SetInputEnabled(localPlayer, true);
			return localPlayer;
		}

		public GameObject[] SpawnDummyPlayers()
		{
			if (dummyPlayers[0] != null && dummyPlayers[1] != null)
			{
				return dummyPlayers;
			}

			if (spawnSettings.PlayerPrefab == null)
			{
				Debug.LogError("PlayerSpawnSettings에 PlayerPrefab 연결이 필요합니다.");
				return dummyPlayers;
			}

			SpawnDummyPlayer(0, spawnSettings.SpawnPosition + new Vector3(3f, 0f, 0f), "Dummy Player 1");
			SpawnDummyPlayer(1, spawnSettings.SpawnPosition + new Vector3(-3f, 0f, 0f), "Dummy Player 2");
			return dummyPlayers;
		}

		public GameObject[] SpawnSteamLobbyPlayers(ulong[] memberSteamIds, ulong localSteamId)
		{
			// NGO 이전 수동 Steam 로비 스폰 경로. 이후 네트워크 스폰으로 축소 대상.
			if (memberSteamIds == null || memberSteamIds.Length == 0)
			{
				return Array.Empty<GameObject>();
			}

			if (steamPlayers.Length == memberSteamIds.Length && steamPlayers.Length > 0)
			{
				return steamPlayers;
			}

			if (spawnSettings.PlayerPrefab == null)
			{
				Debug.LogError("PlayerSpawnSettings PlayerPrefab is missing.");
				return Array.Empty<GameObject>();
			}

			steamPlayers = new GameObject[memberSteamIds.Length];

			for (var i = 0; i < memberSteamIds.Length; i++)
			{
				var memberSteamId = memberSteamIds[i];
				var isLocalPlayer = memberSteamId == localSteamId;
				var playerLabel = isLocalPlayer ? "Local Player" : $"Steam Player {i + 1}";
				var player = objectResolver.Instantiate(
					spawnSettings.PlayerPrefab,
					GetSteamSpawnPosition(i, memberSteamIds.Length),
					Quaternion.identity);

				player.name = playerLabel;
				ApplySettings(player);
				ApplyPlayerState(player, playerLabel);
				ApplyNetworkIdentity(player, memberSteamId);
				SetInputEnabled(player, isLocalPlayer);

				if (isLocalPlayer)
				{
					localPlayer = player;
				}

				steamPlayers[i] = player;
			}

			return steamPlayers;
		}

		private void SpawnDummyPlayer(int index, Vector3 spawnPosition, string playerLabel)
		{
			if (dummyPlayers[index] != null)
			{
				return;
			}

			// 임시 로컬 검증용 더미. 이후 실제 멀티플레이 스폰은 Network Spawn 기반으로 교체.
			var dummyPlayer = objectResolver.Instantiate(
				spawnSettings.PlayerPrefab,
				spawnPosition,
				Quaternion.identity);
			dummyPlayer.name = playerLabel;

			ApplySettings(dummyPlayer);
			ApplyPlayerState(dummyPlayer, playerLabel);
			SetInputEnabled(dummyPlayer, false);
			dummyPlayers[index] = dummyPlayer;
		}

		private Vector3 GetSteamSpawnPosition(int index, int playerCount)
		{
			if (playerCount <= 1)
			{
				return spawnSettings.SpawnPosition;
			}

			var angle = Mathf.PI * 2f * index / playerCount;
			var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3f;
			return spawnSettings.SpawnPosition + offset;
		}

		private void ApplySettings(GameObject player)
		{
			// ScriptableObject 스폰 설정을 플레이어 컨트롤러에 주입.
			if (player.TryGetComponent<PlayerMovementController>(out var movementController))
			{
				movementController.Initialize(
					spawnSettings.MoveSpeed,
					spawnSettings.RotationSpeed,
					balanceSettings.DownedMoveSpeedMultiplier);
			}

			if (player.TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashController.Initialize(
					spawnSettings.DashDistance,
					spawnSettings.DashDuration,
					spawnSettings.DashCooldown);
			}

		}

		private void ApplyPlayerState(GameObject player, string playerLabel)
		{
			// 새로 생성된 플레이어의 매치 상태 초기화.
			if (!player.TryGetComponent<PlayerStateController>(out var stateController))
			{
				return;
			}

			stateController.SetPlayerLabel(playerLabel);
			stateController.SetLifeState(PlayerLifeState.Alive);
			stateController.SetMoving(false);
			stateController.SetDashing(false);
			stateController.SetTagImmune(false);
			stateController.SetTarget(false);
			stateController.SetTaunting(false);
			stateController.SetDashLocked(false);
		}

		private void ApplyNetworkIdentity(GameObject player, ulong steamId)
		{
			if (!player.TryGetComponent<PlayerNetworkIdentity>(out var networkIdentity))
			{
				networkIdentity = player.AddComponent<PlayerNetworkIdentity>();
			}

			networkIdentity.SetSteamId(steamId);
		}

		private void SetInputEnabled(GameObject player, bool isInputEnabled)
		{
			if (player.TryGetComponent<PlayerMovementController>(out var movementController))
			{
				movementController.SetInputEnabled(isInputEnabled);
			}

			if (player.TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashController.SetInputEnabled(isInputEnabled);
			}

			if (player.TryGetComponent<PlayerTauntController>(out var tauntController))
			{
				tauntController.SetInputEnabled(isInputEnabled);
			}
		}
	}
}
