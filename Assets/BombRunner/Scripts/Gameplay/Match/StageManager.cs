using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Gameplay.Player;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class StageManager : IStartable
	{
		private readonly PlayerSpawnService playerSpawnService;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly DashCooldownLogView dashCooldownLogView;
		private readonly LocalTargetTossPrototype localTargetTossPrototype;

		public StageManager(
			PlayerSpawnService playerSpawnService,
			LocalPlayerCameraFollow cameraFollow,
			DashCooldownLogView dashCooldownLogView,
			LocalTargetTossPrototype localTargetTossPrototype)
		{
			this.playerSpawnService = playerSpawnService;
			this.cameraFollow = cameraFollow;
			this.dashCooldownLogView = dashCooldownLogView;
			this.localTargetTossPrototype = localTargetTossPrototype;
		}

		// Game Scene 진입 시 로컬 플레이어와 임시 더미를 스폰한다. 더미는 네트워크 더미/실제 플레이어로 교체한다.
		public void Start()
		{
			var player = playerSpawnService.SpawnLocalPlayer();
			var dummy = playerSpawnService.SpawnDummyPlayer();

			if (player == null || dummy == null)
			{
				return;
			}

			// 지금은 로컬 플레이어 기준 카메라와 로그만 연결한다. 이후 CameraService/HUD View로 교체한다.
			cameraFollow.SetTarget(player.transform);

			if (player.TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashCooldownLogView.SetTarget(dashController);
			}

			if (player.TryGetComponent<PlayerStateController>(out var playerState)
				&& dummy.TryGetComponent<PlayerStateController>(out var dummyState))
			{
				localTargetTossPrototype.Initialize(playerState, dummyState);
			}
		}
	}
}
