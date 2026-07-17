using System;
using System.Collections.Generic;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Input;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Gameplay.Items
{
	// 로컬 아이템 프로토타입 전체 흐름.
	public sealed class LocalItemService : ITickable, IDisposable
	{
		// 피격 후 아이템 처리 방식.
		private enum ItemHitBehavior
		{
			Consume,
			Drop
		}

		// 맵에 놓인 획득 가능 아이템 상태.
		private sealed class PickupState
		{
			public GameObject GameObject;
			public ItemType ItemType;
			public bool IsActive;
		}

		// 던져진 뒤 비행 중인 아이템 상태.
		private sealed class ProjectileState
		{
			public GameObject GameObject;
			public ItemType ItemType;
			public PlayerStateController Owner;
			public Vector3 HorizontalVelocity;
			public float VerticalVelocity;
			public float TraveledDistance;
			public float RemainingDistance;
			public float SpawnTime;
		}

		private readonly IInputService inputService;
		private readonly GameBalanceSettings balanceSettings;
		private readonly List<PickupState> pickups = new();
		private readonly List<ProjectileState> projectiles = new();
		private PlayerStateController[] players;
		private PlayerMovementController[] movementControllers;
		private PlayerItemHolder[] itemHolders;
		private bool isInitialized;

		public LocalItemService(IInputService inputService, GameBalanceSettings balanceSettings)
		{
			this.inputService = inputService;
			this.balanceSettings = balanceSettings;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 0;

			ClearRuntimeObjects();

			if (!isInitialized)
			{
				movementControllers = null;
				itemHolders = null;
				return;
			}

			movementControllers = new PlayerMovementController[players.Length];
			itemHolders = new PlayerItemHolder[players.Length];

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null)
				{
					continue;
				}

				player.TryGetComponent(out movementControllers[i]);
				itemHolders[i] = EnsureItemHolder(player);
			}

			SpawnInitialPickups();
		}

		public void Tick()
		{
			if (!isInitialized)
			{
				return;
			}

			ClearDownedPlayerItems();
			UpdatePickupChecks();
			UpdateLocalUseInput();
			UpdateProjectiles();
		}

		public void Dispose()
		{
			ClearRuntimeObjects();
		}

		private PlayerItemHolder EnsureItemHolder(PlayerStateController player)
		{
			if (player.TryGetComponent<PlayerItemHolder>(out var holder))
			{
				return holder;
			}

			return player.gameObject.AddComponent<PlayerItemHolder>();
		}

		private void SpawnInitialPickups()
		{
			// 로컬 기능 검증용 임시 아이템 배치.
			SpawnPickup(new Vector3(0f, balanceSettings.ItemPickupHeight, 3f), ItemType.Slow);
			SpawnPickup(new Vector3(3f, balanceSettings.ItemPickupHeight, 2f), ItemType.Stun);
			SpawnPickup(new Vector3(-3f, balanceSettings.ItemPickupHeight, 2f), ItemType.Slow);
		}

		private void SpawnPickup(Vector3 position, ItemType itemType)
		{
			// 임시 프리미티브 픽업 생성.
			var pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
			pickupObject.name = $"Local Prototype Item Pickup - {itemType}";
			pickupObject.transform.position = position;
			pickupObject.transform.localScale = Vector3.one * 0.45f;
			DisableCollider(pickupObject);

			pickups.Add(new PickupState
			{
				GameObject = pickupObject,
				ItemType = itemType,
				IsActive = true
			});
		}

		private void ClearDownedPlayerItems()
		{
			// 다운 상태 진입 시 보유 아이템 제거.
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];
				var holder = itemHolders[i];

				if (player != null && holder != null && player.IsDowned && holder.HasItem)
				{
					holder.Clear();
				}
			}
		}

		private void UpdatePickupChecks()
		{
			// Host/Master 확정 대상인 아이템 획득 판정.
			for (var pickupIndex = 0; pickupIndex < pickups.Count; pickupIndex++)
			{
				var pickup = pickups[pickupIndex];

				if (pickup == null || !pickup.IsActive || pickup.GameObject == null)
				{
					continue;
				}

				for (var playerIndex = 0; playerIndex < players.Length; playerIndex++)
				{
					var player = players[playerIndex];
					var holder = itemHolders[playerIndex];

					if (player == null || holder == null || !player.IsAlive || holder.HasItem)
					{
						continue;
					}

					var offset = player.transform.position - pickup.GameObject.transform.position;
					offset.y = 0f;

					if (offset.sqrMagnitude > balanceSettings.ItemPickupRadiusSqr)
					{
						continue;
					}

					if (!holder.TryPickup(pickup.ItemType))
					{
						continue;
					}

					pickup.IsActive = false;
					pickup.GameObject.SetActive(false);
					Debug.Log($"Item pickup: {player.PlayerLabel} picked {pickup.ItemType}");
					break;
				}
			}
		}

		private void UpdateLocalUseInput()
		{
			// 로컬 플레이어 임시 아이템 사용 입력 처리.
			if (!inputService.UseItemPressed || players.Length <= 0)
			{
				return;
			}

			var localPlayer = players[0];
			var localHolder = itemHolders[0];
			var localMovement = movementControllers[0];

			if (localPlayer == null || localHolder == null || localMovement == null || !localPlayer.IsAlive)
			{
				return;
			}

			if (!localHolder.TryConsume(out var itemType))
			{
				return;
			}

			var direction = localMovement.LastMoveDirection;
			direction.y = 0f;

			if (direction.sqrMagnitude <= 0.0001f)
			{
				direction = localPlayer.transform.forward;
				direction.y = 0f;
			}

			if (direction.sqrMagnitude <= 0.0001f)
			{
				direction = Vector3.forward;
			}

			SpawnProjectile(localPlayer, itemType, direction.normalized);
		}

		private void SpawnProjectile(PlayerStateController owner, ItemType itemType, Vector3 direction)
		{
			// 임시 프리미티브 투척 아이템 생성.
			var projectileObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			projectileObject.name = $"Local Prototype Item Projectile - {itemType}";
			projectileObject.transform.position = owner.transform.position + direction * 0.85f + Vector3.up * balanceSettings.ItemPickupHeight;
			projectileObject.transform.localScale = Vector3.one * 0.35f;
			DisableCollider(projectileObject);

			var range = GetProjectileRange(itemType);
			var horizontalSpeed = balanceSettings.ItemProjectileSpeed;
			// 사거리와 수평 속도 기반 비행 시간 계산.
			var flightSeconds = horizontalSpeed > 0f ? range / horizontalSpeed : 0f;
			var startHeight = projectileObject.transform.position.y;
			var endHeight = balanceSettings.ItemPickupHeight;

			projectiles.Add(new ProjectileState
			{
				GameObject = projectileObject,
				ItemType = itemType,
				Owner = owner,
				HorizontalVelocity = direction * horizontalSpeed,
				VerticalVelocity = GetThrowVerticalVelocity(startHeight, endHeight, flightSeconds),
				TraveledDistance = 0f,
				RemainingDistance = range,
				SpawnTime = Time.time
			});

			Debug.Log($"Item throw: {owner.PlayerLabel} threw {itemType}");
		}

		private void UpdateProjectiles()
		{
			// 포물선 이동, 피격, 착지 처리.
			for (var i = projectiles.Count - 1; i >= 0; i--)
			{
				var projectile = projectiles[i];

				if (projectile == null || projectile.GameObject == null)
				{
					projectiles.RemoveAt(i);
					continue;
				}

				var moveDistance = projectile.HorizontalVelocity.magnitude * Time.deltaTime;
				var position = projectile.GameObject.transform.position;
				position += projectile.HorizontalVelocity * Time.deltaTime;
				projectile.VerticalVelocity -= balanceSettings.ItemProjectileGravity * Time.deltaTime;
				position.y += projectile.VerticalVelocity * Time.deltaTime;
				projectile.GameObject.transform.position = position;
				projectile.TraveledDistance += moveDistance;
				projectile.RemainingDistance -= moveDistance;

				var hitBehavior = TryResolveProjectileHit(projectile);

				if (hitBehavior == ItemHitBehavior.Consume)
				{
					DestroyProjectileAt(i);
					continue;
				}

				if (hitBehavior == ItemHitBehavior.Drop)
				{
					LandProjectileAt(i);
					continue;
				}

				if (projectile.RemainingDistance <= 0f
					|| (projectile.TraveledDistance > 0.25f && position.y <= balanceSettings.ItemPickupHeight))
				{
					LandProjectileAt(i);
				}
			}
		}

		private float GetThrowVerticalVelocity(float startHeight, float endHeight, float flightSeconds)
		{
			// 지정 사거리 끝에서 착지 높이에 도달하기 위한 초기 수직 속도 계산.
			if (flightSeconds <= 0f)
			{
				return 0f;
			}

			return (endHeight - startHeight + 0.5f * balanceSettings.ItemProjectileGravity * flightSeconds * flightSeconds)
				/ flightSeconds;
		}

		private ItemHitBehavior? TryResolveProjectileHit(ProjectileState projectile)
		{
			// Host/Master 확정 대상인 아이템 피격 판정.
			var canHitOwner = Time.time - projectile.SpawnTime >= balanceSettings.ItemProjectileOwnerIgnoreSeconds;

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null || !player.IsAlive || (player == projectile.Owner && !canHitOwner))
				{
					continue;
				}

				var offset = player.transform.position - projectile.GameObject.transform.position;
				offset.y = 0f;

				if (offset.sqrMagnitude > balanceSettings.ItemProjectileHitRadiusSqr)
				{
					continue;
				}

				ApplyItemEffect(projectile.ItemType, player, movementControllers[i]);
				Debug.Log($"Item hit: {projectile.ItemType} hit {player.PlayerLabel}");
				return GetHitBehavior(projectile.ItemType);
			}

			return null;
		}

		private void ApplyItemEffect(
			ItemType itemType,
			PlayerStateController target,
			PlayerMovementController movementController)
		{
			// 아이템 타입별 피격 효과 적용.
			if (target == null || !target.IsAlive)
			{
				return;
			}

			switch (itemType)
			{
				case ItemType.Slow:
					if (movementController != null)
					{
						movementController.ApplyTemporarySlow(
							balanceSettings.SlowItemDurationSeconds,
							balanceSettings.SlowItemSpeedMultiplier);
					}
					break;
				case ItemType.Stun:
					target.SetStunned(true, balanceSettings.StunItemDurationSeconds);
					break;
			}
		}

		private float GetProjectileRange(ItemType itemType)
		{
			// 아이템 타입별 투척 사거리 조회.
			switch (itemType)
			{
				case ItemType.Slow:
				case ItemType.Stun:
					return balanceSettings.ItemProjectileRange;
				default:
					return 0f;
			}
		}

		private ItemHitBehavior GetHitBehavior(ItemType itemType)
		{
			// 아이템 타입별 피격 후 처리 방식 조회.
			switch (itemType)
			{
				case ItemType.Slow:
					return ItemHitBehavior.Consume;
				case ItemType.Stun:
					return ItemHitBehavior.Drop;
				default:
					return ItemHitBehavior.Consume;
			}
		}

		private void DestroyProjectileAt(int index)
		{
			// 소모형 아이템 제거.
			var projectile = projectiles[index];

			if (projectile != null && projectile.GameObject != null)
			{
				UnityEngine.Object.Destroy(projectile.GameObject);
			}

			projectiles.RemoveAt(index);
		}

		private void LandProjectileAt(int index)
		{
			// 비소모형 또는 미피격 아이템 착지 처리.
			var projectile = projectiles[index];

			if (projectile == null || projectile.GameObject == null)
			{
				projectiles.RemoveAt(index);
				return;
			}

			var landingPosition = projectile.GameObject.transform.position;
			landingPosition.y = balanceSettings.ItemPickupHeight;
			var itemType = projectile.ItemType;

			UnityEngine.Object.Destroy(projectile.GameObject);
			projectiles.RemoveAt(index);
			SpawnPickup(landingPosition, itemType);
			Debug.Log($"Item landed: {itemType}");
		}

		private void DisableCollider(GameObject target)
		{
			// 서비스 거리 판정만 사용하기 위한 임시 프리미티브 Collider 비활성화.
			if (target != null && target.TryGetComponent<Collider>(out var targetCollider))
			{
				targetCollider.enabled = false;
			}
		}

		private void ClearRuntimeObjects()
		{
			// 로컬 프로토타입 런타임 오브젝트 정리.
			for (var i = 0; i < pickups.Count; i++)
			{
				if (pickups[i] != null && pickups[i].GameObject != null)
				{
					UnityEngine.Object.Destroy(pickups[i].GameObject);
				}
			}

			for (var i = 0; i < projectiles.Count; i++)
			{
				if (projectiles[i] != null && projectiles[i].GameObject != null)
				{
					UnityEngine.Object.Destroy(projectiles[i].GameObject);
				}
			}

			pickups.Clear();
			projectiles.Clear();
		}
	}
}
