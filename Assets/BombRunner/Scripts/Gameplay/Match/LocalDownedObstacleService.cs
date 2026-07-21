using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	// 다운된 플레이어를 살아 있는 플레이어의 감속 장애물로 처리하는 서비스.
	public sealed class LocalDownedObstacleService : ITickable
	{
		private readonly GameBalanceSettings balanceSettings;
		private PlayerStateController[] players;
		private PlayerMovementController[] movementControllers;
		private float[,] nextStompTimes;
		private bool isInitialized;

		public LocalDownedObstacleService(GameBalanceSettings balanceSettings)
		{
			this.balanceSettings = balanceSettings;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 1;

			if (!isInitialized)
			{
				movementControllers = null;
				nextStompTimes = null;
				return;
			}

			movementControllers = new PlayerMovementController[players.Length];
			nextStompTimes = new float[players.Length, players.Length];

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player != null)
				{
					player.TryGetComponent(out movementControllers[i]);
				}
			}
		}

		public void Tick()
		{
			// 다운 플레이어별로 살아 있는 플레이어의 밟힘 판정 갱신.
			if (!isInitialized)
			{
				return;
			}

			for (var downedIndex = 0; downedIndex < players.Length; downedIndex++)
			{
				var downedPlayer = players[downedIndex];

				if (downedPlayer == null || !downedPlayer.IsDowned)
				{
					continue;
				}

				CheckAlivePlayersStompingDownedPlayer(downedIndex, downedPlayer);
			}
		}

		private void CheckAlivePlayersStompingDownedPlayer(int downedIndex, PlayerStateController downedPlayer)
		{
			for (var aliveIndex = 0; aliveIndex < players.Length; aliveIndex++)
			{
				if (aliveIndex == downedIndex || Time.time < nextStompTimes[downedIndex, aliveIndex])
				{
					continue;
				}

				var alivePlayer = players[aliveIndex];
				var movementController = movementControllers[aliveIndex];

				if (alivePlayer == null || movementController == null || !alivePlayer.IsAlive)
				{
					continue;
				}

				var offset = alivePlayer.transform.position - downedPlayer.transform.position;
				offset.y = 0f;

				if (offset.sqrMagnitude > balanceSettings.DownedStompRadiusSqr)
				{
					continue;
				}

				// Host/Master 확정 대상인 다운 플레이어 밟힘 판정.
				movementController.ApplyTemporarySlow(
					balanceSettings.DownedStompSlowDurationSeconds,
					balanceSettings.DownedStompSpeedMultiplier);
				nextStompTimes[downedIndex, aliveIndex] = Time.time + balanceSettings.DownedStompCooldownSeconds;
				Debug.Log($"Downed stomp: {alivePlayer.PlayerLabel} stepped on {downedPlayer.PlayerLabel}");
			}
		}
	}
}
