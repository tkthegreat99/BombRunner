using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalMatchFeedbackView : MonoBehaviour
	{
		private GameObject matchResultObject;

		public void ShowSpawnCue(Vector3 spawnPosition, float radius, float height, float durationSeconds)
		{
			spawnPosition.y = height;
			var cue = new GameObject("Temporary Bomb Spawn Cue");
			cue.transform.position = spawnPosition;
			cue.transform.SetParent(transform, true);
			ConfigureRing(cue, radius);
			Destroy(cue, durationSeconds);
		}

		public async UniTask ShowBombStartCountdownAsync(
			Vector3 spawnPosition,
			int countdown,
			Transform cameraTransform,
			CancellationToken cancellationToken)
		{
			var countdownTextObject = CreateWorldText(
				"Temporary Bomb Start Countdown",
				spawnPosition + Vector3.up * 2.2f,
				new Color(1f, 0.85f, 0.12f, 1f),
				1.2f);
			var countdownText = countdownTextObject.GetComponent<TextMesh>();

			try
			{
				for (var i = Mathf.Max(0, countdown); i > 0; i--)
				{
					cancellationToken.ThrowIfCancellationRequested();
					countdownText.text = i.ToString();
					FaceCamera(countdownTextObject.transform, cameraTransform);
					await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
				}
			}
			finally
			{
				Destroy(countdownTextObject);
			}
		}

		public void ShowMatchEnded(string winnerLabel, Transform anchor, Transform cameraTransform)
		{
			if (matchResultObject != null)
			{
				Destroy(matchResultObject);
			}

			var position = anchor != null ? anchor.position + Vector3.up * 3f : Vector3.up * 3f;
			matchResultObject = CreateWorldText(
				"Temporary Match Result",
				position,
				new Color(0.4f, 1f, 0.65f, 1f),
				0.8f);

			var textMesh = matchResultObject.GetComponent<TextMesh>();
			textMesh.text = $"Winner\n{winnerLabel}";
			FaceCamera(matchResultObject.transform, cameraTransform);
		}

		private GameObject CreateWorldText(string objectName, Vector3 position, Color color, float characterSize)
		{
			var textObject = new GameObject(objectName);
			textObject.transform.position = position;
			textObject.transform.SetParent(transform, true);

			var textMesh = textObject.AddComponent<TextMesh>();
			textMesh.anchor = TextAnchor.MiddleCenter;
			textMesh.alignment = TextAlignment.Center;
			textMesh.characterSize = characterSize;
			textMesh.fontSize = 96;
			textMesh.color = color;
			return textObject;
		}

		private void ConfigureRing(GameObject cue, float radius)
		{
			var lineRenderer = cue.AddComponent<LineRenderer>();
			lineRenderer.loop = true;
			lineRenderer.useWorldSpace = false;
			lineRenderer.positionCount = 48;
			lineRenderer.startWidth = 0.08f;
			lineRenderer.endWidth = 0.08f;
			lineRenderer.material = CreateTemporaryMaterial(new Color(1f, 0.78f, 0.12f, 0.42f));
			lineRenderer.startColor = new Color(1f, 0.78f, 0.12f, 0.8f);
			lineRenderer.endColor = new Color(1f, 0.28f, 0.08f, 0.8f);

			for (var i = 0; i < lineRenderer.positionCount; i++)
			{
				var angle = Mathf.PI * 2f * i / lineRenderer.positionCount;
				var position = new Vector3(
					Mathf.Cos(angle) * radius,
					0f,
					Mathf.Sin(angle) * radius);
				lineRenderer.SetPosition(i, position);
			}
		}

		private Material CreateTemporaryMaterial(Color color)
		{
			var shader = Shader.Find("Universal Render Pipeline/Unlit");

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			var material = new Material(shader);

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

		private void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetFloat(propertyName, value);
			}
		}

		private void FaceCamera(Transform target, Transform cameraTransform)
		{
			if (cameraTransform == null)
			{
				return;
			}

			var direction = target.position - cameraTransform.position;

			if (direction.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			target.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		}
	}
}
