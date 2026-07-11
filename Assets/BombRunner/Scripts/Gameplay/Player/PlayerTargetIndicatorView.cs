using UnityEngine;
using UnityEngine.Serialization;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(PlayerStateController))]
	public sealed class PlayerTargetIndicatorView : MonoBehaviour
	{
		[SerializeField] private GameObject targetIndicatorRoot;
		[FormerlySerializedAs("invulnerableIndicatorRoot")]
		[SerializeField] private GameObject tagImmuneIndicatorRoot;

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

			if (tagImmuneIndicatorRoot != null)
			{
				tagImmuneIndicatorRoot.SetActive(isAlive && stateController.IsTagImmune);
			}
		}
	}
}
