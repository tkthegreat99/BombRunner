using BombRunner.Scripts.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Player
{
	public sealed class PlayerSpawnService
	{
		private readonly IObjectResolver objectResolver;
		private readonly PlayerSpawnSettings spawnSettings;
		private readonly GameBalanceSettings balanceSettings;
		private readonly GameObject[] dummyPlayers = new GameObject[2];
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

		private void ApplySettings(GameObject player)
		{
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
