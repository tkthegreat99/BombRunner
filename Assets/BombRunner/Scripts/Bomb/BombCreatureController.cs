using System;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;

namespace BombRunner.Scripts.Bomb
{
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
			GameBalanceSettings balanceSettings,
			PlayerStateController[] players)
		{
			this.bombState = bombState;
			this.bombTargetService = bombTargetService;
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
			phaseRemainingTime = GetRandomPhaseDuration(timerPhase);
			bombState.SetMoveSpeed(GetPhaseMoveSpeed(timerPhase));
			Debug.Log($"Bomb timer phase: {timerPhase}, duration: {phaseRemainingTime:0.00}s");
		}

		private float GetRandomPhaseDuration(BombTimerPhase timerPhase)
		{
			switch (timerPhase)
			{
				case BombTimerPhase.Calm:
					return GetRandomRangeValue(balanceSettings.CalmDurationRange);
				case BombTimerPhase.Warning:
					return GetRandomRangeValue(balanceSettings.WarningDurationRange);
				case BombTimerPhase.Overdrive:
					return GetRandomRangeValue(balanceSettings.OverdriveDurationRange);
				default:
					return 0f;
			}
		}

		private float GetRandomRangeValue(Vector2 range)
		{
			var min = Mathf.Min(range.x, range.y);
			var max = Mathf.Max(range.x, range.y);
			return UnityEngine.Random.Range(min, max);
		}

		private float GetPhaseMoveSpeed(BombTimerPhase timerPhase)
		{
			switch (timerPhase)
			{
				case BombTimerPhase.Calm:
					return balanceSettings.CalmMoveSpeed;
				case BombTimerPhase.Warning:
					return balanceSettings.WarningMoveSpeed;
				case BombTimerPhase.Overdrive:
					return balanceSettings.OverdriveMoveSpeed;
				default:
					return balanceSettings.CalmMoveSpeed;
			}
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
			var closestPlayer = FindClosestAlivePlayerInExplosionRadius();

			if (closestPlayer == null)
			{
				Debug.Log("Bomb exploded: no alive player in explosion radius");
				return null;
			}

			closestPlayer.SetDowned();
			Debug.Log($"Bomb exploded: {closestPlayer.PlayerLabel} downed as closest alive player");
			return closestPlayer;
		}

		private PlayerStateController FindClosestAlivePlayerInExplosionRadius()
		{
			var center = transform.position;
			var radiusSqr = bombState.ExplosionRadius * bombState.ExplosionRadius;
			var closestPlayer = default(PlayerStateController);
			var closestDistanceSqr = float.MaxValue;

			for (var i = 0; i < players.Length; i++)
			{
				TrySelectClosestPlayer(players[i], center, radiusSqr, ref closestPlayer, ref closestDistanceSqr);
			}

			return closestPlayer;
		}

		private void TrySelectClosestPlayer(
			PlayerStateController player,
			Vector3 center,
			float radiusSqr,
			ref PlayerStateController closestPlayer,
			ref float closestDistanceSqr)
		{
			if (player == null || !player.IsAlive)
			{
				return;
			}

			var offset = player.transform.position - center;
			offset.y = 0f;
			var distanceSqr = offset.sqrMagnitude;

			if (distanceSqr > radiusSqr || distanceSqr >= closestDistanceSqr)
			{
				return;
			}

			closestDistanceSqr = distanceSqr;
			closestPlayer = player;
		}

	}
}
