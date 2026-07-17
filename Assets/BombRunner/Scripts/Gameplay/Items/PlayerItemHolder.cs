using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Items
{
	// 플레이어의 단일 아이템 보유 상태.
	[RequireComponent(typeof(PlayerStateController))]
	public sealed class PlayerItemHolder : MonoBehaviour
	{
		[SerializeField] private ItemType heldItem = ItemType.None;

		private PlayerStateController stateController;

		public ItemType HeldItem => heldItem;
		public bool HasItem => heldItem != ItemType.None;
		public bool CanUseItem =>
			stateController == null || (stateController.IsAlive && !stateController.IsTaunting && !stateController.IsStunned);

		// 비어 있을 때만 획득 가능한 보유 규칙.
		public bool TryPickup(ItemType itemType)
		{
			if (itemType == ItemType.None || HasItem || !CanUseItem)
			{
				return false;
			}

			heldItem = itemType;
			return true;
		}

		public bool TryConsume(out ItemType itemType)
		{
			itemType = heldItem;

			if (itemType == ItemType.None || !CanUseItem)
			{
				return false;
			}

			heldItem = ItemType.None;
			return true;
		}

		public void Clear()
		{
			// 다운 또는 매치 정리 시 보유 아이템 제거.
			heldItem = ItemType.None;
		}

		private void Awake()
		{
			TryGetComponent(out stateController);
		}
	}
}
