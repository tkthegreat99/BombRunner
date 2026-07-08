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
				Debug.LogError("PlayerSpawnSettings에 로컬 플레이어 프리팹이 연결되지 않았습니다.");
				return null;
			}

			// 이번 단계는 로컬 검증용 Instantiate이며, 네트워크 스폰 전환 지점을 이 서비스로 제한한다.
			localPlayer = objectResolver.Instantiate(
				spawnSettings.PlayerPrefab,
				spawnSettings.SpawnPosition,
				Quaternion.identity);

			ApplySettings(localPlayer);
			return localPlayer;
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
	}
}
