using UnityEngine;
using UnityEngine.Serialization;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(PlayerStateController))]
	// 플레이어의 타겟 표시와 태그 면역 게이지를 갱신하는 View.
	public sealed class PlayerTargetIndicatorView : MonoBehaviour
	{
		[SerializeField] private GameObject targetIndicatorRoot;
		[FormerlySerializedAs("invulnerableIndicatorRoot")]
		[SerializeField] private GameObject tagImmuneIndicatorRoot;
		[SerializeField] private Transform tagImmuneGaugeFill;

		private PlayerStateController stateController;
		private Transform activeTagImmuneGauge;
		private Vector3 tagImmuneGaugeFullScale = Vector3.one;

		private void Awake()
		{
			stateController = GetComponent<PlayerStateController>();
			activeTagImmuneGauge = tagImmuneGaugeFill != null
				? tagImmuneGaugeFill
				: tagImmuneIndicatorRoot != null ? tagImmuneIndicatorRoot.transform : null;

			if (activeTagImmuneGauge != null)
			{
				tagImmuneGaugeFullScale = activeTagImmuneGauge.localScale;
			}
		}

		private void OnEnable()
		{
			stateController.Changed += ApplyState;
			ApplyState();
		}

		private void OnDisable()
		{
			stateController.Changed -= ApplyState;
		}

		private void Update()
		{
			if (stateController.IsAlive && stateController.IsTagImmune)
			{
				ApplyTagImmuneGauge();
			}
		}

		private void ApplyState()
		{
			var isAlive = stateController.IsAlive;

			if (targetIndicatorRoot != null)
			{
				targetIndicatorRoot.SetActive(isAlive && stateController.IsTarget);
			}

			if (tagImmuneIndicatorRoot != null)
			{
				tagImmuneIndicatorRoot.SetActive(isAlive && stateController.IsTagImmune);
			}

			ApplyTagImmuneGauge();
		}

		private void ApplyTagImmuneGauge()
		{
			// 남은 태그 면역 시간을 scale 비율로 표시.
			if (activeTagImmuneGauge == null)
			{
				return;
			}

			var remaining = stateController.IsAlive && stateController.IsTagImmune
				? stateController.TagImmuneNormalizedRemaining
				: 1f;
			activeTagImmuneGauge.localScale = tagImmuneGaugeFullScale * Mathf.Clamp01(remaining);
		}
	}
}
