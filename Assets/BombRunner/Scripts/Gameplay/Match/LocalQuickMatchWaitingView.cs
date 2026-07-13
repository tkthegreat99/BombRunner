using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalQuickMatchWaitingView : MonoBehaviour
	{
		[SerializeField] private TextMesh statusText;
		[SerializeField] private Transform cameraTransform;
		[SerializeField] private Vector3 fallbackWorldPosition = new(0f, 3f, 0f);

		private GameObject temporaryTextObject;
		private bool didWarnRuntimeView;

		public void ShowWaiting(int currentParticipants, int maxParticipants)
		{
			SetText($"입장 중 {currentParticipants} / {maxParticipants}");
			Debug.Log($"Quick match waiting: participants {currentParticipants}/{maxParticipants}");
		}

		public void ShowCountdown(int seconds)
		{
			SetText($"매치 시작 {seconds}");
			Debug.Log($"Quick match waiting: countdown {seconds}");
		}

		public void ShowStarting()
		{
			SetText("매치 시작");
			Debug.Log("Quick match waiting: local match start");
		}

		public void Hide()
		{
			if (statusText != null)
			{
				statusText.text = "";
			}
		}

		private void SetText(string message)
		{
			var activeText = EnsureStatusText();

			if (activeText == null)
			{
				return;
			}

			activeText.text = message;
			FaceCamera(activeText.transform);
		}

		private TextMesh EnsureStatusText()
		{
			if (statusText != null)
			{
				return statusText;
			}

			if (!didWarnRuntimeView)
			{
				didWarnRuntimeView = true;
				Debug.LogWarning("LocalQuickMatchWaitingView 임시 TextMesh 생성. TODO: QuickMatchWaitingScene에 씬 배치 View 연결");
			}

			if (temporaryTextObject == null)
			{
				temporaryTextObject = new GameObject("Temporary Quick Match Waiting Text");
				temporaryTextObject.transform.SetParent(transform, false);
				temporaryTextObject.transform.position = fallbackWorldPosition;

				statusText = temporaryTextObject.AddComponent<TextMesh>();
				statusText.anchor = TextAnchor.MiddleCenter;
				statusText.alignment = TextAlignment.Center;
				statusText.characterSize = 0.36f;
				statusText.fontSize = 96;
				statusText.color = new Color(0.55f, 0.95f, 1f, 1f);
			}

			return statusText;
		}

		private void FaceCamera(Transform target)
		{
			var mainCamera = UnityEngine.Camera.main;
			var activeCameraTransform = cameraTransform != null
				? cameraTransform
				: mainCamera != null
					? mainCamera.transform
					: null;

			if (activeCameraTransform == null)
			{
				return;
			}

			var direction = target.position - activeCameraTransform.position;

			if (direction.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			target.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		}
	}
}
