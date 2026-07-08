using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.Bomb
{
	public sealed class BombSpawnService
	{
		private readonly IObjectResolver objectResolver;
		private readonly BombSpawnSettings bombSpawnSettings;
		private readonly BombState bombState;
		private readonly BombTargetService bombTargetService;
		private GameObject bombInstance;

		public BombSpawnService(
			IObjectResolver objectResolver,
			BombSpawnSettings bombSpawnSettings,
			BombState bombState,
			BombTargetService bombTargetService)
		{
			this.objectResolver = objectResolver;
			this.bombSpawnSettings = bombSpawnSettings;
			this.bombState = bombState;
			this.bombTargetService = bombTargetService;
		}

		// 임시 로컬 검증용 폭탄 스폰입니다. 이후 이 Prefab을 네트워크 스폰/풀링 대상으로 교체합니다.
		public GameObject SpawnLocalBomb(PlayerStateController localPlayer, PlayerStateController dummyPlayer)
		{
			if (bombInstance != null)
			{
				return bombInstance;
			}

			if (localPlayer == null || dummyPlayer == null)
			{
				return null;
			}

			if (bombSpawnSettings.BombPrefab == null)
			{
				Debug.LogError("BombSpawnSettings에 BombPrefab이 연결되어 있지 않습니다.");
				return null;
			}

			var spawnPosition = (localPlayer.transform.position + dummyPlayer.transform.position) * 0.5f;
			spawnPosition.y = 0.6f;

			bombInstance = objectResolver.Instantiate(bombSpawnSettings.BombPrefab, spawnPosition, Quaternion.identity);
			bombInstance.name = "Local Prototype Bomb";

			if (!bombInstance.TryGetComponent<BombCreatureController>(out var controller))
			{
				Debug.LogError("BombPrefab에 BombCreatureController가 필요합니다.");
				return bombInstance;
			}

			controller.Initialize(bombState, bombTargetService, localPlayer, dummyPlayer);
			return bombInstance;
		}
	}
}
