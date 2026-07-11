using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Match;
using BombRunner.Scripts.Gameplay.Player;
using BombRunner.Scripts.Input;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.App
{
	[RequireComponent(typeof(PlayerInputReader))]
	public sealed class GameLifetimeScope : LifetimeScope
	{
		[SerializeField] private PlayerInputReader playerInputReader;
		[SerializeField] private PlayerSpawnSettings playerSpawnSettings;
		[SerializeField] private BombSpawnSettings bombSpawnSettings;
		[SerializeField] private GameBalanceSettings gameBalanceSettings;
		[SerializeField] private LocalPlayerCameraFollow cameraFollow;
		[SerializeField] private DashCooldownLogView dashCooldownLogView;
		[SerializeField] private LocalMatchFeedbackView matchFeedbackView;

		protected override void Configure(IContainerBuilder builder)
		{
			var activeInputReader = playerInputReader;

			if (activeInputReader == null)
			{
				activeInputReader = GetComponent<PlayerInputReader>();
			}

			if (activeInputReader == null)
			{
				Debug.LogError("GameLifetimeScope requires PlayerInputReader.");
				return;
			}

			var activeSpawnSettings = playerSpawnSettings;

			if (activeSpawnSettings == null)
			{
				activeSpawnSettings = Resources.Load<PlayerSpawnSettings>(PlayerSpawnSettingsResourcePath.DefaultSettings);
			}

			if (activeSpawnSettings == null)
			{
				Debug.LogError("PlayerSpawnSettings is missing. Check Resources/PlayerSpawnSettings.");
				return;
			}

			var activeBombSpawnSettings = bombSpawnSettings;

			if (activeBombSpawnSettings == null)
			{
				activeBombSpawnSettings = Resources.Load<BombSpawnSettings>(BombSpawnSettingsResourcePath.DefaultSettings);
			}

			if (activeBombSpawnSettings == null || activeBombSpawnSettings.BombPrefab == null)
			{
				Debug.LogError("BombSpawnSettings or BombPrefab is missing. Check Resources/BombSpawnSettings.");
				return;
			}

			var activeBalanceSettings = gameBalanceSettings;

			if (activeBalanceSettings == null)
			{
				activeBalanceSettings = Resources.Load<GameBalanceSettings>(GameBalanceSettingsResourcePath.DefaultSettings);
			}

			if (activeBalanceSettings == null)
			{
				activeBalanceSettings = ScriptableObject.CreateInstance<GameBalanceSettings>();
				Debug.LogWarning("GameBalanceSettings is missing. Runtime defaults will be used.");
			}

			if (cameraFollow == null)
			{
				cameraFollow = FindFirstObjectByType<LocalPlayerCameraFollow>();
			}

			if (dashCooldownLogView == null)
			{
				dashCooldownLogView = GetComponent<DashCooldownLogView>();
			}

			if (matchFeedbackView == null)
			{
				matchFeedbackView = FindFirstObjectByType<LocalMatchFeedbackView>();
			}

			if (matchFeedbackView == null)
			{
				var feedbackViewObject = new GameObject("Local Match Feedback View");
				matchFeedbackView = feedbackViewObject.AddComponent<LocalMatchFeedbackView>();
			}

			if (cameraFollow == null || dashCooldownLogView == null)
			{
				Debug.LogError("Game Scene test components are missing. Check CameraFollow and DashCooldownLogView.");
				return;
			}

			builder.RegisterComponent(activeInputReader);
			builder.RegisterComponent(cameraFollow);
			builder.RegisterComponent(dashCooldownLogView);
			builder.RegisterComponent(matchFeedbackView);
			builder.RegisterInstance(activeSpawnSettings);
			builder.RegisterInstance(activeBombSpawnSettings);
			builder.RegisterInstance(activeBalanceSettings);
			builder.Register<IInputService, InputService>(Lifetime.Scoped);
			builder.Register<PlayerSpawnService>(Lifetime.Scoped);
			builder.Register<BombState>(Lifetime.Scoped);
			builder.Register<BombTargetService>(Lifetime.Scoped);
			builder.Register<BombSpawnService>(Lifetime.Scoped);
			builder.Register<LocalMatchFlowService>(Lifetime.Scoped);
			builder.Register<LocalPlayerSeparationService>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
			builder.Register<LocalTargetTossPrototype>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
			builder.Register<LocalTauntPrototype>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
			builder.RegisterEntryPoint<StageManager>(Lifetime.Scoped);

			Debug.Log("GameLifetimeScope registration complete: local prototype match loop ready.");
		}
	}
}
