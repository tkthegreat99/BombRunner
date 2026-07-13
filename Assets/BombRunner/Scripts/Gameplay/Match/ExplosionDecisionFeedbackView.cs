using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class ExplosionDecisionFeedbackView : MonoBehaviour
	{
		[SerializeField] private LineRenderer explosionRing;
		[SerializeField] private LineRenderer selectedVictimMarker;
		[SerializeField] private int segmentCount = 48;
		[SerializeField] private float selectedMarkerRadius = 0.55f;
		[SerializeField] private float selectedMarkerHeightOffset = 0.03f;
		[SerializeField] private float ringWidth = 0.12f;
		[SerializeField] private float selectedMarkerWidth = 0.16f;
		[SerializeField] private Color ringStartColor = new(1f, 0.22f, 0.08f, 1f);
		[SerializeField] private Color ringEndColor = new(1f, 0.95f, 0.18f, 1f);
		[SerializeField] private Color selectedMarkerStartColor = new(1f, 1f, 1f, 1f);
		[SerializeField] private Color selectedMarkerEndColor = new(1f, 0.05f, 0.02f, 1f);

		private Material ringMaterial;

		public void Play(
			Vector3 center,
			float maxRadius,
			float height,
			float expandDurationSeconds,
			float holdSeconds,
			Transform selectedTarget,
			CancellationToken cancellationToken)
		{
			PlayAsync(
				center,
				maxRadius,
				height,
				expandDurationSeconds,
				holdSeconds,
				selectedTarget,
				cancellationToken).Forget();
		}

		private async UniTaskVoid PlayAsync(
			Vector3 center,
			float maxRadius,
			float height,
			float expandDurationSeconds,
			float holdSeconds,
			Transform selectedTarget,
			CancellationToken cancellationToken)
		{
			if (!EnsureRequiredReferences())
			{
				Destroy(gameObject);
				return;
			}

			center.y = height;
			transform.position = center;

			var finalRadius = Mathf.Max(0f, maxRadius);

			if (selectedTarget != null)
			{
				var offset = selectedTarget.position - center;
				offset.y = 0f;
				finalRadius = Mathf.Min(finalRadius, offset.magnitude);
			}

			ConfigureRing(explosionRing, ringWidth, ringStartColor, ringEndColor);
			SetRingRadius(explosionRing, 0.01f);
			ConfigureSelectedVictimMarker(selectedTarget, height);

			try
			{
				var elapsedTime = 0f;
				var safeDuration = Mathf.Max(0.01f, expandDurationSeconds);

				while (elapsedTime < safeDuration)
				{
					cancellationToken.ThrowIfCancellationRequested();
					elapsedTime += Time.deltaTime;
					var t = Mathf.Clamp01(elapsedTime / safeDuration);
					SetRingRadius(explosionRing, Mathf.Lerp(0.01f, finalRadius, t));
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				}

				SetRingRadius(explosionRing, finalRadius);
				await UniTask.Delay(TimeSpan.FromSeconds(Mathf.Max(0f, holdSeconds)), cancellationToken: cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			finally
			{
				if (gameObject != null)
				{
					Destroy(gameObject);
				}
			}
		}

		private bool EnsureRequiredReferences()
		{
			if (explosionRing == null)
			{
				explosionRing = CreateRingRenderer("Explosion Ring");
			}

			if (selectedVictimMarker == null)
			{
				selectedVictimMarker = CreateRingRenderer("Selected Victim Marker");
				selectedVictimMarker.gameObject.SetActive(false);
			}

			return explosionRing != null;
		}

		private LineRenderer CreateRingRenderer(string objectName)
		{
			var ringObject = new GameObject(objectName);
			ringObject.transform.SetParent(transform, false);
			var lineRenderer = ringObject.AddComponent<LineRenderer>();
			lineRenderer.material = GetRingMaterial();
			return lineRenderer;
		}

		private Material GetRingMaterial()
		{
			if (ringMaterial != null)
			{
				return ringMaterial;
			}

			var shader = Shader.Find("Universal Render Pipeline/Unlit");

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			ringMaterial = new Material(shader);
			SetMaterialFloatIfPresent(ringMaterial, "_Surface", 1f);
			SetMaterialFloatIfPresent(ringMaterial, "_Blend", 0f);
			SetMaterialFloatIfPresent(ringMaterial, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
			SetMaterialFloatIfPresent(ringMaterial, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			SetMaterialFloatIfPresent(ringMaterial, "_ZWrite", 0f);
			ringMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			ringMaterial.renderQueue = 3000;
			return ringMaterial;
		}

		private void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetFloat(propertyName, value);
			}
		}

		private void ConfigureSelectedVictimMarker(Transform selectedTarget, float height)
		{
			if (selectedVictimMarker == null)
			{
				return;
			}

			if (selectedTarget == null)
			{
				selectedVictimMarker.gameObject.SetActive(false);
				return;
			}

			var markerPosition = selectedTarget.position;
			markerPosition.y = height + selectedMarkerHeightOffset;
			selectedVictimMarker.transform.position = markerPosition;
			selectedVictimMarker.transform.rotation = Quaternion.identity;
			selectedVictimMarker.gameObject.SetActive(true);
			ConfigureRing(
				selectedVictimMarker,
				selectedMarkerWidth,
				selectedMarkerStartColor,
				selectedMarkerEndColor);
			SetRingRadius(selectedVictimMarker, selectedMarkerRadius);
		}

		private void ConfigureRing(
			LineRenderer lineRenderer,
			float width,
			Color startColor,
			Color endColor)
		{
			lineRenderer.loop = true;
			lineRenderer.useWorldSpace = false;
			lineRenderer.positionCount = Mathf.Max(8, segmentCount);
			lineRenderer.startWidth = width;
			lineRenderer.endWidth = width;
			lineRenderer.startColor = startColor;
			lineRenderer.endColor = endColor;
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

		private void OnDestroy()
		{
			if (ringMaterial != null)
			{
				Destroy(ringMaterial);
			}
		}
	}
}
