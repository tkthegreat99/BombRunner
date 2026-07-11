using System;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;

namespace BombRunner.Scripts.Bomb
{
	public sealed class BombTargetService
	{
		private readonly BombState bombState;
		private PlayerStateController localPlayer;
		private PlayerStateController dummyPlayer;
		private bool isInitialized;

		public event Action<PlayerStateController> TargetChanged;

		public PlayerStateController TargetPlayer => bombState.TargetPlayer;

		public BombTargetService(BombState bombState)
		{
			this.bombState = bombState;
		}

		// 임시 로컬 2인 검증용. 이후 Host/Master가 타겟 변경을 확정하고 클라이언트는 표시만 갱신.
		public void Initialize(PlayerStateController localPlayer, PlayerStateController dummyPlayer)
		{
			this.localPlayer = localPlayer;
			this.dummyPlayer = dummyPlayer;
			isInitialized = localPlayer != null && dummyPlayer != null;

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

			if (targetPlayer != localPlayer && targetPlayer != dummyPlayer)
			{
				return false;
			}

			if (!targetPlayer.IsAlive)
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

			var hasLocalCandidate = localPlayer != excludedPlayer && localPlayer.IsAlive;
			var hasDummyCandidate = dummyPlayer != excludedPlayer && dummyPlayer.IsAlive;

			if (!hasLocalCandidate && !hasDummyCandidate)
			{
				bombState.SetTargetPlayer(null);
				ApplyTargetFlags(null);
				TargetChanged?.Invoke(null);
				Debug.Log("Bomb target cleared: no alive target");
				return false;
			}

			if (hasLocalCandidate && hasDummyCandidate && UnityEngine.Random.value >= 0.5f)
			{
				return TrySetTarget(dummyPlayer);
			}

			if (hasLocalCandidate)
			{
				return TrySetTarget(localPlayer);
			}

			return TrySetTarget(dummyPlayer);
		}

		private void ApplyTargetFlags(PlayerStateController targetPlayer)
		{
			localPlayer.SetTarget(localPlayer == targetPlayer);
			dummyPlayer.SetTarget(dummyPlayer == targetPlayer);
		}
	}
}
