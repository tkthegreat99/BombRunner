namespace BombRunner.Scripts.Multiplayer
{
	// Facepunch Steam 클라이언트 초기화 상태와 로컬 Steam 정보를 제공하는 경계.
	public interface ISteamworksClientService
	{
		bool IsInitialized { get; }
		ulong LocalSteamId { get; }
		string PersonaName { get; }
	}
}
