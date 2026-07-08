using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	[CreateAssetMenu(fileName = "PlayerSpawnSettings", menuName = "Boom Runner/Player Spawn Settings")]
	public sealed class PlayerSpawnSettings : ScriptableObject
	{
		// 로컬 이동 검증용 프로토타입 프리팹. 이후 네트워크 스폰 프리팹으로 교체한다.
		[SerializeField] private GameObject playerPrefab;

		// Game Scene에서 첫 로컬 플레이어가 등장할 위치
		[SerializeField] private Vector3 spawnPosition = Vector3.zero;

		// XZ 평면 기본 이동 속도
		[SerializeField] private float moveSpeed = 5f;

		// 이동 방향으로 몸을 돌리는 회전 속도
		[SerializeField] private float rotationSpeed = 720f;

		// 대시 한 번에 전진하는 거리
		[SerializeField] private float dashDistance = 4f;

		// 대시 이동이 지속되는 시간
		[SerializeField] private float dashDuration = 0.14f;

		// 대시 재사용을 막는 쿨타임
		[SerializeField] private float dashCooldown = 0.8f;

		public GameObject PlayerPrefab => playerPrefab;
		public Vector3 SpawnPosition => spawnPosition;
		public float MoveSpeed => moveSpeed;
		public float RotationSpeed => rotationSpeed;
		public float DashDistance => dashDistance;
		public float DashDuration => dashDuration;
		public float DashCooldown => dashCooldown;
	}
}
