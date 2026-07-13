using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using UnityEngine;
using VContainer;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalWorldFeedbackView : MonoBehaviour
	{
		[SerializeField] private Transform feedbackRoot;
		[SerializeField] private TargetMarkerFeedbackView targetMarkerPrefab;
		[SerializeField] private BombTargetLinkFeedbackView bombTargetLinkPrefab;
		[SerializeField] private TauntAreaFeedbackView tauntAreaPrefab;

		private BombTargetService bombTargetService;
		private BombSpawnService bombSpawnService;
		private GameBalanceSettings balanceSettings;
		private PlayerStateController[] players;
		private TargetMarkerFeedbackView targetMarkerView;
		private BombTargetLinkFeedbackView bombTargetLinkView;
		private TauntAreaFeedbackView[] tauntAreaViews;
		private bool isInitialized;
		private bool didWarnMissingTargetMarkerPrefab;
		private bool didWarnMissingBombTargetLinkPrefab;
		private bool didWarnMissingTauntAreaPrefab;

		[Inject]
		public void Construct(
			BombTargetService bombTargetService,
			BombSpawnService bombSpawnService,
			GameBalanceSettings balanceSettings)
		{
			this.bombTargetService = bombTargetService;
			this.bombSpawnService = bombSpawnService;
			this.balanceSettings = balanceSettings;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 0;

			if (!isInitialized)
			{
				ClearFeedback();
				return;
			}

			tauntAreaViews = new TauntAreaFeedbackView[players.Length];
		}

		private void LateUpdate()
		{
			if (!isInitialized)
			{
				return;
			}

			UpdateTargetFeedback();
			UpdateTauntAreaFeedback();
		}

		private void UpdateTargetFeedback()
		{
			var targetPlayer = bombTargetService != null ? bombTargetService.TargetPlayer : null;
			var targetTransform = targetPlayer != null && targetPlayer.IsAlive ? targetPlayer.transform : null;
			var bombController = bombSpawnService != null ? bombSpawnService.CurrentController : null;
			var bombTransform = bombController != null ? bombController.transform : null;

			if (targetTransform == null)
			{
				if (targetMarkerView != null)
				{
					targetMarkerView.SetTarget(null);
				}

				if (bombTargetLinkView != null)
				{
					bombTargetLinkView.SetEndpoints(null, null);
				}

				return;
			}

			var markerView = EnsureTargetMarkerView();

			if (markerView != null)
			{
				markerView.SetTarget(targetTransform);
			}

			var linkView = EnsureBombTargetLinkView();

			if (linkView != null)
			{
				linkView.SetEndpoints(bombTransform, targetTransform);
			}
		}

		private void UpdateTauntAreaFeedback()
		{
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];
				var shouldShow = player != null && player.IsAlive && player.IsTaunting;
				var tauntView = tauntAreaViews != null ? tauntAreaViews[i] : null;

				if (tauntView == null && shouldShow)
				{
					tauntView = EnsureTauntAreaView(i);
				}

				if (tauntView == null)
				{
					continue;
				}

				tauntView.SetAnchor(
					shouldShow ? player.transform : null,
					shouldShow && balanceSettings != null ? balanceSettings.TauntRadius : 0f);
			}
		}

		private TargetMarkerFeedbackView EnsureTargetMarkerView()
		{
			if (targetMarkerView != null)
			{
				return targetMarkerView;
			}

			if (targetMarkerPrefab == null)
			{
				if (!didWarnMissingTargetMarkerPrefab)
				{
					didWarnMissingTargetMarkerPrefab = true;
					Debug.LogWarning("LocalWorldFeedbackView에 targetMarkerPrefab 미연결. 타깃 마커 prefab View를 연결해야 합니다.");
				}

				return null;
			}

			targetMarkerView = Instantiate(targetMarkerPrefab, GetFeedbackRoot());
			targetMarkerView.gameObject.SetActive(false);
			return targetMarkerView;
		}

		private BombTargetLinkFeedbackView EnsureBombTargetLinkView()
		{
			if (bombTargetLinkView != null)
			{
				return bombTargetLinkView;
			}

			if (bombTargetLinkPrefab == null)
			{
				if (!didWarnMissingBombTargetLinkPrefab)
				{
					didWarnMissingBombTargetLinkPrefab = true;
					Debug.LogWarning("LocalWorldFeedbackView에 bombTargetLinkPrefab 미연결. 폭탄-타깃 연결선 prefab View를 연결해야 합니다.");
				}

				return null;
			}

			bombTargetLinkView = Instantiate(bombTargetLinkPrefab, GetFeedbackRoot());
			bombTargetLinkView.gameObject.SetActive(false);
			return bombTargetLinkView;
		}

		private TauntAreaFeedbackView EnsureTauntAreaView(int index)
		{
			if (tauntAreaViews == null || index < 0 || index >= tauntAreaViews.Length)
			{
				return null;
			}

			if (tauntAreaViews[index] != null)
			{
				return tauntAreaViews[index];
			}

			if (tauntAreaPrefab == null)
			{
				if (!didWarnMissingTauntAreaPrefab)
				{
					didWarnMissingTauntAreaPrefab = true;
					Debug.LogWarning("LocalWorldFeedbackView에 tauntAreaPrefab 미연결. 도발 dash-lock 영역 prefab View를 연결해야 합니다.");
				}

				return null;
			}

			tauntAreaViews[index] = Instantiate(tauntAreaPrefab, GetFeedbackRoot());
			tauntAreaViews[index].gameObject.SetActive(false);
			return tauntAreaViews[index];
		}

		private Transform GetFeedbackRoot()
		{
			return feedbackRoot != null ? feedbackRoot : transform;
		}

		private void ClearFeedback()
		{
			if (targetMarkerView != null)
			{
				targetMarkerView.SetTarget(null);
			}

			if (bombTargetLinkView != null)
			{
				bombTargetLinkView.SetEndpoints(null, null);
			}

			if (tauntAreaViews == null)
			{
				return;
			}

			for (var i = 0; i < tauntAreaViews.Length; i++)
			{
				if (tauntAreaViews[i] != null)
				{
					tauntAreaViews[i].SetAnchor(null, 0f);
				}
			}
		}
	}
}
