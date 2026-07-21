using BombRunner.Scripts.Input;
using UnityEngine;
using VContainer;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(PlayerStateController))]
	// 로컬 도발 hold 입력을 PlayerStateController의 taunting 상태로 반영하는 컨트롤러.
	public sealed class PlayerTauntController : MonoBehaviour
	{
		private IInputService inputService;
		private PlayerStateController stateController;
		private bool hasInputService;
		private bool isInputEnabled = true;

		[Inject]
		public void Construct(IInputService inputService)
		{
			this.inputService = inputService;
			hasInputService = true;
		}

		public void SetInputEnabled(bool isInputEnabled)
		{
			this.isInputEnabled = isInputEnabled;

			if (!isInputEnabled && stateController != null)
			{
				stateController.SetTaunting(false);
			}
		}

		private void Awake()
		{
			stateController = GetComponent<PlayerStateController>();
		}

		private void Update()
		{
			// 입력 비활성, 다운, 스턴 상태의 도발 해제.
			if (!hasInputService
				|| !isInputEnabled
				|| stateController == null
				|| !stateController.IsAlive
				|| stateController.IsStunned)
			{
				SetTaunting(false);
				return;
			}

			SetTaunting(inputService.TauntHeld);
		}

		private void OnDisable()
		{
			SetTaunting(false);
		}

		private void SetTaunting(bool isTaunting)
		{
			if (stateController != null)
			{
				stateController.SetTaunting(isTaunting);
			}
		}
	}
}
