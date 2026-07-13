using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class TauntAreaFeedbackView : MonoBehaviour
	{
		[SerializeField] private LineRenderer areaRing;
		[SerializeField] private int segmentCount = 64;
		[SerializeField] private float heightOffset = 0.05f;
		[SerializeField] private float width = 0.08f;
		[SerializeField] private Color startColor = new(0.25f, 0.95f, 1f, 0.9f);
		[SerializeField] private Color endColor = new(0.25f, 0.55f, 1f, 0.45f);

		private Transform anchor;
		private float radius;
		private Material areaMaterial;

		public void SetAnchor(Transform anchor, float radius)
		{
			this.anchor = anchor;
			this.radius = Mathf.Max(0f, radius);
			EnsureRenderer();
			gameObject.SetActive(anchor != null && this.radius > 0f);
			UpdateTransform();
			SetRingRadius(this.radius);
		}

		private void LateUpdate()
		{
			UpdateTransform();
		}

		private void UpdateTransform()
		{
			if (anchor == null)
			{
				return;
			}

			var position = anchor.position;
			position.y += heightOffset;
			transform.position = position;
		}

		private void EnsureRenderer()
		{
			if (areaRing == null)
			{
				areaRing = gameObject.AddComponent<LineRenderer>();
			}

			areaRing.loop = true;
			areaRing.useWorldSpace = false;
			areaRing.positionCount = Mathf.Max(8, segmentCount);
			areaRing.startWidth = width;
			areaRing.endWidth = width;
			areaRing.startColor = startColor;
			areaRing.endColor = endColor;
			areaRing.material = GetMaterial(startColor);
		}

		private void SetRingRadius(float radius)
		{
			if (areaRing == null)
			{
				return;
			}

			for (var i = 0; i < areaRing.positionCount; i++)
			{
				var angle = Mathf.PI * 2f * i / areaRing.positionCount;
				areaRing.SetPosition(i, new Vector3(
					Mathf.Cos(angle) * radius,
					0f,
					Mathf.Sin(angle) * radius));
			}
		}

		private Material GetMaterial(Color color)
		{
			if (areaMaterial != null)
			{
				return areaMaterial;
			}

			var shader = Shader.Find("Universal Render Pipeline/Unlit");

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			areaMaterial = new Material(shader);

			if (areaMaterial.HasProperty("_BaseColor"))
			{
				areaMaterial.SetColor("_BaseColor", color);
			}

			if (areaMaterial.HasProperty("_Color"))
			{
				areaMaterial.SetColor("_Color", color);
			}

			SetMaterialFloatIfPresent(areaMaterial, "_Surface", 1f);
			SetMaterialFloatIfPresent(areaMaterial, "_Blend", 0f);
			SetMaterialFloatIfPresent(areaMaterial, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
			SetMaterialFloatIfPresent(areaMaterial, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			SetMaterialFloatIfPresent(areaMaterial, "_ZWrite", 0f);
			areaMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			areaMaterial.renderQueue = 3000;
			return areaMaterial;
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
			if (areaMaterial != null)
			{
				Destroy(areaMaterial);
			}
		}
	}
}
