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

		// 지금은 로컬 2인 검증용입니다. 이후 Host/Master가 타겟 변경을 확정하고 클라이언트는 표시만 갱신합니다.
		public void Initialize(PlayerStateController localPlayer, PlayerStateController dummyPlayer)
		{
			this.localPlayer = localPlayer;
			this.dummyPlayer = dummyPlayer;
			isInitialized = localPlayer != null && dummyPlayer != null;

			if (!isInitialized)
			{
				return;
			}

			TrySetTarget(this.localPlayer);
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

		private void ApplyTargetFlags(PlayerStateController targetPlayer)
		{
			localPlayer.SetTarget(localPlayer == targetPlayer);
			dummyPlayer.SetTarget(dummyPlayer == targetPlayer);
		}
	}
}
