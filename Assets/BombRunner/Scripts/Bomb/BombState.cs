using BombRunner.Scripts.Gameplay.Player;

namespace BombRunner.Scripts.Bomb
{
	public sealed class BombState
	{
		// 이후 Host/Master 권한의 Networked State 또는 RPC 동기화 대상으로 이동할 폭탄 핵심 상태입니다.
		public BombTimerPhase TimerPhase { get; private set; } = BombTimerPhase.Calm;
		public PlayerStateController TargetPlayer { get; private set; }
		public int RageStack { get; private set; }
		public float MoveSpeed { get; private set; } = 6.8f;
		public float ExplosionRadius { get; private set; } = 2.5f;

		public void SetTimerPhase(BombTimerPhase timerPhase)
		{
			TimerPhase = timerPhase;
		}

		public void SetTargetPlayer(PlayerStateController targetPlayer)
		{
			TargetPlayer = targetPlayer;
		}

		public void SetRageStack(int rageStack)
		{
			RageStack = rageStack < 0 ? 0 : rageStack;
		}

		public void SetMoveSpeed(float moveSpeed)
		{
			MoveSpeed = moveSpeed < 0f ? 0f : moveSpeed;
		}

		public void SetExplosionRadius(float explosionRadius)
		{
			ExplosionRadius = explosionRadius < 0f ? 0f : explosionRadius;
		}
	}
}
