using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	// 플레이어끼리 겹치지 않도록 로컬 separation을 적용하는 서비스.
	public sealed class LocalPlayerSeparationService : ITickable
	{
		private const float MinimumDistance = 0.001f;

		private readonly GameBalanceSettings balanceSettings;
		private PlayerStateController[] players;
		private CharacterController[] characterControllers;
		private bool isInitialized;

		public LocalPlayerSeparationService(GameBalanceSettings balanceSettings)
		{
			this.balanceSettings = balanceSettings;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 1;

			if (!isInitialized)
			{
				characterControllers = null;
				return;
			}

			characterControllers = new CharacterController[players.Length];

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null)
				{
					continue;
				}

				player.TryGetComponent(out characterControllers[i]);
			}
		}

		public void Tick()
		{
			if (!isInitialized)
			{
				return;
			}

			for (var i = 0; i < players.Length - 1; i++)
			{
				for (var j = i + 1; j < players.Length; j++)
				{
					SeparatePair(i, j);
				}
			}
		}

		private void SeparatePair(int firstIndex, int secondIndex)
		{
			var firstPlayer = players[firstIndex];
			var secondPlayer = players[secondIndex];

			if (firstPlayer == null || secondPlayer == null)
			{
				return;
			}

			var firstController = characterControllers[firstIndex];
			var secondController = characterControllers[secondIndex];

			if (firstController == null || secondController == null)
			{
				return;
			}

			var firstRadius = GetSeparationRadius(firstPlayer);
			var secondRadius = GetSeparationRadius(secondPlayer);
			var minimumDistance = firstRadius + secondRadius;
			var offset = secondPlayer.transform.position - firstPlayer.transform.position;
			offset.y = 0f;

			var distanceSqr = offset.sqrMagnitude;

			if (distanceSqr >= minimumDistance * minimumDistance)
			{
				return;
			}

			var direction = distanceSqr > MinimumDistance
				? offset / Mathf.Sqrt(distanceSqr)
				: GetFallbackDirection(firstIndex, secondIndex);
			var overlap = minimumDistance - Mathf.Sqrt(Mathf.Max(distanceSqr, MinimumDistance));
			var pushDistance = Mathf.Min(overlap, balanceSettings.SeparationStrength * Time.deltaTime);
			var firstWeight = GetPushWeight(firstPlayer);
			var secondWeight = GetPushWeight(secondPlayer);
			var totalWeight = firstWeight + secondWeight;

			if (totalWeight <= 0f)
			{
				return;
			}

			var firstMove = -direction * (pushDistance * (secondWeight / totalWeight));
			var secondMove = direction * (pushDistance * (firstWeight / totalWeight));

			firstController.Move(firstMove);
			secondController.Move(secondMove);
		}

		private float GetSeparationRadius(PlayerStateController player)
		{
			return player.IsDowned ? balanceSettings.DownedSeparationRadius : balanceSettings.AliveSeparationRadius;
		}

		private float GetPushWeight(PlayerStateController player)
		{
			return player.IsDowned ? balanceSettings.DownedSeparationPushWeight : 1f;
		}

		private Vector3 GetFallbackDirection(int firstIndex, int secondIndex)
		{
			var angle = (firstIndex * 97f + secondIndex * 53f) * Mathf.Deg2Rad;
			return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
		}
	}
}
