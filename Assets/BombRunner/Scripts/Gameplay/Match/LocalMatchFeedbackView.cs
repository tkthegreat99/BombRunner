using System;
using System.Threading;
using BombRunner.Scripts.Localization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BombRunner.Scripts.Gameplay.Match
{
	// 로컬 매치의 오버레이 텍스트와 월드 판정 피드백을 중계하는 View.
	public sealed class LocalMatchFeedbackView : MonoBehaviour
	{
		private const string BombSpawnKey = "match.feedback.bomb_spawn";
		private const string WinnerKey = "match.feedback.winner";
		private const string TagImmuneKey = "match.feedback.tag_immune";

		[SerializeField] private Text spawnCueText;
		[SerializeField] private Text bombStartCountdownText;
		[SerializeField] private Text matchResultText;
		[SerializeField] private Transform worldFeedbackRoot;
		[SerializeField] private ExplosionDecisionFeedbackView explosionDecisionFeedbackPrefab;
		[SerializeField] private TagImmuneFeedbackView tagImmuneFeedbackPrefab;

		private LocalizationService localizationService;
		private CancellationTokenSource spawnCueCancellationTokenSource;
		private bool didWarnMissingSpawnCueText;
		private bool didWarnMissingBombStartCountdownText;
		private bool didWarnMissingMatchResultText;
		private bool didWarnMissingExplosionDecisionPrefab;
		private bool didWarnMissingTagImmunePrefab;

		[Inject]
		public void Construct(LocalizationService localizationService)
		{
			this.localizationService = localizationService;
		}

		private void Awake()
		{
			ConfigureScenePlacedText();
			HideScenePlacedFeedback();
		}

		public void ShowSpawnCue(Vector3 spawnPosition, float radius, float height, float durationSeconds)
		{
			// 폭탄 스폰 예고 텍스트 표시.
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
			spawnCueText.text = Localize(BombSpawnKey);
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
			// 폭발 판정 링과 선택 피해자 마커 표시.
			if (explosionDecisionFeedbackPrefab == null)
			{
				if (!didWarnMissingExplosionDecisionPrefab)
				{
					didWarnMissingExplosionDecisionPrefab = true;
					Debug.LogWarning("LocalMatchFeedbackView에 explosionDecisionFeedbackPrefab 미연결. 폭발 판정 링/피해자 마커 prefab View를 연결해야 합니다.");
				}

				return;
			}

			var feedbackView = Instantiate(explosionDecisionFeedbackPrefab, GetWorldFeedbackRoot());
			feedbackView.Play(
				center,
				maxRadius,
				height,
				expandDurationSeconds,
				holdSeconds,
				selectedTarget,
				this.GetCancellationTokenOnDestroy());
		}

		public void ShowTagImmuneRejected(Transform anchor, Transform cameraTransform)
		{
			if (anchor == null)
			{
				return;
			}

			if (tagImmuneFeedbackPrefab == null)
			{
				if (!didWarnMissingTagImmunePrefab)
				{
					didWarnMissingTagImmunePrefab = true;
					Debug.LogWarning("LocalMatchFeedbackView에 tagImmuneFeedbackPrefab 미연결. 태그 면역 머리 위 표시 prefab View를 연결해야 합니다.");
				}

				return;
			}

			var feedbackView = Instantiate(tagImmuneFeedbackPrefab, GetWorldFeedbackRoot());
			feedbackView.Play(
				Localize(TagImmuneKey),
				anchor,
				cameraTransform,
				this.GetCancellationTokenOnDestroy());
		}

		public async UniTask ShowBombStartCountdownAsync(
			Vector3 spawnPosition,
			int countdown,
			Transform cameraTransform,
			CancellationToken cancellationToken)
		{
			// 폭탄 활성화 전 카운트다운 표시.
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

			matchResultText.text = Localize(WinnerKey, winnerLabel);
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

		private Transform GetWorldFeedbackRoot()
		{
			return worldFeedbackRoot != null ? worldFeedbackRoot : transform;
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

		private string Localize(string key, params object[] args)
		{
			return localizationService != null ? localizationService.Get(key, args) : key;
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
	}
}
