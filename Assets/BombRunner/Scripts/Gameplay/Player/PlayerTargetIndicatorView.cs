using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(PlayerStateController))]
	public sealed class PlayerTargetIndicatorView : MonoBehaviour
	{
		[SerializeField] private GameObject targetIndicatorRoot;
		[SerializeField] private GameObject invulnerableIndicatorRoot;

		private PlayerStateController stateController;

		private void Awake()
		{
			stateController = GetComponent<PlayerStateController>();
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

		private void ApplyState()
		{
			var isAlive = stateController.IsAlive;

			if (targetIndicatorRoot != null)
			{
				targetIndicatorRoot.SetActive(isAlive && stateController.IsTarget);
			}

			if (invulnerableIndicatorRoot != null)
			{
				invulnerableIndicatorRoot.SetActive(isAlive && stateController.IsInvulnerable);
			}
		}
	}
}
