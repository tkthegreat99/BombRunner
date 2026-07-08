using BombRunner.Scripts.Gameplay.Player;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class StageManager : IStartable
	{
		private readonly PlayerSpawnService playerSpawnService;

		public StageManager(PlayerSpawnService playerSpawnService)
		{
			this.playerSpawnService = playerSpawnService;
		}

		// Game Scene 진입 시 로컬 플레이어를 1회 스폰한다.
		public void Start()
		{
			playerSpawnService.SpawnLocalPlayer();
		}
	}
}
