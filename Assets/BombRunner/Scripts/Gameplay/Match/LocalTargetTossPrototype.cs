using System;
using System.Threading;
using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalTargetTossPrototype : ITickable, IDisposable
	{
		private readonly BombTargetService bombTargetService;
		private readonly GameBalanceSettings balanceSettings;
		private readonly LocalMatchFeedbackView matchFeedbackView;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private PlayerStateController[] players;
		private bool isInitialized;
		private bool wasTouching;

		public LocalTargetTossPrototype(
			BombTargetService bombTargetService,
			GameBalanceSettings balanceSettings,
			LocalMatchFeedbackView matchFeedbackView,
			LocalPlayerCameraFollow cameraFollow)
		{
			this.bombTargetService = bombTargetService;
			this.balanceSettings = balanceSettings;
			this.matchFeedbackView = matchFeedbackView;
			this.cameraFollow = cameraFollow;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 0;
			wasTouching = false;
		}

		public void Tick()
		{
			if (!isInitialized)
			{
				return;
			}

			var currentTarget = bombTargetService.TargetPlayer;

			if (currentTarget == null || !currentTarget.IsAlive)
			{
				wasTouching = false;
				return;
			}

			for (var i = 0; i < players.Length; i++)
			{
				var candidate = players[i];

				if (candidate == null || candidate == currentTarget || !candidate.IsAlive)
				{
					continue;
				}

				if (!IsTouching(currentTarget, candidate))
				{
					continue;
				}

				if (wasTouching)
				{
					return;
				}

				TryTransferTarget(currentTarget, candidate);
				wasTouching = true;
				return;
			}

			wasTouching = false;
		}

		public void Dispose()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		private bool IsTouching(PlayerStateController fromPlayer, PlayerStateController toPlayer)
		{
			var offset = fromPlayer.transform.position - toPlayer.transform.position;
			offset.y = 0f;
			return offset.sqrMagnitude <= balanceSettings.TagDistanceSqr;
		}

		private bool TryTransferTarget(PlayerStateController fromPlayer, PlayerStateController toPlayer)
		{
			if (fromPlayer == null || toPlayer == null)
			{
				return false;
			}

			if (toPlayer.IsTagImmune)
			{
				if (matchFeedbackView != null)
				{
					matchFeedbackView.ShowTagImmuneRejected(
						toPlayer.transform,
						cameraFollow != null ? cameraFollow.transform : null);
				}

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

		private async UniTaskVoid RunTagImmuneWindowAsync(PlayerStateController player, CancellationToken cancellationToken)
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
	}
}
