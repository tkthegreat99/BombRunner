using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class TargetMarkerFeedbackView : MonoBehaviour
	{
		[SerializeField] private LineRenderer markerRing;
		[SerializeField] private int segmentCount = 48;
		[SerializeField] private float radius = 0.72f;
		[SerializeField] private float heightOffset = 0.08f;
		[SerializeField] private float width = 0.09f;
		[SerializeField] private Color startColor = new(1f, 0.92f, 0.12f, 1f);
		[SerializeField] private Color endColor = new(1f, 0.32f, 0.08f, 1f);

		private Transform target;
		private Material markerMaterial;

		public void SetTarget(Transform target)
		{
			this.target = target;
			EnsureRenderer();
			gameObject.SetActive(target != null);
			UpdateTransform();
		}

		private void LateUpdate()
		{
			UpdateTransform();
		}

		private void UpdateTransform()
		{
			if (target == null)
			{
				return;
			}

			var position = target.position;
			position.y += heightOffset;
			transform.position = position;
		}

		private void EnsureRenderer()
		{
			if (markerRing == null)
			{
				markerRing = gameObject.AddComponent<LineRenderer>();
			}

			markerRing.loop = true;
			markerRing.useWorldSpace = false;
			markerRing.positionCount = Mathf.Max(8, segmentCount);
			markerRing.startWidth = width;
			markerRing.endWidth = width;
			markerRing.startColor = startColor;
			markerRing.endColor = endColor;
			markerRing.material = GetMaterial(startColor);

			for (var i = 0; i < markerRing.positionCount; i++)
			{
				var angle = Mathf.PI * 2f * i / markerRing.positionCount;
				markerRing.SetPosition(i, new Vector3(
					Mathf.Cos(angle) * radius,
					0f,
					Mathf.Sin(angle) * radius));
			}
		}

		private Material GetMaterial(Color color)
		{
			if (markerMaterial != null)
			{
				return markerMaterial;
			}

			var shader = Shader.Find("Universal Render Pipeline/Unlit");

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			markerMaterial = new Material(shader);

			if (markerMaterial.HasProperty("_BaseColor"))
			{
				markerMaterial.SetColor("_BaseColor", color);
			}

			if (markerMaterial.HasProperty("_Color"))
			{
				markerMaterial.SetColor("_Color", color);
			}

			SetMaterialFloatIfPresent(markerMaterial, "_Surface", 1f);
			SetMaterialFloatIfPresent(markerMaterial, "_Blend", 0f);
			SetMaterialFloatIfPresent(markerMaterial, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
			SetMaterialFloatIfPresent(markerMaterial, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			SetMaterialFloatIfPresent(markerMaterial, "_ZWrite", 0f);
			markerMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			markerMaterial.renderQueue = 3000;
			return markerMaterial;
		}

		private void SetMaterialFloatIfPresent(Material material, string propertyName, float value)
		{
			if (material.HasProperty(propertyName))
			{
				material.SetFloat(propertyName, value);
			}
		}

		private void OnDestroy()
		{
			if (markerMaterial != null)
			{
				Destroy(markerMaterial);
			}
		}
	}
}
