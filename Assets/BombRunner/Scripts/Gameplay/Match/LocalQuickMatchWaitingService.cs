using System;
using System.Threading;
using BombRunner.Scripts.App;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Multiplayer;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	// 로컬/Steam 빠른 대전 대기 인원과 카운트다운을 진행하는 서비스.
	// 로컬 모드에서는 더미 참가자를 채우고, Steam 모드에서는 Lobby owner가 Host처럼 시작 시점을 결정.
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
				// Steam Lobby가 있으면 로컬 더미 채우기를 건너뛰고 Lobby 메타데이터로 대기/카운트다운 상태를 맞춤.
				// 실제 플레이어 이동 동기화는 이 대기 단계 뒤의 NGO 부트스트랩에서 시작.
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
			// 참가 직후 UI가 빈 화면으로 남지 않도록 현재 Lobby 상태를 먼저 표시.
			ShowSteamLobbyWaiting();

			if (steamLobbyService.IsLobbyOwner)
			{
				// Host만 Steam Lobby 메타데이터를 쓴다. 클라이언트는 아래 polling 루프에서 상태를 읽기만 함.
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
				// Steam Lobby data changed callback이 늦거나 누락되어도 클라이언트 대기 화면이 최신 상태를 따라가도록 polling.
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
				// Host 쪽 멤버 변경 callback이 늦는 실제 친구 테스트 케이스를 보완하기 위한 명시 갱신.
				ShowSteamLobbyWaiting();
				await DelaySecondsAsync(0.25f, cancellationToken);
			}
		}

		private void ShowSteamLobbyWaiting()
		{
			// Host 쪽 Steam 멤버 변경 콜백이 늦어도 대기 화면과 시작 조건이 최신 Lobby 상태를 보도록 갱신.
			steamLobbyService.RefreshLobbyState();

			var maxMembers = steamLobbyService.MaxMembers > 0
				? steamLobbyService.MaxMembers
				: gameSettings.QuickMatchMaxParticipants;
			var memberCount = Mathf.Max(1, steamLobbyService.CurrentMemberCount);
			waitingView.ShowWaiting(memberCount, maxMembers);
		}
	}
}
