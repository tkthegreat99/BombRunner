using UnityEngine;

namespace BombRunner.Scripts.Multiplayer
{
	// 네트워크 없는 로컬 프로토타입을 Host 권한으로 취급하는 세션 구현.
	public sealed class LocalMatchNetworkSessionService : IMatchNetworkSessionService
	{
		public MatchAuthorityMode AuthorityMode { get; private set; } = MatchAuthorityMode.LocalHost;
		public bool IsHostAuthority => AuthorityMode == MatchAuthorityMode.LocalHost || AuthorityMode == MatchAuthorityMode.Host;
		public ulong LocalClientId { get; private set; }

		public void InitializeLocalSession()
		{
			// Host/Master 확정 대상: 로컬 프로토타입 세션을 Host 권한으로 취급.
			AuthorityMode = MatchAuthorityMode.LocalHost;
			LocalClientId = 0;
			Debug.Log("Match network session: local host authority initialized");
		}
	}
}
