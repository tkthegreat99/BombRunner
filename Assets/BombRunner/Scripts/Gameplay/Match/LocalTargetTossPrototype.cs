using System;
using System.Threading;
using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Gameplay.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalTargetTossPrototype : ITickable, IDisposable
	{
		private const float TossDistanceSqr = 1.44f;
		private static readonly TimeSpan TagImmuneDuration = TimeSpan.FromSeconds(3);

		private readonly BombTargetService bombTargetService;
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private PlayerStateController localPlayer;
		private PlayerStateController dummyPlayer;
		private bool isInitialized;
		private bool wasTouching;

		public LocalTargetTossPrototype(BombTargetService bombTargetService)
		{
			this.bombTargetService = bombTargetService;
		}

		// 임시 로컬 태그 검증용. 이후 타겟 변경 확정은 Host/Master 권한 서비스와 RPC로 이동.
		public void Initialize(PlayerStateController localPlayer, PlayerStateController dummyPlayer)
		{
			this.localPlayer = localPlayer;
			this.dummyPlayer = dummyPlayer;
			isInitialized = localPlayer != null && dummyPlayer != null;
			wasTouching = false;

			if (!isInitialized)
			{
				return;
			}

			this.localPlayer.SetPlayerLabel("Local Player");
			this.dummyPlayer.SetPlayerLabel("Dummy Player");
		}

		public void Tick()
		{
			if (!isInitialized || localPlayer == null || dummyPlayer == null)
			{
				return;
			}

			if (!localPlayer.IsAlive || !dummyPlayer.IsAlive)
			{
				return;
			}

			var offset = localPlayer.transform.position - dummyPlayer.transform.position;
			offset.y = 0f;
			var isTouching = offset.sqrMagnitude <= TossDistanceSqr;

			if (!isTouching)
			{
				wasTouching = false;
				return;
			}

			if (wasTouching)
			{
				return;
			}

			wasTouching = true;

			var currentTarget = bombTargetService.TargetPlayer;

			if (currentTarget == localPlayer)
			{
				TryTransferTarget(localPlayer, dummyPlayer);
			}
			else if (currentTarget == dummyPlayer)
			{
				TryTransferTarget(dummyPlayer, localPlayer);
			}
		}

		public void Dispose()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		private void TryTransferTarget(PlayerStateController fromPlayer, PlayerStateController toPlayer)
		{
			if (fromPlayer == null || toPlayer == null || toPlayer.IsTagImmune)
			{
				return;
			}

			if (!bombTargetService.TrySetTarget(toPlayer))
			{
				return;
			}

			RunTagImmuneWindowAsync(fromPlayer, cancellationTokenSource.Token).Forget();
			Debug.Log($"Target toss: {fromPlayer.PlayerLabel} -> {toPlayer.PlayerLabel}, {fromPlayer.PlayerLabel} 3초 태그 면역");
		}

		private async UniTaskVoid RunTagImmuneWindowAsync(PlayerStateController player, CancellationToken cancellationToken)
		{
			player.SetTagImmune(true);

			try
			{
				await UniTask.Delay(TagImmuneDuration, cancellationToken: cancellationToken);
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
	}
}
