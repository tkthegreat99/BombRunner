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
			ShowTemporaryExplosionRadius();
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
			return offset / distance * (bombState.MoveSpeed * speedScale);
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

		private void ShowTemporaryExplosionRadius()
		{
			// 임시 폭발 범위 확인용 Sphere. 이후 ExplosionRing/VFX 풀 오브젝트로 교체.
			var preview = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			preview.name = "Temporary Explosion Radius Preview";
			preview.transform.position = transform.position;
			preview.transform.localScale = Vector3.one * (bombState.ExplosionRadius * 2f);

			if (preview.TryGetComponent<Collider>(out var previewCollider))
			{
				previewCollider.enabled = false;
			}

			if (preview.TryGetComponent<MeshRenderer>(out var meshRenderer))
			{
				meshRenderer.material = CreateTemporaryRangePreviewMaterial();
			}
		}

		private Material CreateTemporaryRangePreviewMaterial()
		{
			var shader = Shader.Find("Universal Render Pipeline/Unlit");

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			var material = new Material(shader);
			var color = new Color(1f, 0.18f, 0.08f, 0.28f);

			if (material.HasProperty("_BaseColor"))
			{
				material.SetColor("_BaseColor", color);
			}

			if (material.HasProperty("_Color"))
			{
				material.SetColor("_Color", color);
			}

			SetMaterialFloatIfPresent(material, "_Surface", 1f);
			SetMaterialFloatIfPresent(material, "_Blend", 0f);
			SetMaterialFloatIfPresent(material, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
			SetMaterialFloatIfPresent(material, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			SetMaterialFloatIfPresent(material, "_ZWrite", 0f);
			material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			material.renderQueue = 3000;
			return material;
		}

		private void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetFloat(propertyName, value);
			}
		}
	}
}
