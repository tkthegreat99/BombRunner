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

		public event Action Changed;

		public string PlayerLabel => playerLabel;
		public PlayerLifeState LifeState => lifeState;
		public bool IsAlive => lifeState == PlayerLifeState.Alive;
		public bool IsDowned => lifeState == PlayerLifeState.Downed;
		public bool IsMoving => isMoving;
		public bool IsDashing => isDashing;
		public bool IsTagImmune => isTagImmune;
		public bool IsTarget => isTarget;
		public bool CanDash => IsAlive;

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
				isTarget = false;
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
			if (IsDowned && value)
			{
				return;
			}

			SetState(ref isTagImmune, value);
		}

		public void SetTarget(bool value)
		{
			if (IsDowned && value)
			{
				return;
			}

			SetState(ref isTarget, value);
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
