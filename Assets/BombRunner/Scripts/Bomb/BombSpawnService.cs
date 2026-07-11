using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.Bomb
{
	public sealed class BombSpawnService
	{
		private readonly IObjectResolver objectResolver;
		private readonly BombSpawnSettings bombSpawnSettings;
		private readonly GameBalanceSettings balanceSettings;
		private readonly BombState bombState;
		private readonly BombTargetService bombTargetService;
		private GameObject bombInstance;

		public BombSpawnService(
			IObjectResolver objectResolver,
			BombSpawnSettings bombSpawnSettings,
			GameBalanceSettings balanceSettings,
			BombState bombState,
			BombTargetService bombTargetService)
		{
			this.objectResolver = objectResolver;
			this.bombSpawnSettings = bombSpawnSettings;
			this.balanceSettings = balanceSettings;
			this.bombState = bombState;
			this.bombTargetService = bombTargetService;
		}

		public BombCreatureController SpawnLocalBomb(PlayerStateController[] players)
		{
			return SpawnLocalBomb(players, GetLocalBombSpawnPosition(players));
		}

		public BombCreatureController SpawnLocalBomb(PlayerStateController[] players, Vector3 spawnPosition)
		{
			if (bombInstance != null)
			{
				return bombInstance.TryGetComponent<BombCreatureController>(out var currentController) ? currentController : null;
			}

			if (players == null || players.Length == 0)
			{
				return null;
			}

			if (bombSpawnSettings.BombPrefab == null)
			{
				Debug.LogError("BombSpawnSettings에 BombPrefab 연결이 필요합니다.");
				return null;
			}

			bombInstance = objectResolver.Instantiate(bombSpawnSettings.BombPrefab, spawnPosition, Quaternion.identity);
			bombInstance.name = "Local Prototype Bomb";

			if (!bombInstance.TryGetComponent<BombCreatureController>(out var controller))
			{
				Debug.LogError("BombPrefab에 BombCreatureController가 필요합니다.");
				return null;
			}

			controller.Initialize(bombState, bombTargetService, balanceSettings, players);
			return controller;
		}

		public Vector3 GetLocalBombSpawnPosition(PlayerStateController[] players)
		{
			return GetSpawnPosition(players);
		}

		public void ShowLocalSpawnCue(PlayerStateController[] players)
		{
			if (players == null || players.Length == 0)
			{
				return;
			}

			var spawnPosition = GetSpawnPosition(players);
			spawnPosition.y = balanceSettings.SpawnCueHeight;
			var cue = new GameObject("Temporary Bomb Spawn Cue");
			cue.name = "Temporary Bomb Spawn Cue";
			cue.transform.position = spawnPosition;
			ConfigureSpawnCueRing(cue);

			Object.Destroy(cue, balanceSettings.SpawnCueDurationSeconds);
			Debug.Log("Local match flow: next bomb spawn cue shown");
		}

		public void DespawnLocalBomb(BombCreatureController controller)
		{
			if (controller == null || bombInstance == null)
			{
				return;
			}

			if (controller.gameObject != bombInstance)
			{
				return;
			}

			// 임시 로컬 제거. 이후 Bomb 풀 반환 또는 네트워크 디스폰으로 교체.
			Object.Destroy(bombInstance);
			bombInstance = null;
		}

		private Vector3 GetSpawnPosition(PlayerStateController[] players)
		{
			var aliveCount = 0;
			var spawnPosition = Vector3.zero;
			var fallbackPosition = Vector3.zero;
			var fallbackCount = 0;

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null)
				{
					continue;
				}

				fallbackPosition += player.transform.position;
				fallbackCount++;

				if (!player.IsAlive)
				{
					continue;
				}

				spawnPosition += player.transform.position;
				aliveCount++;
			}

			if (aliveCount > 0)
			{
				spawnPosition /= aliveCount;
			}
			else if (fallbackCount > 0)
			{
				spawnPosition = fallbackPosition / fallbackCount;
			}

			spawnPosition.y = 0.6f;
			return spawnPosition;
		}

		private Material CreateTemporarySpawnCueMaterial()
		{
			var shader = Shader.Find("Universal Render Pipeline/Unlit");

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			var material = new Material(shader);
			var color = new Color(1f, 0.78f, 0.12f, 0.42f);

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

		private void ConfigureSpawnCueRing(GameObject cue)
		{
			var lineRenderer = cue.AddComponent<LineRenderer>();
			lineRenderer.loop = true;
			lineRenderer.useWorldSpace = false;
			lineRenderer.positionCount = 48;
			lineRenderer.startWidth = 0.08f;
			lineRenderer.endWidth = 0.08f;
			lineRenderer.material = CreateTemporarySpawnCueMaterial();
			lineRenderer.startColor = new Color(1f, 0.78f, 0.12f, 0.8f);
			lineRenderer.endColor = new Color(1f, 0.28f, 0.08f, 0.8f);

			for (var i = 0; i < lineRenderer.positionCount; i++)
			{
				var angle = Mathf.PI * 2f * i / lineRenderer.positionCount;
				var position = new Vector3(
					Mathf.Cos(angle) * balanceSettings.SpawnCueRadius,
					0f,
					Mathf.Sin(angle) * balanceSettings.SpawnCueRadius);
				lineRenderer.SetPosition(i, position);
			}
		}

		private void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetFloat(propertyName, value);
			}
		}
	}
}
