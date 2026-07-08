using UnityEngine;
using UnityEngine.InputSystem;

namespace BombRunner.Scripts.Input
{
	public sealed class PlayerInputReader : MonoBehaviour
	{
		[SerializeField] private InputActionAsset inputActions;

		private InputActionMap playerActionMap;
		private InputAction moveAction;
		private InputAction dashAction;
		private bool isReady;

		private enum InputActionMapKind
		{
			Player
		}

		private enum PlayerInputAction
		{
			Move,
			Dash
		}

		public Vector2 Move
		{
			get
			{
				EnsureReady();
				return moveAction.ReadValue<Vector2>();
			}
		}

		public bool DashPressedThisFrame
		{
			get
			{
				EnsureReady();
				return dashAction.WasPressedThisFrame();
			}
		}

		private void Awake()
		{
			EnsureReady();
		}

		private void OnEnable()
		{
			EnsureReady();

			if (playerActionMap == null)
			{
				return;
			}

			playerActionMap.Enable();
		}

		private void OnDisable()
		{
			if (playerActionMap == null)
			{
				return;
			}

			playerActionMap.Disable();
		}

		// New Input System 의존성을 이 Reader 안에 가둬 PlayerController가 액션을 직접 보지 않게 한다.
		private void EnsureReady()
		{
			if (isReady)
			{
				return;
			}

			if (inputActions == null)
			{
				inputActions = Resources.Load<InputActionAsset>(InputAssetResourcePath.DefaultPlayerInput);
			}

			if (inputActions == null)
			{
				Debug.LogError("BombRunnerInput InputActionAsset을 찾을 수 없습니다.");
				return;
			}

			playerActionMap = inputActions.FindActionMap(InputActionMapKind.Player.ToString(), true);
			moveAction = playerActionMap.FindAction(nameof(PlayerInputAction.Move), true);
			dashAction = playerActionMap.FindAction(nameof(PlayerInputAction.Dash), true);
			isReady = true;
		}
	}
}
