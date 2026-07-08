using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
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

			// 임시 HUD 대용 로그. 실제 HUD는 프리팹 기반 View가 준비되면 교체한다.
			if (isReady)
			{
				Debug.Log("Dash 준비 완료");
				return;
			}

			Debug.Log($"Dash 쿨타임 시작: {dashController.CooldownRemaining:0.00}초");
		}
	}
}
