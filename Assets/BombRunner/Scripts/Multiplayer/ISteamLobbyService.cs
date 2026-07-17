using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.Multiplayer
{
	public interface ISteamLobbyService
	{
		event Action Changed;

		bool IsAvailable { get; }
		bool IsInLobby { get; }
		bool IsLobbyOwner { get; }
		ulong CurrentLobbyId { get; }
		int CurrentMemberCount { get; }
		int MaxMembers { get; }
		string MatchState { get; }

		UniTask<bool> CreateQuickMatchLobbyAsync(int maxMembers, CancellationToken cancellationToken);
		UniTask<bool> JoinLobbyAsync(ulong lobbyId, CancellationToken cancellationToken);
		void LeaveLobby();
		void OpenInviteDialog();
		void SetMatchState(string matchState);
	}
}
