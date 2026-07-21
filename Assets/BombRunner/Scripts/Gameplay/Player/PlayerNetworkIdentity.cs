using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	// Steam 로비 수동 스폰 프로토타입에서 플레이어와 Steam ID를 연결하는 식별자.
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
