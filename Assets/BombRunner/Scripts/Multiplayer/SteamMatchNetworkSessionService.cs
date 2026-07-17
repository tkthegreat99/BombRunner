using UnityEngine;

namespace BombRunner.Scripts.Multiplayer
{
	public sealed class SteamMatchNetworkSessionService : IMatchNetworkSessionService
	{
		private readonly ISteamworksClientService steamworksClientService;
		private readonly ISteamLobbyService steamLobbyService;

		public MatchAuthorityMode AuthorityMode { get; private set; } = MatchAuthorityMode.LocalHost;
		public bool IsHostAuthority => AuthorityMode == MatchAuthorityMode.LocalHost || AuthorityMode == MatchAuthorityMode.Host;
		public ulong LocalClientId => steamworksClientService != null ? steamworksClientService.LocalSteamId : 0;

		public SteamMatchNetworkSessionService(
			ISteamworksClientService steamworksClientService,
			ISteamLobbyService steamLobbyService)
		{
			this.steamworksClientService = steamworksClientService;
			this.steamLobbyService = steamLobbyService;
		}

		public void InitializeLocalSession()
		{
			// Host/Master 확정 대상: Steam Lobby 소유자 권한을 Host로 매핑.
			if (steamLobbyService != null && steamLobbyService.IsInLobby)
			{
				AuthorityMode = steamLobbyService.IsLobbyOwner ? MatchAuthorityMode.Host : MatchAuthorityMode.Client;
				Debug.Log($"Match network session: Steam lobby authority initialized as {AuthorityMode}");
				return;
			}

			AuthorityMode = MatchAuthorityMode.LocalHost;
			Debug.Log("Match network session: local host authority initialized");
		}
	}
}
