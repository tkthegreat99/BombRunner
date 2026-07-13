using BombRunner.Scripts.Bomb;
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
		private readonly BombTargetService bombTargetService;
		private readonly LocalTargetTossPrototype localTargetTossPrototype;
		private readonly LocalMatchFlowService localMatchFlowService;
		private readonly LocalPlayerSeparationService localPlayerSeparationService;
		private readonly LocalDownedObstacleService localDownedObstacleService;
		private readonly LocalTauntPrototype localTauntPrototype;

		public StageManager(
			PlayerSpawnService playerSpawnService,
			LocalPlayerCameraFollow cameraFollow,
			DashCooldownLogView dashCooldownLogView,
			BombTargetService bombTargetService,
			LocalTargetTossPrototype localTargetTossPrototype,
			LocalMatchFlowService localMatchFlowService,
			LocalPlayerSeparationService localPlayerSeparationService,
			LocalDownedObstacleService localDownedObstacleService,
			LocalTauntPrototype localTauntPrototype)
		{
			this.playerSpawnService = playerSpawnService;
			this.cameraFollow = cameraFollow;
			this.dashCooldownLogView = dashCooldownLogView;
			this.bombTargetService = bombTargetService;
			this.localTargetTossPrototype = localTargetTossPrototype;
			this.localMatchFlowService = localMatchFlowService;
			this.localPlayerSeparationService = localPlayerSeparationService;
			this.localDownedObstacleService = localDownedObstacleService;
			this.localTauntPrototype = localTauntPrototype;
		}

		// 임시 로컬 검증용 진입점. 실제 멀티플레이가 들어오면 네트워크 스폰과 Host 권한 흐름으로 교체.
		public void Start()
		{
			var player = playerSpawnService.SpawnLocalPlayer();
			var dummyPlayers = playerSpawnService.SpawnDummyPlayers();

			if (player == null || dummyPlayers == null || dummyPlayers.Length < 2)
			{
				return;
			}

			// 지금은 로컬 플레이어 기준 카메라와 로그만 연결. 이후 CameraService/HUD View로 교체.
			cameraFollow.SetTarget(player.transform);

			if (player.TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashCooldownLogView.SetTarget(dashController);
			}

			if (!player.TryGetComponent<PlayerStateController>(out var playerState)
				|| !dummyPlayers[0].TryGetComponent<PlayerStateController>(out var dummyStateA)
				|| !dummyPlayers[1].TryGetComponent<PlayerStateController>(out var dummyStateB))
			{
				return;
			}

			var players = new[]
			{
				playerState,
				dummyStateA,
				dummyStateB
			};

			bombTargetService.Initialize(players);
			localTargetTossPrototype.Initialize(players);
			localMatchFlowService.Initialize(players);
			localPlayerSeparationService.Initialize(players);
			localDownedObstacleService.Initialize(players);
			localTauntPrototype.Initialize(players);
		}
	}
}
