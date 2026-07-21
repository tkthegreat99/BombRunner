using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Multiplayer
{
	// Facepunch Steam Lobby를 친구 초대와 빠른 대전 대기장으로 사용하는 서비스.
	public sealed class SteamLobbyService : ISteamLobbyService, IStartable, IDisposable
	{
		private const string LobbyNameKey = "name";
		private const string MatchStateKey = "match_state";

		private readonly ISteamworksClientService steamworksClientService;
		private Lobby currentLobby;

		public event Action Changed;

		public bool IsAvailable => SteamClient.IsValid;
		public bool IsInLobby { get; private set; }
		public bool IsLobbyOwner { get; private set; }
		public ulong CurrentLobbyId { get; private set; }
		public int CurrentMemberCount { get; private set; }
		public int MaxMembers { get; private set; }
		public string MatchState { get; private set; } = SteamLobbyMatchState.Waiting;

		public SteamLobbyService(ISteamworksClientService steamworksClientService)
		{
			this.steamworksClientService = steamworksClientService;
		}

		public void Start()
		{
			// Facepunch Steam callback 구독.
			SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
			SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberChanged;
			SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberChanged;
			SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
			SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
			TryJoinCommandLineLobbyAsync().Forget();
		}

		public async UniTask<bool> CreateQuickMatchLobbyAsync(int maxMembers, CancellationToken cancellationToken)
		{
			// Host가 Enter/Space로 빠른 대전을 시작할 때 호출되는 진입점.
			// Lobby는 친구 초대와 매치 상태 공유만 담당하고, 실제 플레이어 동기화는 이후 NGO 세션에서 처리.
			// 현재 친구 테스트용 friends-only quick match lobby 생성.
			if (!IsAvailable)
			{
				return false;
			}

			if (IsInLobby)
			{
				return true;
			}

			MaxMembers = Mathf.Max(1, maxMembers);
			MatchState = SteamLobbyMatchState.Waiting;
			var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(MaxMembers);

			if (cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			if (!lobbyResult.HasValue)
			{
				Debug.LogWarning("Facepunch Steam lobby creation failed.");
				return false;
			}

			currentLobby = lobbyResult.Value;
			currentLobby.SetFriendsOnly();
			currentLobby.SetJoinable(true);
			// Steam 친구 목록과 초대 overlay에서 알아볼 수 있도록 최소한의 Lobby metadata 기록.
			currentLobby.SetData(LobbyNameKey, $"{steamworksClientService.PersonaName}'s Boom Runner Lobby");
			currentLobby.SetData(MatchStateKey, SteamLobbyMatchState.Waiting);
			ApplyLobbyState(currentLobby, true);
			Debug.Log($"Facepunch Steam lobby created: {CurrentLobbyId}");
			return true;
		}

		public async UniTask<bool> JoinLobbyAsync(ulong lobbyId, CancellationToken cancellationToken)
		{
			// Steam 초대 수락 또는 "+connect_lobby <id>" 실행 인자를 통해 들어오는 Client 참가 경로.
			// Join 결과는 참가자 본인에게 먼저 반영되므로, Host와 Client의 멤버 수 표시가 잠시 다를 수 있음.
			// 초대 또는 command line으로 전달된 lobbyId 참가.
			if (!IsAvailable || lobbyId == 0)
			{
				return false;
			}

			var lobbyResult = await SteamMatchmaking.JoinLobbyAsync(lobbyId);

			if (cancellationToken.IsCancellationRequested)
			{
				return false;
			}

			if (!lobbyResult.HasValue)
			{
				Debug.LogWarning($"Facepunch Steam lobby join failed: {lobbyId}");
				return false;
			}

			currentLobby = lobbyResult.Value;
			// Host 쪽 Lobby member callback이 늦는 환경을 줄이기 위한 작은 member data 갱신 신호.
			currentLobby.SetMemberData("joined", steamworksClientService.LocalSteamId.ToString());
			ApplyLobbyState(currentLobby, true);
			Debug.Log($"Facepunch Steam lobby joined: {CurrentLobbyId}, members={CurrentMemberCount}");
			return true;
		}

		public ulong GetLobbyOwnerSteamId()
		{
			// NGO Client가 FacepunchTransport.targetSteamId로 사용할 Host Steam ID 조회.
			// 초대 직후 owner 정보가 stale일 수 있어 읽기 직전에 상태 갱신.
			RefreshLobbyState();
			return IsInLobby ? currentLobby.Owner.Id.Value : 0;
		}

		public ulong[] GetLobbyMemberSteamIds()
		{
			if (!IsInLobby)
			{
				return Array.Empty<ulong>();
			}

			RefreshLobbyState();
			// 현재 단계에서는 디버그/확장용 조회. 최종 플레이어 spawn 권한은 NGO 연결 이벤트 기준으로 옮겨갈 예정.
			var memberIds = new ulong[Mathf.Max(0, currentLobby.MemberCount)];
			var index = 0;

			foreach (var member in currentLobby.Members)
			{
				if (index >= memberIds.Length)
				{
					break;
				}

				memberIds[index] = member.Id.Value;
				index++;
			}

			return memberIds;
		}

		public void RefreshLobbyState()
		{
			// Steamworks callback을 놓쳐도 UI와 Host 시작 조건이 영원히 1/8에 머물지 않게 하는 보정 경로.
			// 테스트 중 Host만 1/8, Client는 2/8로 보이던 문제를 이 polling 갱신으로 완화.
			// Host에서 멤버 참가 콜백이 늦거나 누락되어도 대기 루프가 최신 Steam Lobby 상태를 다시 읽도록 하는 방어 갱신.
			if (!IsInLobby)
			{
				return;
			}

			ApplyLobbyState(currentLobby, false);
		}

		public void LeaveLobby()
		{
			if (IsInLobby)
			{
				currentLobby.Leave();
			}

			ClearLobbyState();
		}

		public void OpenInviteDialog()
		{
			if (IsInLobby)
			{
				SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
			}
		}

		public void SetMatchState(string matchState)
		{
			// Waiting/Countdown/Starting은 Steam Lobby metadata로 공유.
			// 실시간 게임 상태가 아니므로 패킷 순서 보장보다 "대기 화면이 같은 흐름을 보는 것"을 우선.
			MatchState = string.IsNullOrWhiteSpace(matchState) ? SteamLobbyMatchState.Waiting : matchState;

			if (IsInLobby && IsLobbyOwner)
			{
				currentLobby.SetData(MatchStateKey, MatchState);
			}

			Changed?.Invoke();
		}

		public void Dispose()
		{
			SteamMatchmaking.OnLobbyEntered -= OnLobbyEntered;
			SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberChanged;
			SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberChanged;
			SteamMatchmaking.OnLobbyDataChanged -= OnLobbyDataChanged;
			SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
			LeaveLobby();
			Changed = null;
		}

		private async UniTaskVoid TryJoinCommandLineLobbyAsync()
		{
			// Steam 초대 실행 인자 +connect_lobby 처리.
			var elapsedTime = 0f;

			while (!IsAvailable && elapsedTime < 3f)
			{
				await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
				elapsedTime += 0.1f;
			}

			if (!IsAvailable)
			{
				return;
			}

			var args = Environment.GetCommandLineArgs();

			for (var i = 0; i < args.Length - 1; i++)
			{
				if (!string.Equals(args[i], "+connect_lobby", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (ulong.TryParse(args[i + 1], out var lobbyId))
				{
					JoinLobbyAsync(lobbyId, CancellationToken.None).Forget();
				}

				return;
			}
		}

		private void OnLobbyEntered(Lobby lobby)
		{
			ApplyLobbyState(lobby, true);
		}

		private void OnLobbyMemberChanged(Lobby lobby, Friend friend)
		{
			if (IsSameLobby(lobby))
			{
				ApplyLobbyState(lobby, true);
			}
		}

		private void OnLobbyDataChanged(Lobby lobby)
		{
			if (IsSameLobby(lobby))
			{
				ApplyLobbyState(lobby, true);
			}
		}

		private void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
		{
			// Steam overlay 초대 수락 callback. 이미 게임이 켜져 있으면 이 경로로 바로 Lobby 참가 시도.
			Debug.Log($"Facepunch Steam lobby join requested: lobby={lobby.Id.Value}, friend={steamId.Value}");
			JoinLobbyAsync(lobby.Id.Value, CancellationToken.None).Forget();
		}

		private bool IsSameLobby(Lobby lobby)
		{
			return IsInLobby && lobby.Id.Value == CurrentLobbyId;
		}

		private void ApplyLobbyState(Lobby lobby, bool forceNotify)
		{
			// Facepunch Lobby 객체에서 필요한 값만 서비스 캐시로 복사.
			// View와 StageManager가 Facepunch 타입에 직접 묶이지 않게 하는 작은 어댑터 역할.
			// Facepunch Lobby 상태를 게임 서비스 상태로 복사.
			var previousMemberCount = CurrentMemberCount;
			var previousMatchState = MatchState;
			var wasInLobby = IsInLobby;

			currentLobby = lobby;
			IsInLobby = true;
			CurrentLobbyId = lobby.Id.Value;
			CurrentMemberCount = lobby.MemberCount;
			IsLobbyOwner = lobby.Owner.Id.Value == steamworksClientService.LocalSteamId;

			var matchState = lobby.GetData(MatchStateKey);
			MatchState = string.IsNullOrWhiteSpace(matchState) ? SteamLobbyMatchState.Waiting : matchState;

			if (forceNotify || !wasInLobby || previousMemberCount != CurrentMemberCount || previousMatchState != MatchState)
			{
				Debug.Log($"Facepunch Steam lobby state updated: lobby={CurrentLobbyId}, members={CurrentMemberCount}, owner={IsLobbyOwner}, matchState={MatchState}");
				Changed?.Invoke();
			}
		}

		private void ClearLobbyState()
		{
			currentLobby = default;
			IsInLobby = false;
			IsLobbyOwner = false;
			CurrentLobbyId = 0;
			CurrentMemberCount = 0;
			MaxMembers = 0;
			MatchState = SteamLobbyMatchState.Waiting;
			Changed?.Invoke();
		}
	}
}
