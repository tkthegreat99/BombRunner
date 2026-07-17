using System;
using System.Threading;
using BombRunner.Scripts.App;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Multiplayer;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalQuickMatchWaitingService : IDisposable
	{
		private readonly GameSettings gameSettings;
		private readonly LocalQuickMatchWaitingView waitingView;
		private readonly ISteamLobbyService steamLobbyService;
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private int participantCount;
		private bool isRunning;

		public LocalQuickMatchWaitingService(
			GameSettings gameSettings,
			LocalQuickMatchWaitingView waitingView,
			ISteamLobbyService steamLobbyService)
		{
			this.gameSettings = gameSettings;
			this.waitingView = waitingView;
			this.steamLobbyService = steamLobbyService;
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
				if (steamLobbyService != null && steamLobbyService.IsInLobby)
				{
					await WaitForSteamLobbyMatchStartAsync(linkedTokenSource.Token);
					return;
				}

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

		private async UniTask WaitForSteamLobbyMatchStartAsync(CancellationToken cancellationToken)
		{
			ShowSteamLobbyWaiting();

			if (steamLobbyService.IsLobbyOwner)
			{
				steamLobbyService.SetMatchState(SteamLobbyMatchState.Waiting);
				steamLobbyService.OpenInviteDialog();
				await DelaySecondsAsync(gameSettings.QuickMatchLocalJoinHoldSeconds, cancellationToken);
				await WaitForFriendAsync(cancellationToken);
				steamLobbyService.SetMatchState(SteamLobbyMatchState.Countdown);
				await RunCountdownAsync(cancellationToken);
				steamLobbyService.SetMatchState(SteamLobbyMatchState.Starting);
				waitingView.ShowStarting();
				await DelaySecondsAsync(gameSettings.QuickMatchStartHoldSeconds, cancellationToken);
				return;
			}

			while (steamLobbyService.IsInLobby
				&& steamLobbyService.MatchState == SteamLobbyMatchState.Waiting)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ShowSteamLobbyWaiting();
				await DelaySecondsAsync(0.25f, cancellationToken);
			}

			if (steamLobbyService.MatchState == SteamLobbyMatchState.Countdown)
			{
				await RunCountdownAsync(cancellationToken);
			}

			waitingView.ShowStarting();
			await DelaySecondsAsync(gameSettings.QuickMatchStartHoldSeconds, cancellationToken);
		}

		private async UniTask WaitForFriendAsync(CancellationToken cancellationToken)
		{
			while (steamLobbyService.IsInLobby && steamLobbyService.CurrentMemberCount < 2)
			{
				cancellationToken.ThrowIfCancellationRequested();
				ShowSteamLobbyWaiting();
				await DelaySecondsAsync(0.25f, cancellationToken);
			}
		}

		private void ShowSteamLobbyWaiting()
		{
			var maxMembers = steamLobbyService.MaxMembers > 0
				? steamLobbyService.MaxMembers
				: gameSettings.QuickMatchMaxParticipants;
			var memberCount = Mathf.Max(1, steamLobbyService.CurrentMemberCount);
			waitingView.ShowWaiting(memberCount, maxMembers);
		}
	}
}
