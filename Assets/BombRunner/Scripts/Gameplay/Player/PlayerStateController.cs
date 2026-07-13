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
		private float tagImmuneDurationSeconds;
		private float tagImmuneEndTime;

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
		public bool CanDash => IsAlive && !isTaunting && !isDashLocked;
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
			}

			NotifyChanged();
		}

		public void SetMoving(bool value)
		{
			SetState(ref isMoving, value);
		}

		public void SetDashing(bool value)
		{
			if (IsDowned && value)
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
			if (IsDowned && value)
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
