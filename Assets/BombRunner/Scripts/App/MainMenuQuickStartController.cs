using System;
using System.Threading;
using BombRunner.Scripts.Multiplayer;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.App
{
	public sealed class MainMenuQuickStartController : MonoBehaviour
	{
		private SceneFlowService sceneFlowService;
		private ISteamLobbyService steamLobbyService;
		private GameSettings gameSettings;
		private bool isLoading;
		private bool hasSceneFlowService;
		private bool isSteamLobbySubscribed;

		[Inject]
		public void Construct(
			SceneFlowService sceneFlowService,
			ISteamLobbyService steamLobbyService,
			GameSettings gameSettings)
		{
			this.sceneFlowService = sceneFlowService;
			this.steamLobbyService = steamLobbyService;
			this.gameSettings = gameSettings;
			hasSceneFlowService = true;
			SubscribeSteamLobbyChanged();
		}

		private void OnEnable()
		{
			SubscribeSteamLobbyChanged();
		}

		private void OnDisable()
		{
			if (steamLobbyService != null && isSteamLobbySubscribed)
			{
				steamLobbyService.Changed -= OnSteamLobbyChanged;
				isSteamLobbySubscribed = false;
			}
		}

		private void Awake()
		{
			if (hasSceneFlowService)
			{
				return;
			}

			var projectScope = LifetimeScope.Find<ProjectLifetimeScope>();

			if (projectScope != null)
			{
				projectScope.Container.Inject(this);
			}
		}

		private void Start()
		{
			Debug.Log("MainMenu 테스트 진입: Enter, Space, G 중 하나를 누르면 빠른 대전 대기장으로 이동합니다.");
		}

		private void Update()
		{
			if (isLoading || Keyboard.current == null)
			{
				return;
			}

			if (!WasStartRequested(out var forceLocalFallback))
			{
				return;
			}

			LoadQuickMatchWaitingAsync(forceLocalFallback, this.GetCancellationTokenOnDestroy()).Forget();
		}

		private bool WasStartRequested(out bool forceLocalFallback)
		{
			var keyboard = Keyboard.current;
			forceLocalFallback = keyboard.gKey.wasPressedThisFrame;
			return keyboard.enterKey.wasPressedThisFrame
				|| keyboard.spaceKey.wasPressedThisFrame
				|| forceLocalFallback;
		}

		// 임시 MainMenu 진입 경로. Steam Lobby 전까지 로컬 대기장 모드를 요청.
		private async UniTaskVoid LoadQuickMatchWaitingAsync(
			bool forceLocalFallback,
			CancellationToken cancellationToken)
		{
			if (sceneFlowService == null)
			{
				Debug.LogError("MainMenuQuickStartController에 SceneFlowService가 주입되지 않았습니다. Init 씬부터 실행해 ProjectLifetimeScope를 유지해야 합니다.");
				return;
			}

			isLoading = true;

			try
			{
				if (!forceLocalFallback)
				{
					if (steamLobbyService == null || !steamLobbyService.IsAvailable)
					{
						Debug.LogWarning("Steam lobby quick match unavailable. Press G for local fallback.");
						isLoading = false;
						return;
					}

					if (!steamLobbyService.IsInLobby)
					{
						var maxMembers = gameSettings != null ? gameSettings.QuickMatchMaxParticipants : 8;
						var created = await steamLobbyService.CreateQuickMatchLobbyAsync(maxMembers, cancellationToken);

						if (!created)
						{
							Debug.LogWarning("Steam lobby creation failed. Press G for local fallback.");
							isLoading = false;
							return;
						}

						steamLobbyService.OpenInviteDialog();
					}
				}

				await sceneFlowService.LoadLocalQuickMatchWaitingAsync(cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				isLoading = false;
			}
		}

		private void OnSteamLobbyChanged()
		{
			if (isLoading || steamLobbyService == null || !steamLobbyService.IsInLobby)
			{
				return;
			}

			LoadQuickMatchWaitingAsync(false, this.GetCancellationTokenOnDestroy()).Forget();
		}

		private void SubscribeSteamLobbyChanged()
		{
			if (!isActiveAndEnabled || steamLobbyService == null || isSteamLobbySubscribed)
			{
				return;
			}

			steamLobbyService.Changed += OnSteamLobbyChanged;
			isSteamLobbySubscribed = true;
			OnSteamLobbyChanged();
		}
	}
}
