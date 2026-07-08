using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	[CreateAssetMenu(fileName = "PlayerSpawnSettings", menuName = "Boom Runner/Player Spawn Settings")]
	public sealed class PlayerSpawnSettings : ScriptableObject
	{
		// 로컬 이동 검증용 플레이어 프리팹입니다. 이후 네트워크 스폰 프리팹으로 교체합니다.
		[SerializeField] private GameObject playerPrefab;

		// Game Scene에서 첫 로컬 플레이어가 등장할 위치입니다.
		[SerializeField] private Vector3 spawnPosition = Vector3.zero;

		[SerializeField] private float moveSpeed = 5f;
		[SerializeField] private float rotationSpeed = 720f;
		[SerializeField] private float dashDistance = 4f;
		[SerializeField] private float dashDuration = 0.14f;
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
