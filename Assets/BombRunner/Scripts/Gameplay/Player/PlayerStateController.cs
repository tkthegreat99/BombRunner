using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BombRunner.Scripts.Gameplay.Player
{
	public enum PlayerLifeState
	{
		Alive,
		Downed
	}

	public sealed class PlayerStateController : MonoBehaviour
	{
		[SerializeField] private string playerLabel = "Player";
		[SerializeField] private bool isAlive = true;
		[SerializeField] private PlayerLifeState lifeState = PlayerLifeState.Alive;
		[SerializeField] private bool isMoving;
		[SerializeField] private bool isDashing;
		[FormerlySerializedAs("isInvulnerable")]
		[SerializeField] private bool isTagImmune;
		[SerializeField] private bool isTarget;
		[SerializeField] private bool isTaunting;
		[SerializeField] private bool isDashLocked;
		// 아이템 피격으로 인한 일시 행동 불가 상태.
		[SerializeField] private bool isStunned;
		private float tagImmuneDurationSeconds;
		private float tagImmuneEndTime;
		private float stunEndTime;

		public event Action Changed;

		public string PlayerLabel => playerLabel;
		public PlayerLifeState LifeState => lifeState;
		public bool IsAlive => lifeState == PlayerLifeState.Alive;
		public bool IsDowned => lifeState == PlayerLifeState.Downed;
		public bool IsMoving => isMoving;
		public bool IsDashing => isDashing;
		public bool IsTagImmune => isTagImmune;
		public bool IsTarget => isTarget;
		public bool IsTaunting => isTaunting;
		public bool IsDashLocked => isDashLocked;
		public bool IsStunned => isStunned;
		public bool CanDash => IsAlive && !isTaunting && !isDashLocked && !isStunned;
		public float TagImmuneNormalizedRemaining
		{
			get
			{
				if (!isTagImmune)
				{
					return 0f;
				}

				if (tagImmuneDurationSeconds <= 0f)
				{
					return 1f;
				}

				return Mathf.Clamp01((tagImmuneEndTime - Time.time) / tagImmuneDurationSeconds);
			}
		}

		public void SetPlayerLabel(string playerLabel)
		{
			if (string.Equals(this.playerLabel, playerLabel, StringComparison.Ordinal))
			{
				return;
			}

			this.playerLabel = playerLabel;
			NotifyChanged();
		}

		public void SetAlive(bool value)
		{
			SetLifeState(value ? PlayerLifeState.Alive : PlayerLifeState.Downed);
		}

		public void SetDowned()
		{
			SetLifeState(PlayerLifeState.Downed);
		}

		public void SetLifeState(PlayerLifeState value)
		{
			if (lifeState == value)
			{
				isAlive = value == PlayerLifeState.Alive;
				return;
			}

			lifeState = value;
			isAlive = value == PlayerLifeState.Alive;

			if (IsDowned)
			{
				isMoving = false;
				isDashing = false;
				isTagImmune = false;
				tagImmuneDurationSeconds = 0f;
				tagImmuneEndTime = 0f;
				isTarget = false;
				isTaunting = false;
				isDashLocked = false;
				isStunned = false;
				stunEndTime = 0f;
			}

			NotifyChanged();
		}

		public void SetMoving(bool value)
		{
			SetState(ref isMoving, value);
		}

		public void SetDashing(bool value)
		{
			if ((IsDowned || IsStunned) && value)
			{
				return;
			}

			SetState(ref isDashing, value);
		}

		public void SetTagImmune(bool value)
		{
			SetTagImmune(value, 0f);
		}

		public void SetTagImmune(bool value, float durationSeconds)
		{
			if (IsDowned && value)
			{
				return;
			}

			var changed = isTagImmune != value;
			isTagImmune = value;

			if (value)
			{
				changed |= !Mathf.Approximately(tagImmuneDurationSeconds, durationSeconds);
				tagImmuneDurationSeconds = Mathf.Max(0f, durationSeconds);
				tagImmuneEndTime = Time.time + tagImmuneDurationSeconds;
			}
			else
			{
				tagImmuneDurationSeconds = 0f;
				tagImmuneEndTime = 0f;
			}

			if (changed)
			{
				NotifyChanged();
			}
		}

		public void SetTarget(bool value)
		{
			if (IsDowned && value)
			{
				return;
			}

			SetState(ref isTarget, value);
		}

		public void SetTaunting(bool value)
		{
			if ((IsDowned || IsStunned) && value)
			{
				return;
			}

			var changed = isTaunting != value;

			if (value)
			{
				changed |= isMoving || isDashing;
				isMoving = false;
				isDashing = false;
			}

			isTaunting = value;

			if (changed)
			{
				NotifyChanged();
			}
		}

		public void SetDashLocked(bool value)
		{
			if (IsDowned && value)
			{
				return;
			}

			var changed = isDashLocked != value;

			if (value)
			{
				changed |= isDashing;
				isDashing = false;
			}

			isDashLocked = value;

			if (changed)
			{
				NotifyChanged();
			}
		}

		public void SetStunned(bool value, float durationSeconds = 0f)
		{
			// Host/Master 확정 대상인 스턴 상태 갱신.
			if (IsDowned && value)
			{
				return;
			}

			var changed = isStunned != value;
			isStunned = value;

			if (value)
			{
				stunEndTime = Time.time + Mathf.Max(0f, durationSeconds);
				changed |= isMoving || isDashing || isTaunting;
				isMoving = false;
				isDashing = false;
				isTaunting = false;
			}
			else
			{
				stunEndTime = 0f;
			}

			if (changed)
			{
				NotifyChanged();
			}
		}

		private void Update()
		{
			// 스턴 지속 시간 만료 처리.
			if (isStunned && Time.time >= stunEndTime)
			{
				SetStunned(false);
			}
		}

		private void SetState(ref bool state, bool value)
		{
			if (state == value)
			{
				return;
			}

			state = value;
			NotifyChanged();
		}

		private void NotifyChanged()
		{
			Changed?.Invoke();
		}

		private void OnDestroy()
		{
			Changed = null;
		}
	}
}
