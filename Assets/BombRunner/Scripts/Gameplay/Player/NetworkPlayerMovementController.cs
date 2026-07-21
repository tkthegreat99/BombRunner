using BombRunner.Scripts.Input;
using Unity.Netcode;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	// Steam NGO 첫 동기화 테스트를 위한 Host 권한 플레이어 이동 컨트롤러.
	[RequireComponent(typeof(NetworkObject))]
	[RequireComponent(typeof(CharacterController))]
	[RequireComponent(typeof(PlayerStateController))]
	public sealed class NetworkPlayerMovementController : NetworkBehaviour
	{
		private const float PositionSnapDistance = 3f;
		private const float RemoteFollowSpeed = 18f;
		private const int MaxDisplayNameLength = 24;
		private static readonly Vector3 NameplateLocalOffset = new(0f, 2.45f, 0f);

		// Host가 확정한 위치를 모든 클라이언트가 따라가기 위한 복제 상태.
		// Host가 확정한 위치를 모든 클라이언트가 따라가기 위한 복제 상태.
		// 현재는 예측/보정 없는 첫 이동 동기화 단계라 위치와 yaw만 최소 전송.
		private NetworkVariable<Vector3> serverPosition = new(
			Vector3.zero,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server);

		// Host가 확정한 Yaw 회전값을 모든 클라이언트가 따라가기 위한 복제 상태.
		// Host가 확정한 Yaw 회전값을 모든 클라이언트가 따라가기 위한 복제 상태.
		// 캐릭터가 바라보는 방향만 맞추면 첫 친구 테스트에서 조작 주체를 충분히 구분 가능.
		private NetworkVariable<float> serverYaw = new(
			0f,
			NetworkVariableReadPermission.Everyone,
			NetworkVariableWritePermission.Server);

		private CharacterController characterController;
		private PlayerStateController stateController;
		private PlayerInputReader inputReader;
		private TextMesh nameplateText;
		private Transform nameplateCameraTransform;
		private NetworkManager callbackNetworkManager;
		private string confirmedDisplayName = "Player";
		private float moveSpeed = 5f;
		private float rotationSpeed = 720f;
		private float downedMoveSpeedMultiplier = 0.35f;
		private bool isReady;

		private void Awake()
		{
			characterController = GetComponent<CharacterController>();
			stateController = GetComponent<PlayerStateController>();
		}

		public override void OnNetworkSpawn()
		{
			// 기존 local prototype 컴포넌트와 NGO 소유자 입력이 동시에 움직이면 위치가 두 번 적용된다.
			// 네트워크 매치에서는 이 컴포넌트 하나가 입력 제출, Host 이동 판정, 원격 보간을 모두 담당.
			// 기존 로컬 프로토타입 입력과 NGO 입력이 동시에 움직이는 상황 방지.
			DisableLocalPrototypeInput();

			// NGO가 직접 생성한 PlayerPrefab에 ScriptableObject 기반 이동값 적용.
			ApplyRuntimeSettings();
			nameplateCameraTransform = NetworkPlayerRuntimeSettings.NameplateCameraTransform;

			// 오늘 친구 테스트용 머리 위 이름표 생성. 최종 UI 방향이 정해지면 prefab View로 교체 예정.
			EnsureNameplate();

			// 복제 이름이 오기 전에도 상태 View와 로그에서 플레이어 구분이 가능하도록 임시 라벨 지정.
			confirmedDisplayName = GetInitialDisplayName();
			ApplyLocalLabel();
			ApplyDisplayName(confirmedDisplayName);

			if (IsServer)
			{
				// Host가 모든 플레이어의 시작 위치를 정하는 스폰 권한 지점.
				var spawnPosition = NetworkPlayerRuntimeSettings.GetSpawnPosition(OwnerClientId);
				transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
				PublishServerTransform();
				SubscribeClientConnectedCallback();
			}

			if (IsOwner)
			{
				// 소유 클라이언트만 로컬 입력 Reader를 사용하고, 원격 플레이어는 표시만 수행.
				inputReader = NetworkPlayerRuntimeSettings.InputReader;

				if (inputReader == null)
				{
					Debug.LogError("Network player input reader is missing. Check GameLifetimeScope PlayerInputReader wiring.");
				}

				// Steam persona 이름을 Host에게 제출하면 Host가 문자열을 보정한 뒤 모든 클라이언트에 방송.
				SubmitLocalDisplayName();
			}

			isReady = true;
			Debug.Log($"Network player spawned: ownerClientId={OwnerClientId}, isOwner={IsOwner}, isServer={IsServer}, localSteamId={NetworkPlayerRuntimeSettings.LocalSteamId}, localName={NetworkPlayerRuntimeSettings.LocalDisplayName}");
		}

		public override void OnNetworkDespawn()
		{
			UnsubscribeClientConnectedCallback();
		}

		private void Update()
		{
			if (!isReady)
			{
				return;
			}

			// 입력은 소유자만 제출하고, 실제 이동 판정과 Transform 발행은 Host가 수행.
			if (IsOwner)
			{
				UpdateOwnerInput();
			}

			// Host가 아닌 인스턴스는 Host가 복제한 위치와 회전을 표시용으로 추종.
			if (!IsServer)
			{
				FollowServerTransform();
			}
		}

		private void LateUpdate()
		{
			// Camera.main 또는 Find 계열 호출 없이 부트스트랩에서 받은 카메라 Transform만 사용.
			// LateUpdate마다 카메라를 검색하면 비용도 들고, 씬 카메라가 바뀔 때 예측하기 어려운 참조가 생김.
			// 부트스트랩에서 주입한 카메라 Transform을 사용해 매 프레임 검색 없이 이름표 방향 보정.
			UpdateNameplateFacing();
		}

		private void UpdateOwnerInput()
		{
			if (inputReader == null)
			{
				return;
			}

			var moveInput = inputReader.Move;

			if (IsServer)
			{
				// Host 자신의 플레이어는 RPC 왕복 없이 같은 권한 이동 함수를 직접 호출.
				ApplyServerMove(moveInput, Time.deltaTime);
				return;
			}

			// Client 플레이어는 입력 의도만 Host로 보내고 위치는 Host 복제를 따름.
			// Client 플레이어는 입력 의도만 Host에 보내고 위치는 Host 복제를 따름.
			// 이 방식은 지연 보상은 없지만, 오늘 목표인 "서로 움직이는 것을 본다"에 맞는 최소 권한 구조.
			SubmitMoveServerRpc(moveInput);
		}

		[ServerRpc]
		private void SubmitMoveServerRpc(Vector2 moveInput)
		{
			// Client 입력 의도를 Host의 CharacterController 이동 판정으로 전달.
			ApplyServerMove(moveInput, Time.deltaTime);
		}

		[ServerRpc]
		private void SubmitDisplayNameServerRpc(string requestedDisplayName)
		{
			// Client가 보낸 Steam persona를 Host가 공백/길이 보정 후 최종 표시 이름으로 확정.
			SetDisplayNameOnServer(requestedDisplayName);
		}

		[ClientRpc]
		private void ApplyDisplayNameClientRpc(string displayNameText, ClientRpcParams clientRpcParams = default)
		{
			// Host가 확정한 이름을 각 클라이언트의 상태 라벨과 머리 위 TextMesh에 반영.
			confirmedDisplayName = displayNameText;
			ApplyDisplayName(confirmedDisplayName);
		}

		private void ApplyServerMove(Vector2 moveInput, float deltaTime)
		{
			// Host/Master가 이동 불가 상태를 최종 반영하는 권한 지점.
			if (stateController != null && (stateController.IsTaunting || stateController.IsStunned))
			{
				stateController.SetMoving(false);
				PublishServerTransform();
				return;
			}

			var moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
			var sqrMagnitude = moveDirection.sqrMagnitude;

			// 대각선 입력으로 속도가 빨라지지 않도록 단위 벡터로 보정.
			if (sqrMagnitude > 1f)
			{
				moveDirection.Normalize();
				sqrMagnitude = 1f;
			}

			var isMoving = sqrMagnitude > 0.0001f;

			if (stateController != null)
			{
				stateController.SetMoving(isMoving);
			}

			if (isMoving)
			{
				RotateToMoveDirection(moveDirection, deltaTime);
			}

			var currentMoveSpeed = moveSpeed;

			// 다운 상태 플레이어는 기존 로컬 규칙처럼 느리게 기어다니는 이동만 허용.
			if (stateController != null && stateController.IsDowned)
			{
				currentMoveSpeed *= downedMoveSpeedMultiplier;
			}

			characterController.Move(moveDirection * (currentMoveSpeed * deltaTime));
			PublishServerTransform();
		}

		private void RotateToMoveDirection(Vector3 moveDirection, float deltaTime)
		{
			var targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
			transform.rotation = Quaternion.RotateTowards(
				transform.rotation,
				targetRotation,
				rotationSpeed * deltaTime);
		}

		private void PublishServerTransform()
		{
			// 클라이언트 표시가 따라갈 Host 확정 Transform 발행.
			serverPosition.Value = transform.position;
			serverYaw.Value = transform.eulerAngles.y;
		}

		private void FollowServerTransform()
		{
			// 원격 플레이어는 NetworkVariable에 담긴 Host 위치를 부드럽게 따라감.
			// 거리가 크게 벌어지면 보간 대신 snap해서 접속 직후나 패킷 누락 뒤의 긴 미끄러짐을 방지.
			// 큰 오차는 즉시 보정하고, 작은 오차는 부드럽게 따라가도록 친구 테스트의 위치 튐을 완화.
			var targetPosition = serverPosition.Value;
			var distance = Vector3.Distance(transform.position, targetPosition);

			if (distance > PositionSnapDistance)
			{
				transform.position = targetPosition;
			}
			else
			{
				transform.position = Vector3.Lerp(
					transform.position,
					targetPosition,
					RemoteFollowSpeed * Time.deltaTime);
			}

			var targetRotation = Quaternion.Euler(0f, serverYaw.Value, 0f);
			transform.rotation = Quaternion.RotateTowards(
				transform.rotation,
				targetRotation,
				rotationSpeed * Time.deltaTime);
		}

		private void ApplyRuntimeSettings()
		{
			// Steam NGO 부트스트랩이 준비한 로컬 설정이 없으면 prefab 기본값으로 동작.
			if (!NetworkPlayerRuntimeSettings.IsConfigured)
			{
				return;
			}

			moveSpeed = NetworkPlayerRuntimeSettings.MoveSpeed;
			rotationSpeed = NetworkPlayerRuntimeSettings.RotationSpeed;
			downedMoveSpeedMultiplier = NetworkPlayerRuntimeSettings.DownedMoveSpeedMultiplier;
		}

		private void ApplyLocalLabel()
		{
			// displayName RPC 도착 전에도 상태 View와 로그에서 누군지 구분되도록 초기 라벨 지정.
			if (stateController == null)
			{
				return;
			}

			stateController.SetPlayerLabel(confirmedDisplayName);
			stateController.SetLifeState(PlayerLifeState.Alive);
			stateController.SetMoving(false);
			stateController.SetDashing(false);
			stateController.SetTagImmune(false);
			stateController.SetTarget(false);
			stateController.SetTaunting(false);
			stateController.SetDashLocked(false);
		}

		private void SubmitLocalDisplayName()
		{
			// Host 소유 PlayerObject는 즉시 확정하고, Client 소유 PlayerObject는 ServerRpc로 Host에게 요청.
			var localDisplayName = GetInitialDisplayName();

			if (IsServer)
			{
				SetDisplayNameOnServer(localDisplayName);
				return;
			}

			SubmitDisplayNameServerRpc(localDisplayName);
		}

		private void SetDisplayNameOnServer(string requestedDisplayName)
		{
			// Steam persona가 비어 있거나 너무 길 때도 친구 테스트 화면이 깨지지 않도록 표시 이름 보정.
			confirmedDisplayName = NormalizeDisplayName(requestedDisplayName);
			ApplyDisplayName(confirmedDisplayName);
			ApplyDisplayNameClientRpc(confirmedDisplayName);
			Debug.Log($"Network player display name set: ownerClientId={OwnerClientId}, displayName={confirmedDisplayName}");
		}

		private string NormalizeDisplayName(string requestedDisplayName)
		{
			var displayNameText = string.IsNullOrWhiteSpace(requestedDisplayName)
				? $"Player {OwnerClientId}"
				: requestedDisplayName.Trim();

			if (displayNameText.Length > MaxDisplayNameLength)
			{
				displayNameText = displayNameText.Substring(0, MaxDisplayNameLength);
			}

			return displayNameText;
		}

		private void ApplyDisplayName(string displayNameText)
		{
			var safeDisplayName = NormalizeDisplayName(displayNameText);

			if (stateController != null)
			{
				stateController.SetPlayerLabel(safeDisplayName);
			}

			if (nameplateText != null)
			{
				nameplateText.text = safeDisplayName;
				nameplateText.color = IsOwner
					? new Color(0.55f, 1f, 0.72f, 1f)
					: Color.white;
			}
		}

		private string GetInitialDisplayName()
		{
			// 소유자는 Steam persona를 우선 사용하고, 원격 플레이어는 복제 이름 수신 전 임시 라벨 사용.
			if (IsOwner && NetworkPlayerRuntimeSettings.IsConfigured)
			{
				return NetworkPlayerRuntimeSettings.LocalDisplayName;
			}

			return $"Player {OwnerClientId}";
		}

		private void SubscribeClientConnectedCallback()
		{
			// 새 클라이언트가 늦게 들어와도 기존 플레이어 이름표를 다시 받을 수 있도록 서버에서 재전송 준비.
			callbackNetworkManager = NetworkManager.Singleton;

			if (callbackNetworkManager == null)
			{
				return;
			}

			callbackNetworkManager.OnClientConnectedCallback -= OnClientConnected;
			callbackNetworkManager.OnClientConnectedCallback += OnClientConnected;
		}

		private void UnsubscribeClientConnectedCallback()
		{
			// PlayerObject despawn 시 콜백이 남아 이름 재전송이 중복되지 않도록 정리.
			if (callbackNetworkManager == null)
			{
				return;
			}

			callbackNetworkManager.OnClientConnectedCallback -= OnClientConnected;
			callbackNetworkManager = null;
		}

		private void OnClientConnected(ulong clientId)
		{
			// 새로 접속한 클라이언트에게 이 PlayerObject의 확정 이름만 타겟 전송.
			var clientRpcParams = new ClientRpcParams
			{
				Send = new ClientRpcSendParams
				{
					TargetClientIds = new[] { clientId }
				}
			};

			ApplyDisplayNameClientRpc(confirmedDisplayName, clientRpcParams);
		}

		private void EnsureNameplate()
		{
			if (nameplateText != null)
			{
				return;
			}

			// 오늘 친구 테스트용 임시 이름표. 최종 HUD/월드 UI 방향이 정해지면 prefab View로 교체.
			// 오늘 친구 테스트용 임시 이름표. 최종 HUD/월드 UI 방향이 정해지면 prefab View로 교체.
			// Runtime 생성은 원칙적으로 피하지만, 현재는 네트워크 prefab 수정 폭을 줄이기 위한 제한적 예외.
			var nameplateObject = new GameObject("NetworkNameplate");
			var nameplateTransform = nameplateObject.transform;
			nameplateTransform.SetParent(transform, false);
			nameplateTransform.localPosition = NameplateLocalOffset;
			nameplateText = nameplateObject.AddComponent<TextMesh>();

			nameplateText.anchor = TextAnchor.MiddleCenter;
			nameplateText.alignment = TextAlignment.Center;
			nameplateText.fontSize = 48;
			nameplateText.characterSize = 0.065f;
			nameplateText.text = "Player";
			nameplateText.color = Color.white;
			nameplateText.gameObject.SetActive(true);
		}

		private void UpdateNameplateFacing()
		{
			if (nameplateText == null || nameplateCameraTransform == null)
			{
				return;
			}

			nameplateText.transform.localPosition = NameplateLocalOffset;
			var direction = nameplateText.transform.position - nameplateCameraTransform.position;

			if (direction.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			nameplateText.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		}

		private void DisableLocalPrototypeInput()
		{
			// PrototypePlayer prefab에는 기존 로컬 입력 컴포넌트가 같이 붙어 있다.
			// NGO 매치에서는 해당 컴포넌트의 입력만 끄고 상태/표시 컴포넌트는 재사용해 첫 sync 범위를 작게 유지.
			// 네트워크 매치에서는 기존 로컬 이동/대시/도발 입력 컴포넌트를 모두 비활성화.
			if (TryGetComponent<PlayerMovementController>(out var movementController))
			{
				movementController.SetInputEnabled(false);
			}

			if (TryGetComponent<PlayerDashController>(out var dashController))
			{
				dashController.SetInputEnabled(false);
			}

			if (TryGetComponent<PlayerTauntController>(out var tauntController))
			{
				tauntController.SetInputEnabled(false);
			}
		}
	}
}
