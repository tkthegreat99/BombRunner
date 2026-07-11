using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace BombRunner.Scripts.Gameplay.Player
{
	public sealed class PlayerStateController : MonoBehaviour
	{
		[SerializeField] private string playerLabel = "Player";
		[SerializeField] private bool isAlive = true;
		[SerializeField] private bool isMoving;
		[SerializeField] private bool isDashing;
		[FormerlySerializedAs("isInvulnerable")]
		[SerializeField] private bool isTagImmune;
		[SerializeField] private bool isTarget;

		public event Action Changed;

		public string PlayerLabel => playerLabel;
		public bool IsAlive => isAlive;
		public bool IsMoving => isMoving;
		public bool IsDashing => isDashing;
		public bool IsTagImmune => isTagImmune;
		public bool IsTarget => isTarget;

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
			SetState(ref isAlive, value);
		}

		public void SetMoving(bool value)
		{
			SetState(ref isMoving, value);
		}

		public void SetDashing(bool value)
		{
			SetState(ref isDashing, value);
		}

		public void SetTagImmune(bool value)
		{
			SetState(ref isTagImmune, value);
		}

		public void SetTarget(bool value)
		{
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
