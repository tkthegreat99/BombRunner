using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalBombSpawnCameraFocusView : MonoBehaviour
	{
		public Transform Target => transform;

		public void SetPosition(Vector3 position)
		{
			transform.position = position;
		}
	}
}
