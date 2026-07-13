using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BombRunner.Scripts.App
{
	public sealed class ProjectLifetimeScope : LifetimeScope
	{
		// 씬 이름과 전역 설정은 GameSettings ScriptableObject에서만 관리
		[SerializeField] private GameSettings gameSettings = default;

		protected override void Awake()
		{
			// MainMenu 씬으로 이동해도 전역 서비스 컨테이너가 유지
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
				Debug.LogWarning("ProjectLifetimeScope에 GameSettings 미연결. 기본 씬 이름으로 초기화");
			}

			builder.RegisterInstance(activeGameSettings);

			// Init 씬에서 게임 실행 내내 유지될 전역 서비스를 등록
			builder.Register<SceneLoader>(Lifetime.Singleton);
			builder.Register<SceneFlowService>(Lifetime.Singleton);
			builder.Register<SaveService>(Lifetime.Singleton);
			builder.Register<DataManager>(Lifetime.Singleton);
			builder.Register<SoundManager>(Lifetime.Singleton);
			builder.Register<UiManager>(Lifetime.Singleton);

			// Init 씬에 배치된 GameBootstrap에 위 서비스들을 주입
			builder.RegisterComponentInHierarchy<GameBootstrap>();
		}
	}
}
