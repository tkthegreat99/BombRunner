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
		private Vector3 lastMoveDirection = Vector3.forward;
		private bool hasInputService;

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

		private void Awake()
		{
			characterController = GetComponent<CharacterController>();
			TryGetComponent(out dashController);
		}

		private void Update()
		{
			if (!hasInputService)
			{
				return;
			}

			if (dashController != null && dashController.IsDashing)
			{
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

			// XZ 평면에서만 이동하며, 입력이 있을 때 마지막 방향을 대시 기준으로 저장한다.
			if (sqrMagnitude > 0.0001f)
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
