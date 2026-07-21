using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.Multiplayer
{
	// Steam Lobby 생성, 참가, 멤버 상태, 매치 메타데이터를 게임 흐름에 노출하는 경계.
	// 현재 Steam Lobby는 실제 실시간 패킷 동기화가 아니라 친구 초대, Host 판정, 대기/카운트다운 상태 공유까지만 담당.
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

		ulong GetLobbyOwnerSteamId();
		ulong[] GetLobbyMemberSteamIds();
		UniTask<bool> CreateQuickMatchLobbyAsync(int maxMembers, CancellationToken cancellationToken);
		UniTask<bool> JoinLobbyAsync(ulong lobbyId, CancellationToken cancellationToken);
		void RefreshLobbyState();
		void LeaveLobby();
		void OpenInviteDialog();
		void SetMatchState(string matchState);
	}
}
