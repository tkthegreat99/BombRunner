using System;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Authority;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;

namespace BombRunner.Scripts.Bomb
{
	// 로컬 프로토타입 폭탄의 페이즈 진행, 추격, 폭발 판정을 담당하는 컨트롤러.
	public sealed class BombCreatureController : MonoBehaviour
	{
		private const float Acceleration = 15f;
		private const float Deceleration = 10f;
		private const float RotationSpeed = 720f;
		private const float ArriveDistance = 0.35f;
		private const float SlowRadius = 1.4f;
		private const float WarningPulseSpeed = 7f;
		private const float WarningPulseScale = 0.16f;

		private BombState bombState;
		private BombTargetService bombTargetService;
		private IMatchAuthorityService matchAuthorityService;
		private GameBalanceSettings balanceSettings;
		private PlayerStateController[] players;
		private Vector3 velocity;
		private Vector3 baseScale;
		private float phaseRemainingTime;
		private float temporarySpeedMultiplier = 1f;
		private float temporarySpeedBoostEndTime;
		private float nextDownedBoostTime;
		private bool isInitialized;
		private bool isActivated;
		private bool hasExploded;

		public event Action<BombCreatureController, PlayerStateController> Exploded;

		// 임시 폭탄 크리처. 이후 네트워크 스폰 프리팹과 풀링 대상으로 교체.
		public void Initialize(
			BombState bombState,
			BombTargetService bombTargetService,
			IMatchAuthorityService matchAuthorityService,
			GameBalanceSettings balanceSettings,
			PlayerStateController[] players)
		{
			this.bombState = bombState;
			this.bombTargetService = bombTargetService;
			this.matchAuthorityService = matchAuthorityService;
			this.balanceSettings = balanceSettings;
			this.players = players;
			velocity = Vector3.zero;
			baseScale = transform.localScale;
			phaseRemainingTime = 0f;
			temporarySpeedMultiplier = 1f;
			temporarySpeedBoostEndTime = 0f;
			nextDownedBoostTime = 0f;
			isActivated = false;
			hasExploded = false;
			isInitialized = bombState != null
				&& bombTargetService != null
				&& matchAuthorityService != null
				&& balanceSettings != null
				&& players != null
				&& players.Length > 0;

			if (!isInitialized)
			{
				return;
			}

			bombState.SetExplosionRadius(balanceSettings.ExplosionRadius);
		}

		private void Update()
		{
			// 활성화 이후 매 프레임 페이즈, 추격, 임시 연출 갱신.
			if (!isInitialized || !isActivated || hasExploded)
			{
				return;
			}

			UpdateTimer();

			if (hasExploded)
			{
				return;
			}

			TryTriggerDownedPlayerBoost();
			ChaseTarget();
			UpdateTemporaryWarningPulse();
		}

		public void Activate()
		{
			if (!isInitialized || isActivated || hasExploded)
			{
				return;
			}

			isActivated = true;
			SetTimerPhase(BombTimerPhase.Calm);
		}

		private void UpdateTimer()
		{
			phaseRemainingTime -= Time.deltaTime;

			if (phaseRemainingTime > 0f)
			{
				return;
			}

			AdvanceTimerPhaseOrExplode();
		}

		private void AdvanceTimerPhaseOrExplode()
		{
			// Calm, Warning, Overdrive 순서 후 폭발.
			if (bombState.TimerPhase == BombTimerPhase.Calm)
			{
				SetTimerPhase(BombTimerPhase.Warning);
				return;
			}

			if (bombState.TimerPhase == BombTimerPhase.Warning)
			{
				SetTimerPhase(BombTimerPhase.Overdrive);
				return;
			}

			Explode();
		}

		private void SetTimerPhase(BombTimerPhase timerPhase)
		{
			bombState.SetTimerPhase(timerPhase);
			phaseRemainingTime = matchAuthorityService.ResolveBombPhaseDuration(timerPhase);
			bombState.SetMoveSpeed(balanceSettings.GetMoveSpeed(timerPhase));
			Debug.Log($"Bomb timer phase: {timerPhase}, duration: {phaseRemainingTime:0.00}s");
		}

		private void Explode()
		{
			hasExploded = true;
			transform.localScale = baseScale;
			var downedPlayer = ResolveClosestPlayerDown();
			Exploded?.Invoke(this, downedPlayer);
		}

		private void ChaseTarget()
		{
			// 타겟이 없거나 다운되면 감속 정지.
			var targetPlayer = bombTargetService.TargetPlayer;

			if (targetPlayer == null || !targetPlayer.IsAlive)
			{
				ApplyDesiredVelocity(Vector3.zero);
				MoveByVelocity();
				return;
			}

			var currentPosition = transform.position;
			var targetPosition = targetPlayer.transform.position;
			targetPosition.y = currentPosition.y;

			var desiredVelocity = CalculateDesiredVelocity(currentPosition, targetPosition);
			ApplyDesiredVelocity(desiredVelocity);
			MoveByVelocity();

			RotateToVelocity();
		}

		private Vector3 CalculateDesiredVelocity(Vector3 currentPosition, Vector3 targetPosition)
		{
			var offset = targetPosition - currentPosition;
			var distance = offset.magnitude;

			if (distance <= ArriveDistance)
			{
				return Vector3.zero;
			}

			var speedScale = Mathf.Clamp01(distance / SlowRadius);
			return offset / distance * (bombState.MoveSpeed * temporarySpeedMultiplier * speedScale);
		}

		private void ApplyDesiredVelocity(Vector3 desiredVelocity)
		{
			var acceleration = desiredVelocity.sqrMagnitude > velocity.sqrMagnitude ? Acceleration : Deceleration;
			velocity = Vector3.MoveTowards(velocity, desiredVelocity, acceleration * Time.deltaTime);
		}

		private void MoveByVelocity()
		{
			transform.position += velocity * Time.deltaTime;
		}

		private void RotateToVelocity()
		{
			if (velocity.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			var targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
		}

		private void UpdateTemporaryWarningPulse()
		{
			// 임시 Warning 연출. 이후 Bomb Animator 또는 전용 VFX/Tween으로 교체.
			if (bombState.TimerPhase != BombTimerPhase.Warning)
			{
				transform.localScale = baseScale;
				return;
			}

			var pulse = 1f + Mathf.Sin(Time.time * WarningPulseSpeed) * WarningPulseScale;
			transform.localScale = baseScale * pulse;
		}

		private void TryTriggerDownedPlayerBoost()
		{
			if (Time.time < temporarySpeedBoostEndTime)
			{
				return;
			}

			temporarySpeedMultiplier = 1f;

			if (Time.time < nextDownedBoostTime)
			{
				return;
			}

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null || !player.IsDowned)
				{
					continue;
				}

				var offset = player.transform.position - transform.position;
				offset.y = 0f;

				if (offset.sqrMagnitude > balanceSettings.BombDownedBoostRadiusSqr)
				{
					continue;
				}

				// Host/Master 확정 대상인 폭탄 임시 가속 판정.
				temporarySpeedMultiplier = balanceSettings.BombDownedBoostSpeedMultiplier;
				temporarySpeedBoostEndTime = Time.time + balanceSettings.BombDownedBoostDurationSeconds;
				nextDownedBoostTime = Time.time + balanceSettings.BombDownedBoostCooldownSeconds;
				Debug.Log($"Bomb boost: stepped on downed player {player.PlayerLabel}");
				return;
			}
		}

		private PlayerStateController ResolveClosestPlayerDown()
		{
			// 폭발 범위 안 가장 가까운 생존자만 다운 처리.
			var closestPlayer = matchAuthorityService.ResolveExplosionVictim(
				transform.position,
				players,
				bombState.ExplosionRadius);

			if (closestPlayer == null)
			{
				Debug.Log("Bomb exploded: no alive player in explosion radius");
				return null;
			}

			if (!matchAuthorityService.SetPlayerDowned(closestPlayer))
			{
				return null;
			}

			Debug.Log($"Bomb exploded: {closestPlayer.PlayerLabel} downed as closest alive player");
			return closestPlayer;
		}

	}
}
