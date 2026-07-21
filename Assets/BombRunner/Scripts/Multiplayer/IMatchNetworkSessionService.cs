namespace BombRunner.Scripts.Multiplayer
{
	// 매치 코드가 현재 권한 모드를 확인하는 네트워크 세션 경계.
	public interface IMatchNetworkSessionService
	{
		MatchAuthorityMode AuthorityMode { get; }
		bool IsHostAuthority { get; }
		ulong LocalClientId { get; }
		void InitializeLocalSession();
	}
}
