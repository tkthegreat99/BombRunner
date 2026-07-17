using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	public sealed class PlayerNetworkIdentity : MonoBehaviour
	{
		[SerializeField] private ulong steamId;

		public ulong SteamId => steamId;

		public void SetSteamId(ulong steamId)
		{
			this.steamId = steamId;
		}
	}
}
