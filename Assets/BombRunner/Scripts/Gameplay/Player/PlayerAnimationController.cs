using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(PlayerStateController))]
	// PlayerStateController의 이동/대시 상태를 Animator 상태로 반영하는 View.
	public sealed class PlayerAnimationController : MonoBehaviour
	{
		[SerializeField] private Animator animator;
		[SerializeField] private string idleStateName = "Idle";
		[SerializeField] private string runStateName = "Run";
		[SerializeField] private string dashStateName = "Dash";
		[SerializeField] private string dashTriggerName = "Dash";
		[SerializeField] private float locomotionCrossFadeDuration = 0.08f;
		[SerializeField] private float dashCrossFadeDuration = 0.03f;

		private PlayerStateController stateController;
		private int idleStateHash;
		private int runStateHash;
		private int dashStateHash;
		private int dashTriggerHash;
		private bool hasDashTrigger;
		private bool wasMoving;
		private bool wasDashing;

		private void Awake()
		{
			stateController = GetComponent<PlayerStateController>();

			if (animator == null)
			{
				animator = GetComponentInChildren<Animator>();
			}

			idleStateHash = Animator.StringToHash(idleStateName);
			runStateHash = Animator.StringToHash(runStateName);
			dashStateHash = Animator.StringToHash(dashStateName);
			dashTriggerHash = Animator.StringToHash(dashTriggerName);
			hasDashTrigger = HasAnimatorParameter(dashTriggerHash, AnimatorControllerParameterType.Trigger);
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
			// 상태 변화가 있을 때만 Animator 전환 적용.
			if (animator == null)
			{
				return;
			}

			if (stateController.IsDashing && !wasDashing)
			{
				if (hasDashTrigger)
				{
					animator.SetTrigger(dashTriggerHash);
				}

				animator.CrossFadeInFixedTime(dashStateHash, dashCrossFadeDuration);
			}
			else if (!stateController.IsDashing && (wasDashing || stateController.IsMoving != wasMoving))
			{
				var nextStateHash = stateController.IsMoving ? runStateHash : idleStateHash;
				animator.CrossFadeInFixedTime(nextStateHash, locomotionCrossFadeDuration);
			}

			wasMoving = stateController.IsMoving;
			wasDashing = stateController.IsDashing;
		}

		private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType parameterType)
		{
			if (animator == null)
			{
				return false;
			}

			var parameters = animator.parameters;

			for (var i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i];

				if (parameter.nameHash == parameterHash && parameter.type == parameterType)
				{
					return true;
				}
			}

			return false;
		}
	}
}
