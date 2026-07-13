using BombRunner.Scripts.Bomb;
using UnityEngine;

namespace BombRunner.Scripts.Data
{
	[CreateAssetMenu(fileName = "GameBalanceSettings", menuName = "Boom Runner/Game Balance Settings")]
	public sealed class GameBalanceSettings : ScriptableObject
	{
		[SerializeField] private float tagDistance = 1.5f;
		[SerializeField] private float tagImmuneDurationSeconds = 3f;
		[SerializeField] private Vector2 calmDurationRange = new(4f, 6f);
		[SerializeField] private Vector2 warningDurationRange = new(3f, 5f);
		[SerializeField] private Vector2 overdriveDurationRange = new(2f, 4f);
		[SerializeField] private float calmMoveSpeed = 5.2f;
		[SerializeField] private float warningMoveSpeed = 6.8f;
		[SerializeField] private float overdriveMoveSpeed = 8.4f;
		[SerializeField] private float explosionRadius = 2.5f;
		[SerializeField] private float downedMoveSpeedMultiplier = 0.35f;
		[SerializeField] private float aliveSeparationRadius = 0.55f;
		[SerializeField] private float downedSeparationRadius = 0.35f;
		[SerializeField] private float separationStrength = 14f;
		[SerializeField] private float downedSeparationPushWeight = 0.45f;
		[SerializeField] private float downedStompRadius = 0.85f;
		[SerializeField] private float downedStompSlowDurationSeconds = 0.45f;
		[SerializeField] private float downedStompSpeedMultiplier = 0.25f;
		[SerializeField] private float downedStompCooldownSeconds = 1.2f;
		[SerializeField] private float bombDownedBoostRadius = 0.8f;
		[SerializeField] private float bombDownedBoostSpeedMultiplier = 1.45f;
		[SerializeField] private float bombDownedBoostDurationSeconds = 0.75f;
		[SerializeField] private float bombDownedBoostCooldownSeconds = 1f;
		[SerializeField] private float spawnCueDurationSeconds = 1.6f;
		[SerializeField] private float spawnCueRadius = 1.2f;
		[SerializeField] private float spawnCueHeight = 0.03f;
		[SerializeField] private float bombSpawnCameraFocusSeconds = 0.75f;
		[SerializeField] private float bombDropHeight = 6f;
		[SerializeField] private float bombDropDurationSeconds = 0.65f;
		[SerializeField] private float bombStartCountdownSeconds = 3f;
		[SerializeField] private float tauntRadius = 2.2f;
		[SerializeField] private float tauntBombRiskHoldSeconds = 1.25f;
		[SerializeField] private float tauntBombRiskDistance = 5f;

		public float TagDistance => Mathf.Max(0f, tagDistance);
		public float TagDistanceSqr => TagDistance * TagDistance;
		public float TagImmuneDurationSeconds => Mathf.Max(0f, tagImmuneDurationSeconds);
		public Vector2 CalmDurationRange => GetSafeRange(calmDurationRange);
		public Vector2 WarningDurationRange => GetSafeRange(warningDurationRange);
		public Vector2 OverdriveDurationRange => GetSafeRange(overdriveDurationRange);
		public float CalmMoveSpeed => Mathf.Max(0f, calmMoveSpeed);
		public float WarningMoveSpeed => Mathf.Max(0f, warningMoveSpeed);
		public float OverdriveMoveSpeed => Mathf.Max(0f, overdriveMoveSpeed);
		public float ExplosionRadius => Mathf.Max(0f, explosionRadius);
		public float DownedMoveSpeedMultiplier => Mathf.Max(0f, downedMoveSpeedMultiplier);
		public float AliveSeparationRadius => Mathf.Max(0f, aliveSeparationRadius);
		public float DownedSeparationRadius => Mathf.Max(0f, downedSeparationRadius);
		public float SeparationStrength => Mathf.Max(0f, separationStrength);
		public float DownedSeparationPushWeight => Mathf.Max(0f, downedSeparationPushWeight);
		public float DownedStompRadius => Mathf.Max(0f, downedStompRadius);
		public float DownedStompRadiusSqr => DownedStompRadius * DownedStompRadius;
		public float DownedStompSlowDurationSeconds => Mathf.Max(0f, downedStompSlowDurationSeconds);
		public float DownedStompSpeedMultiplier => Mathf.Clamp01(downedStompSpeedMultiplier);
		public float DownedStompCooldownSeconds => Mathf.Max(0f, downedStompCooldownSeconds);
		public float BombDownedBoostRadius => Mathf.Max(0f, bombDownedBoostRadius);
		public float BombDownedBoostRadiusSqr => BombDownedBoostRadius * BombDownedBoostRadius;
		public float BombDownedBoostSpeedMultiplier => Mathf.Max(1f, bombDownedBoostSpeedMultiplier);
		public float BombDownedBoostDurationSeconds => Mathf.Max(0f, bombDownedBoostDurationSeconds);
		public float BombDownedBoostCooldownSeconds => Mathf.Max(0f, bombDownedBoostCooldownSeconds);
		public float SpawnCueDurationSeconds => Mathf.Max(0f, spawnCueDurationSeconds);
		public float SpawnCueRadius => Mathf.Max(0f, spawnCueRadius);
		public float SpawnCueHeight => Mathf.Max(0f, spawnCueHeight);
		public float BombSpawnCameraFocusSeconds => Mathf.Max(0f, bombSpawnCameraFocusSeconds);
		public float BombDropHeight => Mathf.Max(0f, bombDropHeight);
		public float BombDropDurationSeconds => Mathf.Max(0f, bombDropDurationSeconds);
		public float BombStartCountdownSeconds => Mathf.Max(0f, bombStartCountdownSeconds);
		public float TauntRadius => Mathf.Max(0f, tauntRadius);
		public float TauntRadiusSqr => TauntRadius * TauntRadius;
		public float TauntBombRiskHoldSeconds => Mathf.Max(0f, tauntBombRiskHoldSeconds);
		public float TauntBombRiskDistance => Mathf.Max(0f, tauntBombRiskDistance);
		public float TauntBombRiskDistanceSqr => TauntBombRiskDistance * TauntBombRiskDistance;

		public Vector2 GetDurationRange(BombTimerPhase timerPhase)
		{
			switch (timerPhase)
			{
				case BombTimerPhase.Calm:
					return CalmDurationRange;
				case BombTimerPhase.Warning:
					return WarningDurationRange;
				case BombTimerPhase.Overdrive:
					return OverdriveDurationRange;
				default:
					return Vector2.zero;
			}
		}

		public float GetMoveSpeed(BombTimerPhase timerPhase)
		{
			switch (timerPhase)
			{
				case BombTimerPhase.Calm:
					return CalmMoveSpeed;
				case BombTimerPhase.Warning:
					return WarningMoveSpeed;
				case BombTimerPhase.Overdrive:
					return OverdriveMoveSpeed;
				default:
					return CalmMoveSpeed;
			}
		}

		private Vector2 GetSafeRange(Vector2 range)
		{
			var min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
			var max = Mathf.Max(0f, Mathf.Max(range.x, range.y));
			return new Vector2(min, max);
		}
	}
}
