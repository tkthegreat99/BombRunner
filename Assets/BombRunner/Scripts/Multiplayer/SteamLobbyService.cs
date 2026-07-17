using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
using Steamworks;
#endif

namespace BombRunner.Scripts.Multiplayer
{
	public sealed class SteamLobbyService : ISteamLobbyService, IStartable, IDisposable
	{
		private const string LobbyNameKey = "name";
		private const string MatchStateKey = "match_state";

		private readonly ISteamworksClientService steamworksClientService;
		private UniTaskCompletionSource<bool> pendingCreateLobby;
		private UniTaskCompletionSource<bool> pendingJoinLobby;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
		private readonly CallResult<LobbyCreated_t> lobbyCreatedResult;
		private readonly CallResult<LobbyEnter_t> lobbyEnterResult;
		private readonly Callback<LobbyEnter_t> lobbyEnterCallback;
		private readonly Callback<LobbyChatUpdate_t> lobbyChatUpdateCallback;
		private readonly Callback<LobbyDataUpdate_t> lobbyDataUpdateCallback;
		private readonly Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequestedCallback;
		private CSteamID currentLobbyId = CSteamID.Nil;
#endif

		public event Action Changed;

		public bool IsAvailable => steamworksClientService.IsInitialized;
		public bool IsInLobby { get; private set; }
		public bool IsLobbyOwner { get; private set; }
		public ulong CurrentLobbyId { get; private set; }
		public int CurrentMemberCount { get; private set; }
		public int MaxMembers { get; private set; }
		public string MatchState { get; private set; } = SteamLobbyMatchState.Waiting;

		public SteamLobbyService(ISteamworksClientService steamworksClientService)
		{
			this.steamworksClientService = steamworksClientService;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			lobbyCreatedResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
			lobbyEnterResult = CallResult<LobbyEnter_t>.Create(OnLobbyEnterResult);
			lobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
			lobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdated);
			lobbyDataUpdateCallback = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
			gameLobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
#endif
		}

		public void Start()
		{
			TryJoinCommandLineLobbyAsync().Forget();
		}

		public async UniTask<bool> CreateQuickMatchLobbyAsync(int maxMembers, CancellationToken cancellationToken)
		{
			if (!IsAvailable)
			{
				return false;
			}

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			if (IsInLobby)
			{
				return true;
			}

			MaxMembers = Mathf.Max(1, maxMembers);
			MatchState = SteamLobbyMatchState.Waiting;
			pendingCreateLobby = new UniTaskCompletionSource<bool>();
			var apiCall = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxMembers);
			lobbyCreatedResult.Set(apiCall);

			using (cancellationToken.Register(() => pendingCreateLobby.TrySetCanceled(cancellationToken)))
			{
				return await pendingCreateLobby.Task;
			}
#else
			await UniTask.Yield(cancellationToken);
			return false;
#endif
		}

		public async UniTask<bool> JoinLobbyAsync(ulong lobbyId, CancellationToken cancellationToken)
		{
			if (!IsAvailable || lobbyId == 0)
			{
				return false;
			}

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			pendingJoinLobby = new UniTaskCompletionSource<bool>();
			var apiCall = SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
			lobbyEnterResult.Set(apiCall);

			using (cancellationToken.Register(() => pendingJoinLobby.TrySetCanceled(cancellationToken)))
			{
				return await pendingJoinLobby.Task;
			}
#else
			await UniTask.Yield(cancellationToken);
			return false;
#endif
		}

		public void LeaveLobby()
		{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			if (IsAvailable && IsInLobby)
			{
				SteamMatchmaking.LeaveLobby(currentLobbyId);
			}
#endif

			ClearLobbyState();
		}

		public void OpenInviteDialog()
		{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			if (IsAvailable && IsInLobby)
			{
				SteamFriends.ActivateGameOverlayInviteDialog(currentLobbyId);
			}
#endif
		}

		public void SetMatchState(string matchState)
		{
			MatchState = string.IsNullOrWhiteSpace(matchState) ? SteamLobbyMatchState.Waiting : matchState;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			if (IsAvailable && IsInLobby && IsLobbyOwner)
			{
				SteamMatchmaking.SetLobbyData(currentLobbyId, MatchStateKey, MatchState);
			}
#endif

			Changed?.Invoke();
		}

		public void Dispose()
		{
			LeaveLobby();

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			lobbyCreatedResult.Dispose();
			lobbyEnterResult.Dispose();
			lobbyEnterCallback.Dispose();
			lobbyChatUpdateCallback.Dispose();
			lobbyDataUpdateCallback.Dispose();
			gameLobbyJoinRequestedCallback.Dispose();
#endif
		}

		private async UniTaskVoid TryJoinCommandLineLobbyAsync()
		{
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

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
		private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
		{
			if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
			{
				Debug.LogWarning($"Steam lobby create failed: ioFailure={ioFailure}, result={callback.m_eResult}");
				pendingCreateLobby?.TrySetResult(false);
				return;
			}

			currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
			CurrentLobbyId = callback.m_ulSteamIDLobby;
			IsInLobby = true;
			IsLobbyOwner = true;
			CurrentMemberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);
			SteamMatchmaking.SetLobbyData(currentLobbyId, LobbyNameKey, $"{steamworksClientService.PersonaName}'s Boom Runner Lobby");
			SteamMatchmaking.SetLobbyData(currentLobbyId, MatchStateKey, SteamLobbyMatchState.Waiting);
			Debug.Log($"Steam lobby created: {CurrentLobbyId}");
			pendingCreateLobby?.TrySetResult(true);
			Changed?.Invoke();
		}

		private void OnLobbyEnterResult(LobbyEnter_t callback, bool ioFailure)
		{
			if (ioFailure || callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
			{
				Debug.LogWarning($"Steam lobby join failed: ioFailure={ioFailure}, response={callback.m_EChatRoomEnterResponse}");
				pendingJoinLobby?.TrySetResult(false);
				return;
			}

			ApplyEnteredLobby(new CSteamID(callback.m_ulSteamIDLobby));
			pendingJoinLobby?.TrySetResult(true);
		}

		private void OnLobbyEntered(LobbyEnter_t callback)
		{
			if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
			{
				return;
			}

			ApplyEnteredLobby(new CSteamID(callback.m_ulSteamIDLobby));
		}

		private void OnLobbyChatUpdated(LobbyChatUpdate_t callback)
		{
			if (!IsInLobby || callback.m_ulSteamIDLobby != CurrentLobbyId)
			{
				return;
			}

			RefreshLobbyState();
		}

		private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
		{
			if (!IsInLobby || callback.m_ulSteamIDLobby != CurrentLobbyId)
			{
				return;
			}

			RefreshLobbyState();
		}

		private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
		{
			Debug.Log($"Steam lobby join requested: lobby={callback.m_steamIDLobby.m_SteamID}");
			JoinLobbyAsync(callback.m_steamIDLobby.m_SteamID, CancellationToken.None).Forget();
		}

		private void ApplyEnteredLobby(CSteamID lobbyId)
		{
			currentLobbyId = lobbyId;
			CurrentLobbyId = lobbyId.m_SteamID;
			IsInLobby = true;
			RefreshLobbyState();
			Debug.Log($"Steam lobby entered: {CurrentLobbyId}, members={CurrentMemberCount}");
		}

		private void RefreshLobbyState()
		{
			if (!IsAvailable || !IsInLobby)
			{
				return;
			}

			CurrentMemberCount = SteamMatchmaking.GetNumLobbyMembers(currentLobbyId);
			var owner = SteamMatchmaking.GetLobbyOwner(currentLobbyId);
			IsLobbyOwner = owner.m_SteamID == steamworksClientService.LocalSteamId;
			var matchState = SteamMatchmaking.GetLobbyData(currentLobbyId, MatchStateKey);
			MatchState = string.IsNullOrWhiteSpace(matchState) ? SteamLobbyMatchState.Waiting : matchState;
			Changed?.Invoke();
		}
#endif

		private void ClearLobbyState()
		{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			currentLobbyId = CSteamID.Nil;
#endif
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
