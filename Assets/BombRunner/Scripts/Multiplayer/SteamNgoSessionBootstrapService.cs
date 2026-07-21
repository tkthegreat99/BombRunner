using System;
using System.Threading;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Input;
using Cysharp.Threading.Tasks;
using Netcode.Transports.Facepunch;
using Unity.Netcode;
using UnityEngine;

namespace BombRunner.Scripts.Multiplayer
{
	// Steam Lobby 발견 흐름을 NGO Host/Client 세션으로 넘기는 부트스트랩 서비스.
	public sealed class SteamNgoSessionBootstrapService : IDisposable
	{
		private readonly PlayerInputReader inputReader;
		private readonly PlayerSpawnSettings playerSpawnSettings;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly ISteamworksClientService steamworksClientService;
		private readonly ISteamLobbyService steamLobbyService;
		private readonly float downedMoveSpeedMultiplier;
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private NetworkManager subscribedNetworkManager;

		public SteamNgoSessionBootstrapService(
			PlayerInputReader inputReader,
			PlayerSpawnSettings playerSpawnSettings,
			LocalPlayerCameraFollow cameraFollow,
			BombRunner.Scripts.Data.GameBalanceSettings balanceSettings,
			ISteamworksClientService steamworksClientService,
			ISteamLobbyService steamLobbyService)
		{
			this.inputReader = inputReader;
			this.playerSpawnSettings = playerSpawnSettings;
			this.cameraFollow = cameraFollow;
			this.steamworksClientService = steamworksClientService;
			this.steamLobbyService = steamLobbyService;
			downedMoveSpeedMultiplier = balanceSettings != null ? balanceSettings.DownedMoveSpeedMultiplier : 0.35f;
		}

		public async UniTask<GameObject> StartSteamLobbySessionAsync(CancellationToken cancellationToken)
		{
			// Steam Lobby waiting/countdown이 끝난 직후 호출되는 NGO 진입점.
			// 이 단계의 목표는 폭탄/아이템/도발까지 모두 옮기는 것이 아니라, 두 Steam 유저가 같은 맵에서 서로의 이동을 보는 것.
			// Steam 로비 매치가 아닐 때는 네트워크 세션 시작 생략.
			if (!CanStartSteamLobbySession())
			{
				return null;
			}

			var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
				cancellationTokenSource.Token,
				cancellationToken);

			try
			{
				// NGO PlayerPrefab이 생성된 뒤 읽을 런타임 이동 설정 준비.
				// NGO가 prefab을 직접 생성하면 VContainer 주입을 받지 못하므로 필요한 로컬 의존성을 static runtime 설정으로 넘김.
				// 장기적으로는 scene-placed NetworkManager/NetworkPrefab registration과 별도 factory 경계로 대체 예정.
				NetworkPlayerRuntimeSettings.Configure(
					inputReader,
					playerSpawnSettings,
					downedMoveSpeedMultiplier,
					cameraFollow != null ? cameraFollow.transform : null,
					steamworksClientService.LocalSteamId,
					steamworksClientService.PersonaName);

				var networkManager = GetOrCreateNetworkManager();

				// Transport와 PlayerPrefab 설정 완료 전 네트워크 시작 방지.
				if (!ConfigureNetworkManager(networkManager))
				{
					return null;
				}

				// 이미 시작된 세션 재시작 방지.
				if (!networkManager.IsListening)
				{
					StartNetworkRole(networkManager);
				}

				return await WaitForLocalPlayerAsync(networkManager, linkedTokenSource.Token);
			}
			finally
			{
				linkedTokenSource.Dispose();
			}
		}

		public void Dispose()
		{
			UnsubscribeNetworkCallbacks();
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		private bool CanStartSteamLobbySession()
		{
			// 여기서 실패하면 local match loop로 조용히 돌아가는 대신 명확한 로그를 남김.
			// 친구 테스트에서는 Inspector 누락이나 Steam 미초기화가 가장 흔한 원인이므로 각 조건을 분리해서 검사.
			// Steam Lobby가 세션 발견과 Host 판정의 현재 소스.
			if (steamLobbyService == null || !steamLobbyService.IsInLobby)
			{
				Debug.LogWarning("NGO Steam bootstrap skipped: Steam lobby is not active.");
				return false;
			}

			if (steamworksClientService == null || !steamworksClientService.IsInitialized)
			{
				Debug.LogWarning("NGO Steam bootstrap skipped: Steam client is not initialized.");
				return false;
			}

			if (playerSpawnSettings == null || playerSpawnSettings.PlayerPrefab == null)
			{
				Debug.LogError("NGO Steam bootstrap failed: PlayerSpawnSettings PlayerPrefab is missing.");
				return false;
			}

			if (!playerSpawnSettings.PlayerPrefab.TryGetComponent<NetworkObject>(out var networkObject))
			{
				Debug.LogError("NGO Steam bootstrap failed: player prefab needs a NetworkObject component.");
				return false;
			}

			if (networkObject.PrefabIdHash == 0)
			{
				// Unity prefab reimport 전에는 NGO prefab hash가 비어 있을 수 있는 상태.
				Debug.LogWarning("NGO Steam bootstrap: player prefab NetworkObject hash is 0. Reimport PrototypePlayer prefab if clients cannot spawn it.");
			}

			return true;
		}

		private NetworkManager GetOrCreateNetworkManager()
		{
			// 현재 Game scene에는 NetworkManager가 고정 배치되어 있지 않으므로 테스트 빌드에서 런타임 생성.
			// 최종 구조에서는 scene/prefab에 명시 배치하고 NetworkConfig를 Inspector에서 관리하는 방향이 더 안전.
			// 씬 배치 NetworkManager가 없을 때 사용하는 런타임 임시 구성.
			if (NetworkManager.Singleton != null)
			{
				return NetworkManager.Singleton;
			}

			var networkManagerObject = new GameObject("NetworkManager");
			var networkManager = networkManagerObject.AddComponent<NetworkManager>();
			networkManagerObject.AddComponent<FacepunchTransport>();
			UnityEngine.Object.DontDestroyOnLoad(networkManagerObject);
			Debug.Log("NGO Steam bootstrap: runtime NetworkManager created.");
			return networkManager;
		}

		private bool ConfigureNetworkManager(NetworkManager networkManager)
		{
			// 런타임 생성 NetworkManager는 NetworkConfig가 null일 수 있으므로 시작 직전 방어적으로 구성.
			// FacepunchTransport는 Steam Relay 연결만 맡고, SteamClient lifetime은 SteamworksClientService가 소유.
			// 현재 단계는 Steam Relay용 Facepunch Transport만 사용.
			if (networkManager == null)
			{
				Debug.LogError("NGO Steam bootstrap failed: NetworkManager is missing.");
				return false;
			}

			var facepunchTransport = networkManager.GetComponent<FacepunchTransport>();

			if (facepunchTransport == null)
			{
				facepunchTransport = networkManager.gameObject.AddComponent<FacepunchTransport>();
			}

			if (networkManager.NetworkConfig == null)
			{
				// 런타임으로 생성한 NetworkManager는 Inspector 직렬화 설정이 없으므로 NGO 시작 전 NetworkConfig를 직접 준비.
				networkManager.NetworkConfig = new NetworkConfig();
			}

			networkManager.NetworkConfig.PlayerPrefab = playerSpawnSettings.PlayerPrefab;
			networkManager.NetworkConfig.NetworkTransport = facepunchTransport;
			// 같은 Game 씬에서 출발하는 친구 테스트용 최소 설정.
			networkManager.NetworkConfig.EnableSceneManagement = false;
			networkManager.NetworkConfig.ForceSamePrefabs = false;
			networkManager.NetworkConfig.TickRate = 30;
			SubscribeNetworkCallbacks(networkManager);
			Debug.Log($"NGO Steam bootstrap configured: prefab={playerSpawnSettings.PlayerPrefab.name}, personaName={steamworksClientService.PersonaName}, localSteamId={steamworksClientService.LocalSteamId}");

			return true;
		}

		private void SubscribeNetworkCallbacks(NetworkManager networkManager)
		{
			// Game 씬에 머무는 동안 Steam Lobby 연결 상태를 콘솔에서 추적하기 위한 NGO 콜백 등록.
			if (subscribedNetworkManager != null && subscribedNetworkManager != networkManager)
			{
				UnsubscribeNetworkCallbacks();
			}

			networkManager.OnClientConnectedCallback -= OnClientConnected;
			networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
			networkManager.OnClientConnectedCallback += OnClientConnected;
			networkManager.OnClientDisconnectCallback += OnClientDisconnected;
			subscribedNetworkManager = networkManager;
		}

		private void UnsubscribeNetworkCallbacks()
		{
			// VContainer 수명 종료 또는 씬 전환 시 같은 로그 콜백이 누적 등록되지 않도록 정리.
			if (subscribedNetworkManager == null)
			{
				return;
			}

			subscribedNetworkManager.OnClientConnectedCallback -= OnClientConnected;
			subscribedNetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
			subscribedNetworkManager = null;
		}

		private void OnClientConnected(ulong clientId)
		{
			// 친구 테스트 중 Host/Client 접속 성공 시점을 바로 확인하기 위한 연결 로그.
			Debug.Log($"NGO Steam bootstrap: client connected, clientId={clientId}");
		}

		private void OnClientDisconnected(ulong clientId)
		{
			// Steam Relay 또는 Lobby 연결이 끊겼을 때 재현 로그를 남기기 위한 해제 로그.
			Debug.LogWarning($"NGO Steam bootstrap: client disconnected, clientId={clientId}");
		}

		private void StartNetworkRole(NetworkManager networkManager)
		{
			// Steam Lobby owner를 NGO Host로 매핑하는 현재 권한 규칙.
			// Client는 Lobby owner Steam ID를 Facepunch targetSteamId에 넣은 뒤 StartClient로 Relay 접속.
			// Steam Lobby owner를 NGO Host로 매핑하는 현재 권한 규칙.
			var ownerSteamId = steamLobbyService.GetLobbyOwnerSteamId();
			var localSteamId = steamworksClientService.LocalSteamId;
			var facepunchTransport = networkManager.GetComponent<FacepunchTransport>();

			Debug.Log($"NGO Steam bootstrap: lobbyOwnerSteamId={ownerSteamId}, localSteamId={localSteamId}, isLobbyOwner={steamLobbyService.IsLobbyOwner}");

			if (steamLobbyService.IsLobbyOwner)
			{
				// 로비 owner의 Host 시작.
				var started = networkManager.StartHost();
				Debug.Log($"NGO Steam bootstrap: StartHost result={started}");
				return;
			}

			// non-owner 클라이언트의 로비 owner Steam ID 대상 연결.
			facepunchTransport.targetSteamId = ownerSteamId;
			var clientStarted = networkManager.StartClient();
			Debug.Log($"NGO Steam bootstrap: StartClient result={clientStarted}, targetSteamId={facepunchTransport.targetSteamId}");
		}

		private async UniTask<GameObject> WaitForLocalPlayerAsync(
			NetworkManager networkManager,
			CancellationToken cancellationToken)
		{
			// NetworkManager.StartHost/StartClient 직후에는 LocalPlayerObject가 아직 없을 수 있어 짧게 대기.
			// 성공 시 StageManager가 카메라 target을 이 오브젝트로 바꾸고, 실패 시 첫 sync 단계 진입을 포기.
			// 카메라 타겟 연결을 위한 로컬 PlayerObject 대기.
			var timeoutTime = Time.realtimeSinceStartup + 15f;

			while (Time.realtimeSinceStartup < timeoutTime)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var localPlayerObject = networkManager.SpawnManager != null
					? networkManager.SpawnManager.GetLocalPlayerObject()
					: null;

				if (localPlayerObject != null)
				{
					Debug.Log($"NGO Steam bootstrap: local player ready, ownerClientId={localPlayerObject.OwnerClientId}");
					return localPlayerObject.gameObject;
				}

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}

			Debug.LogWarning("NGO Steam bootstrap: local player spawn wait timed out.");
			return null;
		}
	}
}
