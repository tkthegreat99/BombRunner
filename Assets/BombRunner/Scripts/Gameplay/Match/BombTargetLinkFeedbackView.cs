using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class BombTargetLinkFeedbackView : MonoBehaviour
	{
		[SerializeField] private LineRenderer linkLine;
		[SerializeField] private float width = 0.08f;
		[SerializeField] private float heightOffset = 0.45f;
		[SerializeField] private Color startColor = new(1f, 0.2f, 0.08f, 0.85f);
		[SerializeField] private Color endColor = new(1f, 0.92f, 0.16f, 0.85f);

		private Transform from;
		private Transform to;
		private Material linkMaterial;

		public void SetEndpoints(Transform from, Transform to)
		{
			this.from = from;
			this.to = to;
			EnsureRenderer();
			gameObject.SetActive(from != null && to != null);
			UpdateLine();
		}

		private void LateUpdate()
		{
			UpdateLine();
		}

		private void UpdateLine()
		{
			if (from == null || to == null || linkLine == null)
			{
				return;
			}

			var fromPosition = from.position + Vector3.up * heightOffset;
			var toPosition = to.position + Vector3.up * heightOffset;
			linkLine.SetPosition(0, fromPosition);
			linkLine.SetPosition(1, toPosition);
		}

		private void EnsureRenderer()
		{
			if (linkLine == null)
			{
				linkLine = gameObject.AddComponent<LineRenderer>();
			}

			linkLine.loop = false;
			linkLine.useWorldSpace = true;
			linkLine.positionCount = 2;
			linkLine.startWidth = width;
			linkLine.endWidth = width;
			linkLine.startColor = startColor;
			linkLine.endColor = endColor;
			linkLine.material = GetMaterial(startColor);
		}

		private Material GetMaterial(Color color)
		{
			if (linkMaterial != null)
			{
				return linkMaterial;
			}

			var shader = Shader.Find("Universal Render Pipeline/Unlit");

			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}

			linkMaterial = new Material(shader);

			if (linkMaterial.HasProperty("_BaseColor"))
			{
				linkMaterial.SetColor("_BaseColor", color);
			}

			if (linkMaterial.HasProperty("_Color"))
			{
				linkMaterial.SetColor("_Color", color);
			}

			SetMaterialFloatIfPresent(linkMaterial, "_Surface", 1f);
			SetMaterialFloatIfPresent(linkMaterial, "_Blend", 0f);
			SetMaterialFloatIfPresent(linkMaterial, "_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
			SetMaterialFloatIfPresent(linkMaterial, "_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			SetMaterialFloatIfPresent(linkMaterial, "_ZWrite", 0f);
			linkMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
			linkMaterial.renderQueue = 3000;
			return linkMaterial;
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
			if (linkMaterial != null)
			{
				Destroy(linkMaterial);
			}
		}
	}
}
