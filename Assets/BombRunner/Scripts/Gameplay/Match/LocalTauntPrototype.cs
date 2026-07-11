using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalTauntPrototype : ITickable
	{
		private readonly BombSpawnService bombSpawnService;
		private readonly BombTargetService bombTargetService;
		private readonly GameBalanceSettings balanceSettings;
		private PlayerStateController[] players;
		private float[] tauntHoldTimes;
		private bool[] bombRiskTriggered;
		private bool isInitialized;

		public LocalTauntPrototype(
			BombSpawnService bombSpawnService,
			BombTargetService bombTargetService,
			GameBalanceSettings balanceSettings)
		{
			this.bombSpawnService = bombSpawnService;
			this.bombTargetService = bombTargetService;
			this.balanceSettings = balanceSettings;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 0;

			if (!isInitialized)
			{
				tauntHoldTimes = null;
				bombRiskTriggered = null;
				return;
			}

			tauntHoldTimes = new float[players.Length];
			bombRiskTriggered = new bool[players.Length];
		}

		public void Tick()
		{
			if (!isInitialized)
			{
				return;
			}

			ClearAreaDashLocks();

			for (var i = 0; i < players.Length; i++)
			{
				var taunter = players[i];

				if (taunter == null || !taunter.IsAlive || !taunter.IsTaunting)
				{
					tauntHoldTimes[i] = 0f;
					bombRiskTriggered[i] = false;
					continue;
				}

				tauntHoldTimes[i] += Time.deltaTime;
				ApplyDashLockArea(taunter);
				TryTriggerBombRisk(i, taunter);
			}
		}

		private void ClearAreaDashLocks()
		{
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player != null)
				{
					player.SetDashLocked(false);
				}
			}
		}

		private void ApplyDashLockArea(PlayerStateController taunter)
		{
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null || !player.IsAlive)
				{
					continue;
				}

				var offset = player.transform.position - taunter.transform.position;
				offset.y = 0f;

				if (offset.sqrMagnitude <= balanceSettings.TauntRadiusSqr)
				{
					player.SetDashLocked(true);
				}
			}
		}

		private void TryTriggerBombRisk(int taunterIndex, PlayerStateController taunter)
		{
			if (bombRiskTriggered[taunterIndex]
				|| tauntHoldTimes[taunterIndex] < balanceSettings.TauntBombRiskHoldSeconds
				|| bombTargetService.TargetPlayer == taunter)
			{
				return;
			}

			var currentBomb = bombSpawnService.CurrentController;

			if (currentBomb == null)
			{
				return;
			}

			var offset = currentBomb.transform.position - taunter.transform.position;
			offset.y = 0f;

			if (offset.sqrMagnitude > balanceSettings.TauntBombRiskDistanceSqr)
			{
				return;
			}

			bombRiskTriggered[taunterIndex] = true;
			bombTargetService.TrySetTarget(taunter);
			Debug.Log($"Taunt risk: bomb target changed to {taunter.PlayerLabel}");
		}
	}
}
