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
			ConfigureRing(
				cue,
				radius,
				new Color(1f, 0.78f, 0.12f, 0.8f),
				new Color(1f, 0.28f, 0.08f, 0.8f),
				0.08f);
			Destroy(cue, durationSeconds);
		}

		public void ShowExplosionDecision(
			Vector3 center,
			float maxRadius,
			float height,
			float expandDurationSeconds,
			float holdSeconds,
			Transform selectedTarget)
		{
			ShowExplosionDecisionAsync(
				center,
				maxRadius,
				height,
				expandDurationSeconds,
				holdSeconds,
				selectedTarget,
				this.GetCancellationTokenOnDestroy()).Forget();
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

		private async UniTaskVoid ShowExplosionDecisionAsync(
			Vector3 center,
			float maxRadius,
			float height,
			float expandDurationSeconds,
			float holdSeconds,
			Transform selectedTarget,
			CancellationToken cancellationToken)
		{
			center.y = height;
			var finalRadius = Mathf.Max(0f, maxRadius);

			if (selectedTarget != null)
			{
				var offset = selectedTarget.position - center;
				offset.y = 0f;
				finalRadius = Mathf.Min(finalRadius, offset.magnitude);
			}

			var ringObject = new GameObject("Temporary Explosion Decision Ring");
			ringObject.transform.position = center;
			ringObject.transform.SetParent(transform, true);
			var ring = ConfigureRing(
				ringObject,
				0.01f,
				new Color(1f, 0.22f, 0.08f, 1f),
				new Color(1f, 0.95f, 0.18f, 1f),
				0.12f);
			var markerObject = default(GameObject);

			try
			{
				var elapsedTime = 0f;
				var safeDuration = Mathf.Max(0.01f, expandDurationSeconds);

				while (elapsedTime < safeDuration)
				{
					cancellationToken.ThrowIfCancellationRequested();
					elapsedTime += Time.deltaTime;
					var t = Mathf.Clamp01(elapsedTime / safeDuration);
					SetRingRadius(ring, Mathf.Lerp(0.01f, finalRadius, t));
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				}

				SetRingRadius(ring, finalRadius);

				if (selectedTarget != null)
				{
					markerObject = CreateSelectedTargetMarker(selectedTarget.position, height + 0.03f);
				}

				await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, holdSeconds)), cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				if (markerObject != null)
				{
					Destroy(markerObject);
				}

				if (ringObject != null)
				{
					Destroy(ringObject);
				}
			}
		}

		private GameObject CreateSelectedTargetMarker(Vector3 position, float height)
		{
			position.y = height;
			var markerObject = new GameObject("Temporary Explosion Selected Target Marker");
			markerObject.transform.position = position;
			markerObject.transform.SetParent(transform, true);
			ConfigureRing(
				markerObject,
				0.55f,
				new Color(1f, 1f, 1f, 1f),
				new Color(1f, 0.05f, 0.02f, 1f),
				0.16f);
			return markerObject;
		}

		private LineRenderer ConfigureRing(
			GameObject cue,
			float radius,
			Color startColor,
			Color endColor,
			float width)
		{
			var lineRenderer = cue.AddComponent<LineRenderer>();
			lineRenderer.loop = true;
			lineRenderer.useWorldSpace = false;
			lineRenderer.positionCount = 48;
			lineRenderer.startWidth = width;
			lineRenderer.endWidth = width;
			lineRenderer.material = CreateTemporaryMaterial(startColor);
			lineRenderer.startColor = startColor;
			lineRenderer.endColor = endColor;
			SetRingRadius(lineRenderer, radius);
			return lineRenderer;
		}

		private void SetRingRadius(LineRenderer lineRenderer, float radius)
		{
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
