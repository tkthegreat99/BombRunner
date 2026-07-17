using System;
using System.Threading;
using BombRunner.Scripts.App;
using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Gameplay.Items;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Multiplayer;
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
		private readonly IMatchNetworkSessionService networkSessionService;
		private readonly ISteamworksClientService steamworksClientService;
		private readonly ISteamLobbyService steamLobbyService;
		private readonly LocalTargetTossPrototype localTargetTossPrototype;
		private readonly LocalQuickMatchWaitingService localQuickMatchWaitingService;
		private readonly LocalMatchFlowService localMatchFlowService;
		private readonly LocalPlayerSeparationService localPlayerSeparationService;
		private readonly LocalDownedObstacleService localDownedObstacleService;
		private readonly LocalItemService localItemService;
		private readonly LocalTauntPrototype localTauntPrototype;
		private readonly LocalWorldFeedbackView localWorldFeedbackView;
		private readonly CancellationTokenSource cancellationTokenSource = new();

		public StageManager(
			SceneFlowService sceneFlowService,
			PlayerSpawnService playerSpawnService,
			LocalPlayerCameraFollow cameraFollow,
			DashCooldownLogView dashCooldownLogView,
			BombTargetService bombTargetService,
			IMatchNetworkSessionService networkSessionService,
			ISteamworksClientService steamworksClientService,
			ISteamLobbyService steamLobbyService,
			LocalTargetTossPrototype localTargetTossPrototype,
			LocalQuickMatchWaitingService localQuickMatchWaitingService,
			LocalMatchFlowService localMatchFlowService,
			LocalPlayerSeparationService localPlayerSeparationService,
			LocalDownedObstacleService localDownedObstacleService,
			LocalItemService localItemService,
			LocalTauntPrototype localTauntPrototype,
			LocalWorldFeedbackView localWorldFeedbackView)
		{
			this.sceneFlowService = sceneFlowService;
			this.playerSpawnService = playerSpawnService;
			this.cameraFollow = cameraFollow;
			this.dashCooldownLogView = dashCooldownLogView;
			this.bombTargetService = bombTargetService;
			this.networkSessionService = networkSessionService;
			this.steamworksClientService = steamworksClientService;
			this.steamLobbyService = steamLobbyService;
			this.localTargetTossPrototype = localTargetTossPrototype;
			this.localQuickMatchWaitingService = localQuickMatchWaitingService;
			this.localMatchFlowService = localMatchFlowService;
			this.localPlayerSeparationService = localPlayerSeparationService;
			this.localDownedObstacleService = localDownedObstacleService;
			this.localItemService = localItemService;
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
			networkSessionService.InitializeLocalSession();

			var isSteamLobbyMatch = steamLobbyService != null
				&& steamLobbyService.IsInLobby
				&& steamworksClientService != null
				&& steamworksClientService.IsInitialized;
			GameObject player;
			PlayerStateController[] players;

			if (isSteamLobbyMatch)
			{
				var playerObjects = playerSpawnService.SpawnSteamLobbyPlayers(
					steamLobbyService.GetLobbyMemberSteamIds(),
					steamworksClientService.LocalSteamId);
				player = FindLocalPlayerObject(playerObjects, steamworksClientService.LocalSteamId);
				players = GetPlayerStates(playerObjects);
			}
			else
			{
				player = playerSpawnService.SpawnLocalPlayer();
				var dummyPlayers = playerSpawnService.SpawnDummyPlayers();
				players = GetPlayerStates(new[]
				{
					player,
					dummyPlayers[0],
					dummyPlayers[1]
				});
			}

			if (player == null || players == null || players.Length == 0)
			{
				return;
			}

			// 지금은 로컬 플레이어 기준 카메라와 로그만 연결. 이후 CameraService/HUD View로 교체.
			cameraFollow.SetTarget(player.transform);

			if (player.TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashCooldownLogView.SetTarget(dashController);
			}

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
			localWorldFeedbackView.Initialize(players);

			if (isSteamLobbyMatch && !networkSessionService.IsHostAuthority)
			{
				Debug.Log("StageManager: Steam client follows Host/Master snapshots.");
				return;
			}

			localTargetTossPrototype.Initialize(players);
			localMatchFlowService.Initialize(players);
			localPlayerSeparationService.Initialize(players);
			localDownedObstacleService.Initialize(players);
			localItemService.Initialize(players);
			localTauntPrototype.Initialize(players);
		}

		private GameObject FindLocalPlayerObject(GameObject[] playerObjects, ulong localSteamId)
		{
			if (playerObjects == null)
			{
				return null;
			}

			for (var i = 0; i < playerObjects.Length; i++)
			{
				var playerObject = playerObjects[i];

				if (playerObject != null
					&& playerObject.TryGetComponent<PlayerNetworkIdentity>(out var networkIdentity)
					&& networkIdentity.SteamId == localSteamId)
				{
					return playerObject;
				}
			}

			return null;
		}

		private PlayerStateController[] GetPlayerStates(GameObject[] playerObjects)
		{
			if (playerObjects == null || playerObjects.Length == 0)
			{
				return Array.Empty<PlayerStateController>();
			}

			var states = new PlayerStateController[playerObjects.Length];

			for (var i = 0; i < playerObjects.Length; i++)
			{
				var playerObject = playerObjects[i];

				if (playerObject == null
					|| !playerObject.TryGetComponent<PlayerStateController>(out var playerState))
				{
					return Array.Empty<PlayerStateController>();
				}

				states[i] = playerState;
			}

			return states;
		}
	}
}
