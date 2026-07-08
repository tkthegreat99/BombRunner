using System;
using System.Threading;
using BombRunner.Scripts.Input;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace BombRunner.Scripts.Gameplay.Player
{
	[RequireComponent(typeof(CharacterController))]
	[RequireComponent(typeof(PlayerMovementController))]
	public sealed class PlayerDashController : MonoBehaviour
	{
		[SerializeField] private float dashDistance = 4f;
		[SerializeField] private float dashDuration = 0.14f;
		[SerializeField] private float dashCooldown = 0.8f;

		private IInputService inputService;
		private CharacterController characterController;
		private PlayerMovementController movementController;
		private bool hasInputService;
		private bool isDashing;
		private bool isCoolingDown;
		private float cooldownEndTime;

		public bool IsDashing => isDashing;
		public bool IsCoolingDown => isCoolingDown;
		public bool IsDashReady => !isDashing && !isCoolingDown;
		public float CooldownRemaining => isCoolingDown ? Mathf.Max(0f, cooldownEndTime - Time.time) : 0f;

		[Inject]
		public void Construct(IInputService inputService)
		{
			this.inputService = inputService;
			hasInputService = true;
		}

		public void Initialize(float dashDistance, float dashDuration, float dashCooldown)
		{
			this.dashDistance = dashDistance;
			this.dashDuration = dashDuration;
			this.dashCooldown = dashCooldown;
		}

		private void Awake()
		{
			characterController = GetComponent<CharacterController>();
			movementController = GetComponent<PlayerMovementController>();
		}

		private void Update()
		{
			if (!hasInputService || !inputService.DashPressed)
			{
				return;
			}

			TryDashAsync(this.GetCancellationTokenOnDestroy()).Forget();
		}

		// 대시는 Coroutine 대신 UniTask와 CancellationToken으로 시간 흐름을 제어한다.
		private async UniTaskVoid TryDashAsync(CancellationToken cancellationToken)
		{
			if (isDashing || isCoolingDown)
			{
				return;
			}

			isDashing = true;

			try
			{
				await RunDashMoveAsync(movementController.LastMoveDirection, cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			finally
			{
				isDashing = false;
			}

			await RunCooldownAsync(cancellationToken);
		}

		// 짧은 시간 동안 현재 바라보는 방향으로 CharacterController를 밀어낸다.
		private async UniTask RunDashMoveAsync(Vector3 direction, CancellationToken cancellationToken)
		{
			var elapsedTime = 0f;
			var safeDuration = Mathf.Max(0.01f, dashDuration);
			var dashSpeed = dashDistance / safeDuration;

			while (elapsedTime < safeDuration)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var deltaTime = Time.deltaTime;
				elapsedTime += deltaTime;
				characterController.Move(direction * (dashSpeed * deltaTime));

				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}
		}

		// 쿨타임 동안 재대시를 막아 폭탄 추격전의 리듬을 만든다.
		private async UniTask RunCooldownAsync(CancellationToken cancellationToken)
		{
			isCoolingDown = true;
			cooldownEndTime = Time.time + Mathf.Max(0f, dashCooldown);

			try
			{
				var delay = TimeSpan.FromSeconds(Mathf.Max(0f, dashCooldown));
				await UniTask.Delay(delay, cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				isCoolingDown = false;
				cooldownEndTime = 0f;
			}
		}
	}
}
