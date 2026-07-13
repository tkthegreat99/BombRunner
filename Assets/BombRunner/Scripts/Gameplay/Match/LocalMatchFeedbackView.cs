using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalMatchFeedbackView : MonoBehaviour
	{
		[SerializeField] private Text spawnCueText;
		[SerializeField] private Text bombStartCountdownText;
		[SerializeField] private Text matchResultText;

		private CancellationTokenSource spawnCueCancellationTokenSource;
		private bool didWarnMissingSpawnCueText;
		private bool didWarnMissingBombStartCountdownText;
		private bool didWarnMissingMatchResultText;

		private void Awake()
		{
			ConfigureScenePlacedText();
			HideScenePlacedFeedback();
		}

		public void ShowSpawnCue(Vector3 spawnPosition, float radius, float height, float durationSeconds)
		{
			if (spawnCueText == null)
			{
				if (!didWarnMissingSpawnCueText)
				{
					didWarnMissingSpawnCueText = true;
					Debug.LogWarning("LocalMatchFeedbackView에 spawnCueText 미연결. 씬 배치 Overlay Canvas Text를 연결해야 합니다.");
				}

				return;
			}

			spawnCueCancellationTokenSource?.Cancel();
			spawnCueCancellationTokenSource?.Dispose();
			spawnCueCancellationTokenSource = new CancellationTokenSource();
			spawnCueText.text = "폭탄 출현!";
			ConfigureText(spawnCueText, new Color(1f, 0.68f, 0.08f, 1f), 34);
			spawnCueText.gameObject.SetActive(true);
			HideSpawnCueAfterDelayAsync(durationSeconds, spawnCueCancellationTokenSource.Token).Forget();
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

		public void ShowTagImmuneRejected(Transform anchor, Transform cameraTransform)
		{
			if (anchor == null)
			{
				return;
			}

			ShowTagImmuneRejectedAsync(
				anchor,
				cameraTransform,
				this.GetCancellationTokenOnDestroy()).Forget();
		}

		private async UniTaskVoid ShowTagImmuneRejectedAsync(
			Transform anchor,
			Transform cameraTransform,
			CancellationToken cancellationToken)
		{
			var textObject = CreateWorldText(
				"Temporary Tag Immune Notice",
				anchor.position + Vector3.up * 1.65f,
				new Color(1f, 0.88f, 0.18f, 1f),
				0.14f);
			var textMesh = textObject.GetComponent<TextMesh>();
			textMesh.text = "면역";
			textMesh.fontSize = 48;

			try
			{
				var elapsedTime = 0f;
				var durationSeconds = 0.75f;

				while (elapsedTime < durationSeconds)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (anchor == null)
					{
						break;
					}

					elapsedTime += Time.deltaTime;
					textObject.transform.position = anchor.position + Vector3.up * 1.65f;
					FaceCamera(textObject.transform, cameraTransform);
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				if (textObject != null)
				{
					Destroy(textObject);
				}
			}
		}

		public async UniTask ShowBombStartCountdownAsync(
			Vector3 spawnPosition,
			int countdown,
			Transform cameraTransform,
			CancellationToken cancellationToken)
		{
			var hasCountdownText = bombStartCountdownText != null;

			if (!hasCountdownText && !didWarnMissingBombStartCountdownText)
			{
				didWarnMissingBombStartCountdownText = true;
				Debug.LogWarning("LocalMatchFeedbackView에 bombStartCountdownText 미연결. 씬 배치 Overlay Canvas Text를 연결해야 합니다.");
			}

			if (hasCountdownText)
			{
				ConfigureText(bombStartCountdownText, new Color(1f, 0.85f, 0.12f, 1f), 96);
				bombStartCountdownText.gameObject.SetActive(true);
			}

			try
			{
				for (var i = Mathf.Max(0, countdown); i > 0; i--)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (hasCountdownText)
					{
						bombStartCountdownText.text = i.ToString();
					}

					await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
				}
			}
			finally
			{
				if (hasCountdownText)
				{
					bombStartCountdownText.text = "";
					bombStartCountdownText.gameObject.SetActive(false);
				}
			}
		}

		public void ShowMatchEnded(string winnerLabel, Transform anchor, Transform cameraTransform)
		{
			if (matchResultText == null)
			{
				if (!didWarnMissingMatchResultText)
				{
					didWarnMissingMatchResultText = true;
					Debug.LogWarning("LocalMatchFeedbackView에 matchResultText 미연결. 씬 배치 Overlay Canvas Text를 연결해야 합니다.");
				}

				return;
			}

			matchResultText.text = $"승자\n{winnerLabel}";
			ConfigureText(matchResultText, new Color(0.4f, 1f, 0.65f, 1f), 52);
			matchResultText.gameObject.SetActive(true);
		}

		private void OnDisable()
		{
			spawnCueCancellationTokenSource?.Cancel();
			spawnCueCancellationTokenSource?.Dispose();
			spawnCueCancellationTokenSource = null;

			HideScenePlacedFeedback();
		}

		private void HideScenePlacedFeedback()
		{
			if (spawnCueText != null)
			{
				spawnCueText.gameObject.SetActive(false);
			}

			if (bombStartCountdownText != null)
			{
				bombStartCountdownText.gameObject.SetActive(false);
			}

			if (matchResultText != null)
			{
				matchResultText.gameObject.SetActive(false);
			}
		}

		private void ConfigureScenePlacedText()
		{
			ConfigureText(spawnCueText, new Color(1f, 0.68f, 0.08f, 1f), 34);
			ConfigureText(bombStartCountdownText, new Color(1f, 0.85f, 0.12f, 1f), 96);
			ConfigureText(matchResultText, new Color(0.4f, 1f, 0.65f, 1f), 52);
		}

		private void ConfigureText(Text text, Color color, int fontSize)
		{
			if (text == null)
			{
				return;
			}

			text.alignment = TextAnchor.MiddleCenter;
			text.fontSize = fontSize;
			text.color = color;
			text.raycastTarget = false;
			EnsureTextFont(text);
		}

		private void EnsureTextFont(Text text)
		{
			if (text.font == null)
			{
				text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
					?? Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
		}

		private async UniTaskVoid HideSpawnCueAfterDelayAsync(float durationSeconds, CancellationToken cancellationToken)
		{
			try
			{
				await UniTask.Delay(
					TimeSpan.FromSeconds(Mathf.Max(0f, durationSeconds)),
					cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			if (spawnCueText != null)
			{
				spawnCueText.gameObject.SetActive(false);
			}
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
			return ConfigureRing(lineRenderer, radius, startColor, endColor, width);
		}

		private LineRenderer ConfigureRing(
			LineRenderer lineRenderer,
			float radius,
			Color startColor,
			Color endColor,
			float width)
		{
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
