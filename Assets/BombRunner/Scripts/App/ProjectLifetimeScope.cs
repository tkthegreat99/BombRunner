using BombRunner.Scripts.Localization;
using BombRunner.Scripts.Multiplayer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.App
{
	// 게임 실행 동안 유지되는 전역 VContainer LifetimeScope.
	public sealed class ProjectLifetimeScope : LifetimeScope
	{
		// 게임 흐름과 전역 설정은 GameSettings ScriptableObject에서만 관리.
		[SerializeField] private GameSettings gameSettings = default;

		protected override void Awake()
		{
			// MainMenu 재진입 시에도 전역 서비스 컨테이너 유지.
			DontDestroyOnLoad(gameObject);
			base.Awake();
		}

		protected override void Configure(IContainerBuilder builder)
		{
			var activeGameSettings = gameSettings;

			if (activeGameSettings == null)
			{
				activeGameSettings = ScriptableObject.CreateInstance<GameSettings>();
				activeGameSettings.name = "RuntimeGameSettings";
				Debug.LogWarning("ProjectLifetimeScope에 GameSettings 미연결. 기본 게임 설정으로 초기화합니다.");
			}

			builder.RegisterInstance(activeGameSettings);

			// Init 씬에서 게임 실행 내내 유지할 전역 서비스 등록.
			builder.Register<SceneLoader>(Lifetime.Singleton);
			builder.Register<SceneFlowService>(Lifetime.Singleton);
			builder.Register<SaveService>(Lifetime.Singleton);
			builder.Register<DataManager>(Lifetime.Singleton);
			builder.Register<SoundManager>(Lifetime.Singleton);
			builder.Register<UiManager>(Lifetime.Singleton);
			builder.Register<LocalizationService>(Lifetime.Singleton);
			builder.RegisterEntryPoint<SteamworksClientService>(Lifetime.Singleton);
			builder.RegisterEntryPoint<SteamLobbyService>(Lifetime.Singleton);

			// Init 씬에 배치된 GameBootstrap에 서비스 주입.
			builder.RegisterComponentInHierarchy<GameBootstrap>();
		}
	}
}
