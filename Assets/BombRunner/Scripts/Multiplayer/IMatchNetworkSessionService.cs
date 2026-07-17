namespace BombRunner.Scripts.Multiplayer
{
	public interface IMatchNetworkSessionService
	{
		MatchAuthorityMode AuthorityMode { get; }
		bool IsHostAuthority { get; }
		ulong LocalClientId { get; }
		void InitializeLocalSession();
	}
}
