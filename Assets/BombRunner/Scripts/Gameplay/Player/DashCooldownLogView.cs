using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	// 대시 가능 상태 변화를 콘솔로 확인하는 임시 로그 View.
	public sealed class DashCooldownLogView : MonoBehaviour
	{
		private PlayerDashController dashController;
		private bool previousReadyState = true;
		private bool hasTarget;

		public void SetTarget(PlayerDashController dashController)
		{
			this.dashController = dashController;
			hasTarget = dashController != null;
			previousReadyState = !hasTarget || dashController.IsDashReady;
		}

		private void Update()
		{
			if (!hasTarget)
			{
				return;
			}

			var isReady = dashController.IsDashReady;

			if (isReady == previousReadyState)
			{
				return;
			}

			previousReadyState = isReady;

			if (isReady)
			{
				Debug.Log("Dash ready");
				return;
			}

			if (dashController.IsDashLocked || dashController.IsTaunting)
			{
				Debug.Log("Dash unavailable");
				return;
			}

			Debug.Log($"Dash cooldown started: {dashController.CooldownRemaining:0.00}s");
		}
	}
}
