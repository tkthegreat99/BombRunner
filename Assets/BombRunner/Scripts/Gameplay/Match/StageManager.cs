using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Camera;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class StageManager : IStartable
	{
		private readonly PlayerSpawnService playerSpawnService;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly DashCooldownLogView dashCooldownLogView;

		public StageManager(
			PlayerSpawnService playerSpawnService,
			LocalPlayerCameraFollow cameraFollow,
			DashCooldownLogView dashCooldownLogView)
		{
			this.playerSpawnService = playerSpawnService;
			this.cameraFollow = cameraFollow;
			this.dashCooldownLogView = dashCooldownLogView;
		}

		// Game Scene 진입 시 로컬 플레이어를 1회 스폰한다.
		public void Start()
		{
			var player = playerSpawnService.SpawnLocalPlayer();

			if (player == null)
			{
				return;
			}

			// 지금은 로컬 플레이어 기준 카메라와 대시 로그만 연결한다. 이후 CameraService/HUD View로 옮긴다.
			cameraFollow.SetTarget(player.transform);

			if (player.TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashCooldownLogView.SetTarget(dashController);
			}
		}
	}
}
