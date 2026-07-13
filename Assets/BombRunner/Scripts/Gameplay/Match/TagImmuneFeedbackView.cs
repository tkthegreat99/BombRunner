using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class TagImmuneFeedbackView : MonoBehaviour
	{
		[SerializeField] private TextMesh textMesh;
		[SerializeField] private Vector3 anchorOffset = new(0f, 1.65f, 0f);
		[SerializeField] private float durationSeconds = 0.75f;
		[SerializeField] private int fontSize = 48;
		[SerializeField] private Color textColor = new(1f, 0.88f, 0.18f, 1f);

		private bool didWarnMissingTextMesh;

		public void Play(
			string message,
			Transform anchor,
			Transform cameraTransform,
			CancellationToken cancellationToken)
		{
			PlayAsync(message, anchor, cameraTransform, cancellationToken).Forget();
		}

		private async UniTaskVoid PlayAsync(
			string message,
			Transform anchor,
			Transform cameraTransform,
			CancellationToken cancellationToken)
		{
			if (anchor == null || !EnsureRequiredReferences())
			{
				Destroy(gameObject);
				return;
			}

			textMesh.text = message;
			textMesh.fontSize = fontSize;
			textMesh.color = textColor;

			try
			{
				var elapsedTime = 0f;
				var safeDuration = Mathf.Max(0f, durationSeconds);

				while (elapsedTime < safeDuration)
				{
					cancellationToken.ThrowIfCancellationRequested();

					if (anchor == null)
					{
						break;
					}

					elapsedTime += Time.deltaTime;
					transform.position = anchor.position + anchorOffset;
					FaceCamera(cameraTransform);
					await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
				}
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
			if (textMesh != null)
			{
				return true;
			}

			TryGetComponent(out textMesh);

			if (textMesh != null)
			{
				return true;
			}

			if (!didWarnMissingTextMesh)
			{
				didWarnMissingTextMesh = true;
				Debug.LogWarning("TagImmuneFeedbackView에 textMesh 미연결. prefab의 TextMesh를 연결해야 합니다.");
			}

			return false;
		}

		private void FaceCamera(Transform cameraTransform)
		{
			if (cameraTransform == null)
			{
				return;
			}

			var direction = transform.position - cameraTransform.position;

			if (direction.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		}
	}
}
