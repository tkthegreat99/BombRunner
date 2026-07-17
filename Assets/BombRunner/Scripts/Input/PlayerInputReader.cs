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
		private InputAction tauntAction;
		private InputAction useItemAction;
		private bool isReady;

		private enum InputActionMapKind
		{
			Player
		}

		private enum PlayerInputAction
		{
			Move,
			Dash,
			Taunt,
			UseItem
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

		public bool TauntHeld
		{
			get
			{
				EnsureReady();
				return tauntAction != null && tauntAction.IsPressed();
			}
		}

		public bool UseItemPressedThisFrame
		{
			get
			{
				EnsureReady();

				if (useItemAction != null)
				{
					return useItemAction.WasPressedThisFrame();
				}

				// UseItem InputAction 연결 전까지 사용하는 임시 E 키 입력.
				return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
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

		// New Input System 의존성을 Reader 안에 가두는 입력 경계.
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
			tauntAction = playerActionMap.FindAction(nameof(PlayerInputAction.Taunt), false);
			useItemAction = playerActionMap.FindAction(nameof(PlayerInputAction.UseItem), false);
			isReady = true;
		}
	}
}
