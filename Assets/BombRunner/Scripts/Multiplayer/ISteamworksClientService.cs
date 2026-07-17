namespace BombRunner.Scripts.Multiplayer
{
	public interface ISteamworksClientService
	{
		bool IsInitialized { get; }
		ulong LocalSteamId { get; }
		string PersonaName { get; }
	}
}
