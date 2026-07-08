using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Gameplay.Player;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class StageManager : IStartable
	{
		private readonly PlayerSpawnService playerSpawnService;
		private readonly BombSpawnService bombSpawnService;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly DashCooldownLogView dashCooldownLogView;
		private readonly BombTargetService bombTargetService;
		private readonly LocalTargetTossPrototype localTargetTossPrototype;

		public StageManager(
			PlayerSpawnService playerSpawnService,
			BombSpawnService bombSpawnService,
			LocalPlayerCameraFollow cameraFollow,
			DashCooldownLogView dashCooldownLogView,
			BombTargetService bombTargetService,
			LocalTargetTossPrototype localTargetTossPrototype)
		{
			this.playerSpawnService = playerSpawnService;
			this.bombSpawnService = bombSpawnService;
			this.cameraFollow = cameraFollow;
			this.dashCooldownLogView = dashCooldownLogView;
			this.bombTargetService = bombTargetService;
			this.localTargetTossPrototype = localTargetTossPrototype;
		}

		// 임시 로컬 검증용 진입점입니다. 실제 멀티플레이가 들어오면 네트워크 스폰과 Host 권한 흐름으로 교체합니다.
		public void Start()
		{
			var player = playerSpawnService.SpawnLocalPlayer();
			var dummy = playerSpawnService.SpawnDummyPlayer();

			if (player == null || dummy == null)
			{
				return;
			}

			// 지금은 로컬 플레이어 기준 카메라와 로그만 연결합니다. 이후 CameraService/HUD View로 교체합니다.
			cameraFollow.SetTarget(player.transform);

			if (player.TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashCooldownLogView.SetTarget(dashController);
			}

			if (player.TryGetComponent<PlayerStateController>(out var playerState)
				&& dummy.TryGetComponent<PlayerStateController>(out var dummyState))
			{
				bombTargetService.Initialize(playerState, dummyState);
				localTargetTossPrototype.Initialize(playerState, dummyState);
				bombSpawnService.SpawnLocalBomb(playerState, dummyState);
			}
		}
	}
}
