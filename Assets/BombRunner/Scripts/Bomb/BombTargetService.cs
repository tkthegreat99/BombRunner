using System;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;

namespace BombRunner.Scripts.Bomb
{
	public sealed class BombTargetService
	{
		private readonly BombState bombState;
		private PlayerStateController[] players;
		private bool isInitialized;

		public event Action<PlayerStateController> TargetChanged;

		public PlayerStateController TargetPlayer => bombState.TargetPlayer;

		public BombTargetService(BombState bombState)
		{
			this.bombState = bombState;
		}

		// 임시 로컬 검증용. 이후 Host/Master가 타겟 변경을 확정하고 클라이언트는 표시만 갱신.
		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 0;

			if (!isInitialized)
			{
				return;
			}

			TrySetAnyAliveTarget(null);
		}

		public bool TrySetTarget(PlayerStateController targetPlayer)
		{
			if (!isInitialized || targetPlayer == null)
			{
				return false;
			}

			if (!ContainsPlayer(targetPlayer) || !targetPlayer.IsAlive)
			{
				return false;
			}

			if (bombState.TargetPlayer == targetPlayer)
			{
				ApplyTargetFlags(targetPlayer);
				return true;
			}

			bombState.SetTargetPlayer(targetPlayer);
			ApplyTargetFlags(targetPlayer);
			TargetChanged?.Invoke(targetPlayer);
			Debug.Log($"Bomb target changed: {targetPlayer.PlayerLabel}");
			return true;
		}

		public bool TrySetAnyAliveTarget(PlayerStateController excludedPlayer)
		{
			if (!isInitialized)
			{
				return false;
			}

			var candidateCount = CountAliveCandidates(excludedPlayer);

			if (candidateCount <= 0)
			{
				ClearTarget();
				return false;
			}

			var selectedIndex = UnityEngine.Random.Range(0, candidateCount);
			var currentIndex = 0;

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null || player == excludedPlayer || !player.IsAlive)
				{
					continue;
				}

				if (currentIndex == selectedIndex)
				{
					return TrySetTarget(player);
				}

				currentIndex++;
			}

			ClearTarget();
			return false;
		}

		public void ClearTarget()
		{
			if (!isInitialized)
			{
				return;
			}

			bombState.SetTargetPlayer(null);
			ApplyTargetFlags(null);
			TargetChanged?.Invoke(null);
			Debug.Log("Bomb target cleared");
		}

		private bool ContainsPlayer(PlayerStateController targetPlayer)
		{
			for (var i = 0; i < players.Length; i++)
			{
				if (players[i] == targetPlayer)
				{
					return true;
				}
			}

			return false;
		}

		private int CountAliveCandidates(PlayerStateController excludedPlayer)
		{
			var candidateCount = 0;

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null || player == excludedPlayer || !player.IsAlive)
				{
					continue;
				}

				candidateCount++;
			}

			return candidateCount;
		}

		private void ApplyTargetFlags(PlayerStateController targetPlayer)
		{
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null)
				{
					continue;
				}

				player.SetTarget(player == targetPlayer);
			}
		}
	}
}
