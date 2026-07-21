using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Authority;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Match
{
	// 로컬 플레이어가 가까운 생존자에게 타겟을 넘기는 태그 프로토타입.
	public sealed class LocalTargetTossPrototype : ITickable
	{
		private readonly BombTargetService bombTargetService;
		private readonly IMatchAuthorityService matchAuthorityService;
		private readonly GameBalanceSettings balanceSettings;
		private readonly LocalMatchFeedbackView matchFeedbackView;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private PlayerStateController[] players;
		private bool isInitialized;
		private bool wasTouching;

		public LocalTargetTossPrototype(
			BombTargetService bombTargetService,
			IMatchAuthorityService matchAuthorityService,
			GameBalanceSettings balanceSettings,
			LocalMatchFeedbackView matchFeedbackView,
			LocalPlayerCameraFollow cameraFollow)
		{
			this.bombTargetService = bombTargetService;
			this.matchAuthorityService = matchAuthorityService;
			this.balanceSettings = balanceSettings;
			this.matchFeedbackView = matchFeedbackView;
			this.cameraFollow = cameraFollow;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 0;
			wasTouching = false;
		}

		public void Tick()
		{
			if (!isInitialized)
			{
				return;
			}

			var currentTarget = bombTargetService.TargetPlayer;

			if (currentTarget == null || !currentTarget.IsAlive)
			{
				wasTouching = false;
				return;
			}

			for (var i = 0; i < players.Length; i++)
			{
				var candidate = players[i];

				if (candidate == null || candidate == currentTarget || !candidate.IsAlive)
				{
					continue;
				}

				if (!IsTouching(currentTarget, candidate))
				{
					continue;
				}

				if (wasTouching)
				{
					return;
				}

				TryTransferTarget(currentTarget, candidate);
				wasTouching = true;
				return;
			}

			wasTouching = false;
		}

		private bool IsTouching(PlayerStateController fromPlayer, PlayerStateController toPlayer)
		{
			var offset = fromPlayer.transform.position - toPlayer.transform.position;
			offset.y = 0f;
			return offset.sqrMagnitude <= balanceSettings.TagDistanceSqr;
		}

		private bool TryTransferTarget(PlayerStateController fromPlayer, PlayerStateController toPlayer)
		{
			if (fromPlayer == null || toPlayer == null)
			{
				return false;
			}

			if (toPlayer.IsTagImmune)
			{
				if (matchFeedbackView != null)
				{
					matchFeedbackView.ShowTagImmuneRejected(
						toPlayer.transform,
						cameraFollow != null ? cameraFollow.transform : null);
				}

				return false;
			}

			return matchAuthorityService.TryTransferTarget(fromPlayer, toPlayer);
		}
	}
}
