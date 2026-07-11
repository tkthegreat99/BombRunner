using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Player
{
	public sealed class PlayerSpawnService
	{
		private readonly IObjectResolver objectResolver;
		private readonly PlayerSpawnSettings spawnSettings;
		private GameObject localPlayer;
		private GameObject dummyPlayer;

		public PlayerSpawnService(IObjectResolver objectResolver, PlayerSpawnSettings spawnSettings)
		{
			this.objectResolver = objectResolver;
			this.spawnSettings = spawnSettings;
		}

		public GameObject SpawnLocalPlayer()
		{
			if (localPlayer != null)
			{
				return localPlayer;
			}

			if (spawnSettings.PlayerPrefab == null)
			{
				Debug.LogError("PlayerSpawnSettings에 플레이어 프리팹 연결이 필요합니다.");
				return null;
			}

			// 이번 단계는 로컬 검증용 Instantiate이며, 네트워크 스폰 전환 지점을 이 서비스로 제한한다.
			localPlayer = objectResolver.Instantiate(
				spawnSettings.PlayerPrefab,
				spawnSettings.SpawnPosition,
				Quaternion.identity);

			ApplySettings(localPlayer);
			ApplyPlayerState(localPlayer, "Local Player");
			SetInputEnabled(localPlayer, true);
			return localPlayer;
		}

		public GameObject SpawnDummyPlayer()
		{
			if (dummyPlayer != null)
			{
				return dummyPlayer;
			}

			if (spawnSettings.PlayerPrefab == null)
			{
				Debug.LogError("PlayerSpawnSettings에 플레이어 프리팹 연결이 필요합니다.");
				return null;
			}

			// 임시 2인 로컬 검증용 더미입니다. 실제 멀티플레이 스폰이 들어오면 Network Spawn 기반으로 교체합니다.
			var dummyPosition = spawnSettings.SpawnPosition + new Vector3(3f, 0f, 0f);
			dummyPlayer = objectResolver.Instantiate(
				spawnSettings.PlayerPrefab,
				dummyPosition,
				Quaternion.identity);
			dummyPlayer.name = "Dummy Player";

			ApplySettings(dummyPlayer);
			ApplyPlayerState(dummyPlayer, "Dummy Player");
			SetInputEnabled(dummyPlayer, false);
			return dummyPlayer;
		}

		private void ApplySettings(GameObject player)
		{
			if (player.TryGetComponent<PlayerMovementController>(out var movementController))
			{
				movementController.Initialize(spawnSettings.MoveSpeed, spawnSettings.RotationSpeed);
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
			stateController.SetAlive(true);
			stateController.SetMoving(false);
			stateController.SetDashing(false);
			stateController.SetTagImmune(false);
			stateController.SetTarget(false);
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
		}
	}
}
