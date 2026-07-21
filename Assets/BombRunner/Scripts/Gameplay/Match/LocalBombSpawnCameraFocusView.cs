using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	// 폭탄 스폰 위치로 카메라 포커스를 임시 이동시키는 씬 배치 View.
	public sealed class LocalBombSpawnCameraFocusView : MonoBehaviour
	{
		public Transform Target => transform;

		public void SetPosition(Vector3 position)
		{
			transform.position = position;
		}
	}
}
