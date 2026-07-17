using BombRunner.Scripts.Gameplay.Items;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Bomb;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Authority
{
	public interface IMatchAuthorityService
	{
		bool TryTransferTarget(PlayerStateController fromPlayer, PlayerStateController toPlayer);
		bool TrySetBombTarget(PlayerStateController player);
		bool TrySetAnyAliveBombTarget(PlayerStateController[] players, PlayerStateController excludedPlayer);
		void ClearBombTarget();
		float ResolveBombPhaseDuration(BombTimerPhase timerPhase);
		PlayerStateController ResolveExplosionVictim(Vector3 bombPosition, PlayerStateController[] players, float radius);
		bool SetPlayerDowned(PlayerStateController player);
		bool TryPickupItem(PlayerStateController player, PlayerItemHolder holder, ItemType itemType);
		bool TryThrowItem(PlayerStateController owner, ItemType itemType, Vector3 direction);
		bool ApplyItemHit(ItemType itemType, PlayerStateController target);
		bool ApplyTauntRisk(PlayerStateController taunter);
	}
}
