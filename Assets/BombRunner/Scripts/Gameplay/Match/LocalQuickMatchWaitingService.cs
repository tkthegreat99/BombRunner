using System;
using System.Threading;
using BombRunner.Scripts.App;
using BombRunner.Scripts.Gameplay.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalQuickMatchWaitingService : IDisposable
	{
		private readonly GameSettings gameSettings;
		private readonly LocalQuickMatchWaitingView waitingView;
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private int participantCount;
		private bool isRunning;

		public LocalQuickMatchWaitingService(
			GameSettings gameSettings,
			LocalQuickMatchWaitingView waitingView)
		{
			this.gameSettings = gameSettings;
			this.waitingView = waitingView;
		}

		public async UniTask WaitForMatchStartAsync(
			PlayerStateController[] localPlayableActors,
			CancellationToken cancellationToken)
		{
			if (isRunning)
			{
				return;
			}

			isRunning = true;
			participantCount = 0;
			var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
				cancellationTokenSource.Token,
				cancellationToken);

			try
			{
				// 로컬 프로토타입에서는 이 서비스가 Host 역할로 참가자 수와 시작 확정을 판정하는 임시 권한 경계.
				AddLocalParticipant(localPlayableActors);
				await DelaySecondsAsync(gameSettings.QuickMatchLocalJoinHoldSeconds, linkedTokenSource.Token);

				while (participantCount < gameSettings.QuickMatchMaxParticipants)
				{
					AddDummyParticipant();
					await DelaySecondsAsync(gameSettings.QuickMatchDummyJoinIntervalSeconds, linkedTokenSource.Token);
				}

				await RunCountdownAsync(linkedTokenSource.Token);
				waitingView.ShowStarting();
				await DelaySecondsAsync(gameSettings.QuickMatchStartHoldSeconds, linkedTokenSource.Token);
			}
			catch (OperationCanceledException) when (linkedTokenSource.IsCancellationRequested)
			{
			}
			finally
			{
				waitingView.Hide();
				linkedTokenSource.Dispose();
				isRunning = false;
			}
		}

		public void Dispose()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		private void AddLocalParticipant(PlayerStateController[] localPlayableActors)
		{
			participantCount = 1;
			waitingView.ShowWaiting(participantCount, gameSettings.QuickMatchMaxParticipants);

			var actorCount = 0;

			if (localPlayableActors != null)
			{
				for (var i = 0; i < localPlayableActors.Length; i++)
				{
					if (localPlayableActors[i] != null)
					{
						actorCount++;
					}
				}
			}

			Debug.Log($"Quick match waiting: local prototype actors ready {actorCount}. Match records are not updated in waiting.");
		}

		private void AddDummyParticipant()
		{
			participantCount = Mathf.Min(gameSettings.QuickMatchMaxParticipants, participantCount + 1);
			waitingView.ShowWaiting(participantCount, gameSettings.QuickMatchMaxParticipants);
		}

		private async UniTask RunCountdownAsync(CancellationToken cancellationToken)
		{
			for (var seconds = gameSettings.QuickMatchCountdownSeconds; seconds > 0; seconds--)
			{
				cancellationToken.ThrowIfCancellationRequested();
				waitingView.ShowCountdown(seconds);
				await DelaySecondsAsync(1f, cancellationToken);
			}
		}

		private async UniTask DelaySecondsAsync(float seconds, CancellationToken cancellationToken)
		{
			var delay = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
			await UniTask.Delay(delay, cancellationToken: cancellationToken);
		}
	}
}
