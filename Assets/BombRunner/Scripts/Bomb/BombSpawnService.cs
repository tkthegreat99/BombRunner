using BombRunner.Scripts.Data;
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
		private readonly GameBalanceSettings balanceSettings;
		private readonly BombState bombState;
		private readonly BombTargetService bombTargetService;
		private GameObject bombInstance;

		public BombCreatureController CurrentController =>
			bombInstance != null && bombInstance.TryGetComponent<BombCreatureController>(out var controller)
				? controller
				: null;

		public BombSpawnService(
			IObjectResolver objectResolver,
			BombSpawnSettings bombSpawnSettings,
			GameBalanceSettings balanceSettings,
			BombState bombState,
			BombTargetService bombTargetService)
		{
			this.objectResolver = objectResolver;
			this.bombSpawnSettings = bombSpawnSettings;
			this.balanceSettings = balanceSettings;
			this.bombState = bombState;
			this.bombTargetService = bombTargetService;
		}

		public BombCreatureController SpawnLocalBomb(PlayerStateController[] players)
		{
			return SpawnLocalBomb(players, GetLocalBombSpawnPosition(players));
		}

		public BombCreatureController SpawnLocalBomb(PlayerStateController[] players, Vector3 spawnPosition)
		{
			if (bombInstance != null)
			{
				return bombInstance.TryGetComponent<BombCreatureController>(out var currentController) ? currentController : null;
			}

			if (players == null || players.Length == 0)
			{
				return null;
			}

			if (bombSpawnSettings.BombPrefab == null)
			{
				Debug.LogError("BombSpawnSettings에 BombPrefab 연결이 필요합니다.");
				return null;
			}

			bombInstance = objectResolver.Instantiate(bombSpawnSettings.BombPrefab, spawnPosition, Quaternion.identity);
			bombInstance.name = "Local Prototype Bomb";

			if (!bombInstance.TryGetComponent<BombCreatureController>(out var controller))
			{
				Debug.LogError("BombPrefab에 BombCreatureController가 필요합니다.");
				return null;
			}

			controller.Initialize(bombState, bombTargetService, balanceSettings, players);
			return controller;
		}

		public Vector3 GetLocalBombSpawnPosition(PlayerStateController[] players)
		{
			return GetSpawnPosition(players);
		}

		public void DespawnLocalBomb(BombCreatureController controller)
		{
			if (controller == null || bombInstance == null)
			{
				return;
			}

			if (controller.gameObject != bombInstance)
			{
				return;
			}

			// 임시 로컬 제거. 이후 Bomb 풀 반환 또는 네트워크 디스폰으로 교체.
			Object.Destroy(bombInstance);
			bombInstance = null;
		}

		private Vector3 GetSpawnPosition(PlayerStateController[] players)
		{
			var aliveCount = 0;
			var spawnPosition = Vector3.zero;
			var fallbackPosition = Vector3.zero;
			var fallbackCount = 0;

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null)
				{
					continue;
				}

				fallbackPosition += player.transform.position;
				fallbackCount++;

				if (!player.IsAlive)
				{
					continue;
				}

				spawnPosition += player.transform.position;
				aliveCount++;
			}

			if (aliveCount > 0)
			{
				spawnPosition /= aliveCount;
			}
			else if (fallbackCount > 0)
			{
				spawnPosition = fallbackPosition / fallbackCount;
			}

			spawnPosition.y = 0.6f;
			return spawnPosition;
		}
	}
}
