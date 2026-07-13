using BombRunner.Scripts.Localization;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalQuickMatchWaitingView : MonoBehaviour
	{
		private const string WaitingCountKey = "quick_match.waiting_count";
		private const string CountdownKey = "quick_match.countdown";
		private const string StartingKey = "quick_match.starting";

		[SerializeField] private Text statusText;

		private LocalizationService localizationService;
		private bool didWarnMissingStatusText;

		[Inject]
		public void Construct(LocalizationService localizationService)
		{
			this.localizationService = localizationService;
		}

		private void Awake()
		{
			ConfigureStatusText();
			Hide();
		}

		public void ShowWaiting(int currentParticipants, int maxParticipants)
		{
			SetLocalizedText(WaitingCountKey, currentParticipants, maxParticipants);
			Debug.Log($"Quick match waiting: participants {currentParticipants}/{maxParticipants}");
		}

		public void ShowCountdown(int seconds)
		{
			SetLocalizedText(CountdownKey, seconds);
			Debug.Log($"Quick match waiting: countdown {seconds}");
		}

		public void ShowStarting()
		{
			SetLocalizedText(StartingKey);
			Debug.Log("Quick match waiting: local match start");
		}

		public void Hide()
		{
			if (statusText != null)
			{
				statusText.text = "";
				statusText.gameObject.SetActive(false);
			}
		}

		private void SetLocalizedText(string key, params object[] args)
		{
			SetText(Localize(key, args));
		}

		private string Localize(string key, params object[] args)
		{
			return localizationService != null ? localizationService.Get(key, args) : key;
		}

		private void SetText(string message)
		{
			var activeText = EnsureStatusText();

			if (activeText == null)
			{
				return;
			}

			activeText.text = message;
			activeText.gameObject.SetActive(true);
		}

		private Text EnsureStatusText()
		{
			if (statusText != null)
			{
				ConfigureStatusText();
				return statusText;
			}

			if (!didWarnMissingStatusText)
			{
				didWarnMissingStatusText = true;
				Debug.LogWarning("LocalQuickMatchWaitingView에 statusText 미연결. QuickMatchWaitingScene 또는 Game 씬에 배치된 Overlay Canvas Text를 연결해야 합니다.");
			}

			return null;
		}

		private void ConfigureStatusText()
		{
			if (statusText == null)
			{
				return;
			}

			statusText.alignment = TextAnchor.MiddleCenter;
			statusText.fontSize = 34;
			statusText.color = new Color(0.55f, 0.95f, 1f, 1f);
			statusText.raycastTarget = false;
			EnsureTextFont(statusText);
		}

		private void EnsureTextFont(Text text)
		{
			if (text.font == null)
			{
				text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
					?? Resources.GetBuiltinResource<Font>("Arial.ttf");
			}
		}
	}
}
