using UnityEngine;

namespace BombRunner.Scripts.Camera
{
	public sealed class LocalPlayerCameraFollow : MonoBehaviour
	{
		[SerializeField] private Vector3 followOffset = new(0f, 9f, -10f);
		[SerializeField] private Vector3 cameraEulerAngles = new(50f, 0f, 0f);
		[SerializeField] private float followSpeed = 12f;
		[SerializeField] private bool snapOnTargetAssigned = true;

		private Transform target;

		private void Awake()
		{
			ApplyCameraAngle();
		}

		public void SetTarget(Transform target)
		{
			SetTarget(target, snapOnTargetAssigned);
		}

		public void SetTarget(Transform target, bool snapToTarget)
		{
			this.target = target;
			ApplyCameraAngle();

			if (target == null || !snapToTarget)
			{
				return;
			}

			// 테스트 시작 시 플레이어가 화면 안에 바로 보이도록 첫 위치는 즉시 맞춘다.
			transform.position = target.position + followOffset;
		}

		private void LateUpdate()
		{
			if (target == null)
			{
				return;
			}

			var targetPosition = target.position + followOffset;
			transform.position = Vector3.Lerp(
				transform.position,
				targetPosition,
				followSpeed * Time.deltaTime);
		}

		private void ApplyCameraAngle()
		{
			// Duckov처럼 완전 수직이 아닌 사선 시점으로 고정한다.
			transform.rotation = Quaternion.Euler(cameraEulerAngles);
		}
	}
}
