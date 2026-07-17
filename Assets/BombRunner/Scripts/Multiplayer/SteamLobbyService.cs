using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Multiplayer
{
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
			SteamMatchmaking.OnLobbyEntered += OnLobbyEntered;
			SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberChanged;
			SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberChanged;
			SteamMatchmaking.OnLobbyDataChanged += OnLobbyDataChanged;
			SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
			TryJoinCommandLineLobbyAsync().Forget();
		}

		public async UniTask<bool> CreateQuickMatchLobbyAsync(int maxMembers, CancellationToken cancellationToken)
		{
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
			currentLobby.SetData(LobbyNameKey, $"{steamworksClientService.PersonaName}'s Boom Runner Lobby");
			currentLobby.SetData(MatchStateKey, SteamLobbyMatchState.Waiting);
			ApplyLobbyState(currentLobby);
			Debug.Log($"Facepunch Steam lobby created: {CurrentLobbyId}");
			return true;
		}

		public async UniTask<bool> JoinLobbyAsync(ulong lobbyId, CancellationToken cancellationToken)
		{
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

			ApplyLobbyState(lobbyResult.Value);
			Debug.Log($"Facepunch Steam lobby joined: {CurrentLobbyId}, members={CurrentMemberCount}");
			return true;
		}

		public ulong GetLobbyOwnerSteamId()
		{
			return IsInLobby ? currentLobby.Owner.Id.Value : 0;
		}

		public ulong[] GetLobbyMemberSteamIds()
		{
			if (!IsInLobby)
			{
				return Array.Empty<ulong>();
			}

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
			ApplyLobbyState(lobby);
		}

		private void OnLobbyMemberChanged(Lobby lobby, Friend friend)
		{
			if (IsSameLobby(lobby))
			{
				ApplyLobbyState(lobby);
			}
		}

		private void OnLobbyDataChanged(Lobby lobby)
		{
			if (IsSameLobby(lobby))
			{
				ApplyLobbyState(lobby);
			}
		}

		private void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
		{
			Debug.Log($"Facepunch Steam lobby join requested: lobby={lobby.Id.Value}, friend={steamId.Value}");
			JoinLobbyAsync(lobby.Id.Value, CancellationToken.None).Forget();
		}

		private bool IsSameLobby(Lobby lobby)
		{
			return IsInLobby && lobby.Id.Value == CurrentLobbyId;
		}

		private void ApplyLobbyState(Lobby lobby)
		{
			currentLobby = lobby;
			IsInLobby = true;
			CurrentLobbyId = lobby.Id.Value;
			CurrentMemberCount = lobby.MemberCount;
			IsLobbyOwner = lobby.Owner.Id.Value == steamworksClientService.LocalSteamId;

			var matchState = lobby.GetData(MatchStateKey);
			MatchState = string.IsNullOrWhiteSpace(matchState) ? SteamLobbyMatchState.Waiting : matchState;
			Changed?.Invoke();
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
