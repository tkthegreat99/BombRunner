using System;
using System.Threading;
using BombRunner.Scripts.Gameplay.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalTargetTossPrototype : ITickable, IDisposable
	{
		private const float TossDistanceSqr = 1.44f;
		private static readonly TimeSpan InvulnerableDuration = TimeSpan.FromSeconds(5);

		private readonly CancellationTokenSource cancellationTokenSource = new();
		private PlayerStateController localPlayer;
		private PlayerStateController dummyPlayer;
		private PlayerStateController currentTarget;
		private bool isInitialized;
		private bool wasTouching;

		// 임시 로컬 타겟 토스 검증용입니다. 네트워크 폭탄 AI/타겟 RPC가 들어오면 Host 권한 서비스로 교체합니다.
		public void Initialize(PlayerStateController localPlayer, PlayerStateController dummyPlayer)
		{
			this.localPlayer = localPlayer;
			this.dummyPlayer = dummyPlayer;
			currentTarget = localPlayer;
			isInitialized = localPlayer != null && dummyPlayer != null;
			wasTouching = false;

			if (!isInitialized)
			{
				return;
			}

			this.localPlayer.SetPlayerLabel("Local Player");
			this.dummyPlayer.SetPlayerLabel("Dummy Player");
			SetTarget(this.localPlayer);
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
			if (fromPlayer == null || toPlayer == null || toPlayer.IsInvulnerable)
			{
				return;
			}

			SetTarget(toPlayer);
			RunInvulnerableWindowAsync(fromPlayer, cancellationTokenSource.Token).Forget();
			Debug.Log($"Target toss: {fromPlayer.PlayerLabel} -> {toPlayer.PlayerLabel}, {fromPlayer.PlayerLabel} 5초 면역");
		}

		private void SetTarget(PlayerStateController target)
		{
			currentTarget = target;
			localPlayer.SetTarget(localPlayer == target);
			dummyPlayer.SetTarget(dummyPlayer == target);
		}

		private async UniTaskVoid RunInvulnerableWindowAsync(PlayerStateController player, CancellationToken cancellationToken)
		{
			player.SetInvulnerable(true);

			try
			{
				await UniTask.Delay(InvulnerableDuration, cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				if (player != null)
				{
					player.SetInvulnerable(false);
				}
			}
		}
	}
}
