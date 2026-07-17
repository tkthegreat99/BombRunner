using System;
using System.Threading;
using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Items;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Multiplayer;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Authority
{
	public sealed class LocalMatchAuthorityService : IMatchAuthorityService, IDisposable
	{
		private readonly BombTargetService bombTargetService;
		private readonly GameBalanceSettings balanceSettings;
		private readonly IMatchNetworkSessionService networkSessionService;
		private readonly CancellationTokenSource cancellationTokenSource = new();

		public LocalMatchAuthorityService(
			BombTargetService bombTargetService,
			GameBalanceSettings balanceSettings,
			IMatchNetworkSessionService networkSessionService)
		{
			this.bombTargetService = bombTargetService;
			this.balanceSettings = balanceSettings;
			this.networkSessionService = networkSessionService;
		}

		public bool TryTransferTarget(PlayerStateController fromPlayer, PlayerStateController toPlayer)
		{
			// Host/Master 확정 대상: 타겟 전달 승인.
			if (!CanConfirmAuthority() || fromPlayer == null || toPlayer == null || toPlayer.IsTagImmune)
			{
				return false;
			}

			if (!bombTargetService.TrySetTarget(toPlayer))
			{
				return false;
			}

			RunTagImmuneWindowAsync(fromPlayer, cancellationTokenSource.Token).Forget();
			Debug.Log($"Target toss: {fromPlayer.PlayerLabel} -> {toPlayer.PlayerLabel}, tag immune {balanceSettings.TagImmuneDurationSeconds:0.00}s");
			return true;
		}

		public bool TrySetBombTarget(PlayerStateController player)
		{
			// Host/Master 확정 대상: 폭탄 타겟 지정.
			if (!CanConfirmAuthority())
			{
				return false;
			}

			return bombTargetService.TrySetTarget(player);
		}

		public bool TrySetAnyAliveBombTarget(PlayerStateController[] players, PlayerStateController excludedPlayer)
		{
			// Host/Master 확정 대상: 다음 폭탄 타겟 무작위 선택.
			if (!CanConfirmAuthority() || players == null || players.Length <= 0)
			{
				return false;
			}

			var candidateCount = CountAliveTargetCandidates(players, excludedPlayer);

			if (candidateCount <= 0)
			{
				ClearBombTarget();
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
					return bombTargetService.TrySetTarget(player);
				}

				currentIndex++;
			}

			ClearBombTarget();
			return false;
		}

		public void ClearBombTarget()
		{
			// Host/Master 확정 대상: 폭탄 타겟 해제.
			if (!CanConfirmAuthority())
			{
				return;
			}

			bombTargetService.ClearTarget();
		}

		public float ResolveBombPhaseDuration(BombTimerPhase timerPhase)
		{
			// Host/Master 확정 대상: 폭탄 페이즈 지속시간 난수.
			if (!CanConfirmAuthority())
			{
				return 0f;
			}

			var range = balanceSettings.GetDurationRange(timerPhase);
			return UnityEngine.Random.Range(range.x, range.y);
		}

		public PlayerStateController ResolveExplosionVictim(
			Vector3 bombPosition,
			PlayerStateController[] players,
			float radius)
		{
			// Host/Master 확정 대상: 폭발 피해자 선택.
			if (!CanConfirmAuthority() || players == null || players.Length <= 0 || radius <= 0f)
			{
				return null;
			}

			var radiusSqr = radius * radius;
			var closestPlayer = default(PlayerStateController);
			var closestDistanceSqr = float.MaxValue;

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null || !player.IsAlive)
				{
					continue;
				}

				var offset = player.transform.position - bombPosition;
				offset.y = 0f;
				var distanceSqr = offset.sqrMagnitude;

				if (distanceSqr > radiusSqr || distanceSqr >= closestDistanceSqr)
				{
					continue;
				}

				closestDistanceSqr = distanceSqr;
				closestPlayer = player;
			}

			return closestPlayer;
		}

		public bool SetPlayerDowned(PlayerStateController player)
		{
			// Host/Master 확정 대상: 플레이어 다운 상태 적용.
			if (!CanConfirmAuthority() || player == null || !player.IsAlive)
			{
				return false;
			}

			player.SetDowned();

			if (player.TryGetComponent<PlayerItemHolder>(out var holder))
			{
				holder.Clear();
			}

			return true;
		}

		public bool TryPickupItem(PlayerStateController player, PlayerItemHolder holder, ItemType itemType)
		{
			// Host/Master 확정 대상: 아이템 획득 승인.
			if (!CanConfirmAuthority() || player == null || holder == null || !player.IsAlive)
			{
				return false;
			}

			return holder.TryPickup(itemType);
		}

		public bool TryThrowItem(PlayerStateController owner, ItemType itemType, Vector3 direction)
		{
			// Host/Master 확정 대상: 아이템 투척 승인.
			if (!CanConfirmAuthority() || owner == null || !owner.IsAlive || itemType == ItemType.None || direction.sqrMagnitude <= 0.0001f)
			{
				return false;
			}

			if (!owner.TryGetComponent<PlayerItemHolder>(out var holder) || holder.HeldItem != itemType)
			{
				return false;
			}

			return holder.TryConsume(out _);
		}

		public bool ApplyItemHit(ItemType itemType, PlayerStateController target)
		{
			// Host/Master 확정 대상: 아이템 피격 효과 적용.
			if (!CanConfirmAuthority() || target == null || !target.IsAlive)
			{
				return false;
			}

			switch (itemType)
			{
				case ItemType.Slow:
					if (target.TryGetComponent<PlayerMovementController>(out var movementController))
					{
						movementController.ApplyTemporarySlow(
							balanceSettings.SlowItemDurationSeconds,
							balanceSettings.SlowItemSpeedMultiplier);
					}
					return true;
				case ItemType.Stun:
					target.SetStunned(true, balanceSettings.StunItemDurationSeconds);
					return true;
				default:
					return false;
			}
		}

		public bool ApplyTauntRisk(PlayerStateController taunter)
		{
			// Host/Master 확정 대상: 도발 리스크 타겟 변경.
			return TrySetBombTarget(taunter);
		}

		public void Dispose()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		private async UniTaskVoid RunTagImmuneWindowAsync(
			PlayerStateController player,
			CancellationToken cancellationToken)
		{
			player.SetTagImmune(true, balanceSettings.TagImmuneDurationSeconds);

			try
			{
				var delay = TimeSpan.FromSeconds(balanceSettings.TagImmuneDurationSeconds);
				await UniTask.Delay(delay, cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				if (player != null)
				{
					player.SetTagImmune(false);
				}
			}
		}

		private bool CanConfirmAuthority()
		{
			return networkSessionService == null || networkSessionService.IsHostAuthority;
		}

		private int CountAliveTargetCandidates(PlayerStateController[] players, PlayerStateController excludedPlayer)
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
	}
}
