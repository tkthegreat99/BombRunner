using System;
using System.Threading;
using BombRunner.Scripts.App;
using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Gameplay.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class StageManager : IStartable, IDisposable
	{
		private readonly SceneFlowService sceneFlowService;
		private readonly PlayerSpawnService playerSpawnService;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly DashCooldownLogView dashCooldownLogView;
		private readonly BombTargetService bombTargetService;
		private readonly LocalTargetTossPrototype localTargetTossPrototype;
		private readonly LocalQuickMatchWaitingService localQuickMatchWaitingService;
		private readonly LocalMatchFlowService localMatchFlowService;
		private readonly LocalPlayerSeparationService localPlayerSeparationService;
		private readonly LocalDownedObstacleService localDownedObstacleService;
		private readonly LocalTauntPrototype localTauntPrototype;
		private readonly LocalWorldFeedbackView localWorldFeedbackView;
		private readonly CancellationTokenSource cancellationTokenSource = new();

		public StageManager(
			SceneFlowService sceneFlowService,
			PlayerSpawnService playerSpawnService,
			LocalPlayerCameraFollow cameraFollow,
			DashCooldownLogView dashCooldownLogView,
			BombTargetService bombTargetService,
			LocalTargetTossPrototype localTargetTossPrototype,
			LocalQuickMatchWaitingService localQuickMatchWaitingService,
			LocalMatchFlowService localMatchFlowService,
			LocalPlayerSeparationService localPlayerSeparationService,
			LocalDownedObstacleService localDownedObstacleService,
			LocalTauntPrototype localTauntPrototype,
			LocalWorldFeedbackView localWorldFeedbackView)
		{
			this.sceneFlowService = sceneFlowService;
			this.playerSpawnService = playerSpawnService;
			this.cameraFollow = cameraFollow;
			this.dashCooldownLogView = dashCooldownLogView;
			this.bombTargetService = bombTargetService;
			this.localTargetTossPrototype = localTargetTossPrototype;
			this.localQuickMatchWaitingService = localQuickMatchWaitingService;
			this.localMatchFlowService = localMatchFlowService;
			this.localPlayerSeparationService = localPlayerSeparationService;
			this.localDownedObstacleService = localDownedObstacleService;
			this.localTauntPrototype = localTauntPrototype;
			this.localWorldFeedbackView = localWorldFeedbackView;
		}

		// 임시 로컬 검증용 진입점. 실제 멀티플레이가 들어오면 네트워크 스폰과 Host 권한 흐름으로 교체.
		public void Start()
		{
			RunStageAsync(cancellationTokenSource.Token).Forget();
		}

		public void Dispose()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		private async UniTaskVoid RunStageAsync(CancellationToken cancellationToken)
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

			if (sceneFlowService.RequestedMatchMode == MatchMode.LocalQuickMatchWaiting)
			{
				Debug.Log("StageManager: local quick match waiting flow started.");
				await localQuickMatchWaitingService.WaitForMatchStartAsync(players, cancellationToken);
			}

			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			bombTargetService.Initialize(players);
			localTargetTossPrototype.Initialize(players);
			localMatchFlowService.Initialize(players);
			localPlayerSeparationService.Initialize(players);
			localDownedObstacleService.Initialize(players);
			localTauntPrototype.Initialize(players);
			localWorldFeedbackView.Initialize(players);
		}
	}
}
