using BombRunner.Scripts.Input;
using UnityEngine;
using VContainer;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(PlayerStateController))]
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
			if (!hasInputService || !isInputEnabled || stateController == null || !stateController.IsAlive)
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
