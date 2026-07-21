using UnityEngine;

namespace BombRunner.Scripts.Bomb
{
	[CreateAssetMenu(fileName = "BombSpawnSettings", menuName = "Boom Runner/Bomb Spawn Settings")]
	// 폭탄 프리팹 참조를 보관하는 ScriptableObject 설정.
	public sealed class BombSpawnSettings : ScriptableObject
	{
		// 임시 폭탄 Prefab입니다. 이후 네트워크 스폰/풀링 대상 Prefab으로 계속 확장합니다.
		[SerializeField] private GameObject bombPrefab;

		public GameObject BombPrefab => bombPrefab;
	}
}
