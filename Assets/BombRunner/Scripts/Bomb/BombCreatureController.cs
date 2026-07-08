using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;

namespace BombRunner.Scripts.Bomb
{
	public sealed class BombCreatureController : MonoBehaviour
	{
		private const float RoundDuration = 15f;
		private const float WarningThreshold = 10f;
		private const float OverdriveThreshold = 5f;
		private const float Acceleration = 15f;
		private const float Deceleration = 10f;
		private const float RotationSpeed = 720f;
		private const float ArriveDistance = 0.35f;
		private const float SlowRadius = 1.4f;
		private const float WarningPulseSpeed = 7f;
		private const float WarningPulseScale = 0.16f;

		private BombState bombState;
		private BombTargetService bombTargetService;
		private PlayerStateController localPlayer;
		private PlayerStateController dummyPlayer;
		private Vector3 velocity;
		private Vector3 baseScale;
		private float remainingTime;
		private bool isInitialized;
		private bool hasExploded;

		// 임시 폭탄 크리처입니다. 이후 네트워크 스폰 프리팹과 풀링 대상으로 교체합니다.
		public void Initialize(
			BombState bombState,
			BombTargetService bombTargetService,
			PlayerStateController localPlayer,
			PlayerStateController dummyPlayer)
		{
			this.bombState = bombState;
			this.bombTargetService = bombTargetService;
			this.localPlayer = localPlayer;
			this.dummyPlayer = dummyPlayer;
			velocity = Vector3.zero;
			baseScale = transform.localScale;
			remainingTime = RoundDuration;
			hasExploded = false;
			isInitialized = bombState != null
				&& bombTargetService != null
				&& localPlayer != null
				&& dummyPlayer != null;

			if (!isInitialized)
			{
				return;
			}

			this.bombState.SetTimerPhase(BombTimerPhase.Calm);
			Debug.Log("Bomb timer phase: Calm");
		}

		private void Update()
		{
			if (!isInitialized || hasExploded)
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

		private void UpdateTimer()
		{
			remainingTime -= Time.deltaTime;

			var nextPhase = GetTimerPhase(remainingTime);

			if (nextPhase != bombState.TimerPhase)
			{
				bombState.SetTimerPhase(nextPhase);
				Debug.Log($"Bomb timer phase: {nextPhase}");
			}

			if (remainingTime > 0f)
			{
				return;
			}

			hasExploded = true;
			transform.localScale = baseScale;
			LogPlayersInExplosionRadius();
			ShowTemporaryExplosionRadius();
		}

		private BombTimerPhase GetTimerPhase(float time)
		{
			if (time > WarningThreshold)
			{
				return BombTimerPhase.Calm;
			}

			if (time > OverdriveThreshold)
			{
				return BombTimerPhase.Warning;
			}

			return BombTimerPhase.Overdrive;
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
			// 임시 Warning 연출입니다. 이후 Bomb Animator 또는 전용 VFX/스케일 Tween으로 교체합니다.
			if (bombState.TimerPhase != BombTimerPhase.Warning)
			{
				transform.localScale = baseScale;
				return;
			}

			var pulse = 1f + Mathf.Sin(Time.time * WarningPulseSpeed) * WarningPulseScale;
			transform.localScale = baseScale * pulse;
		}

		private void LogPlayersInExplosionRadius()
		{
			var targetPlayer = bombTargetService.TargetPlayer;

			var center = transform.position;
			var radiusSqr = bombState.ExplosionRadius * bombState.ExplosionRadius;
			var hitLocal = IsPlayerInRadius(localPlayer, center, radiusSqr);
			var hitDummy = IsPlayerInRadius(dummyPlayer, center, radiusSqr);

			var targetLabel = targetPlayer != null ? targetPlayer.PlayerLabel : "None";
			Debug.Log($"Bomb exploded at bomb position. Target: {targetLabel}, In radius - Local Player: {hitLocal}, Dummy Player: {hitDummy}");
		}

		private void ShowTemporaryExplosionRadius()
		{
			// 임시 폭발 범위 확인용 Sphere입니다. 이후 ExplosionRing/VFX 풀 오브젝트로 교체합니다.
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

		private bool IsPlayerInRadius(PlayerStateController player, Vector3 center, float radiusSqr)
		{
			if (player == null || !player.IsAlive)
			{
				return false;
			}

			var offset = player.transform.position - center;
			offset.y = 0f;
			return offset.sqrMagnitude <= radiusSqr;
		}
	}
}
