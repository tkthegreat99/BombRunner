using BombRunner.Scripts.Input;
using UnityEngine;
using VContainer;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(CharacterController))]
	public sealed class PlayerMovementController : MonoBehaviour
	{
		[SerializeField] private float moveSpeed = 5f;
		[SerializeField] private float rotationSpeed = 720f;

		private IInputService inputService;
		private CharacterController characterController;
		private PlayerDashController dashController;
		private PlayerStateController stateController;
		private Vector3 lastMoveDirection = Vector3.forward;
		private bool hasInputService;
		private bool isInputEnabled = true;

		public Vector3 LastMoveDirection => lastMoveDirection;

		[Inject]
		public void Construct(IInputService inputService)
		{
			this.inputService = inputService;
			hasInputService = true;
		}

		public void Initialize(float moveSpeed, float rotationSpeed)
		{
			this.moveSpeed = moveSpeed;
			this.rotationSpeed = rotationSpeed;
		}

		public void SetInputEnabled(bool isInputEnabled)
		{
			this.isInputEnabled = isInputEnabled;

			if (!isInputEnabled && stateController != null)
			{
				stateController.SetMoving(false);
			}
		}

		private void Awake()
		{
			characterController = GetComponent<CharacterController>();
			TryGetComponent(out dashController);
			TryGetComponent(out stateController);
		}

		private void Update()
		{
			if (!hasInputService || !isInputEnabled)
			{
				return;
			}

			if (dashController != null && dashController.IsDashing)
			{
				if (stateController != null)
				{
					stateController.SetMoving(false);
				}

				return;
			}

			var moveInput = inputService.Move;
			var moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
			var sqrMagnitude = moveDirection.sqrMagnitude;

			if (sqrMagnitude > 1f)
			{
				moveDirection.Normalize();
				sqrMagnitude = 1f;
			}

			var isMoving = sqrMagnitude > 0.0001f;

			if (stateController != null)
			{
				stateController.SetMoving(isMoving);
			}

			// XZ 평면 이동만 처리하고, 입력이 없을 때는 마지막 이동 방향을 유지한다.
			if (isMoving)
			{
				lastMoveDirection = moveDirection;
				RotateToMoveDirection(moveDirection);
			}

			characterController.Move(moveDirection * (moveSpeed * Time.deltaTime));
		}

		private void RotateToMoveDirection(Vector3 moveDirection)
		{
			var targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
			transform.rotation = Quaternion.RotateTowards(
				transform.rotation,
				targetRotation,
				rotationSpeed * Time.deltaTime);
		}
	}
}
