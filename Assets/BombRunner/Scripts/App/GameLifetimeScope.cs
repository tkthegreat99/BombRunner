using BombRunner.Scripts.Camera;
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
		[SerializeField] private LocalPlayerCameraFollow cameraFollow;
		[SerializeField] private DashCooldownLogView dashCooldownLogView;

		protected override void Configure(IContainerBuilder builder)
		{
			var activeInputReader = playerInputReader;

			if (activeInputReader == null)
			{
				activeInputReader = GetComponent<PlayerInputReader>();
			}

			if (activeInputReader == null)
			{
				Debug.LogError("GameLifetimeScope에 PlayerInputReader가 필요합니다.");
				return;
			}

			var activeSpawnSettings = playerSpawnSettings;

			if (activeSpawnSettings == null)
			{
				activeSpawnSettings = Resources.Load<PlayerSpawnSettings>(PlayerSpawnSettingsResourcePath.DefaultSettings);
			}

			if (activeSpawnSettings == null)
			{
				Debug.LogError("PlayerSpawnSettings를 찾을 수 없습니다. Resources/PlayerSpawnSettings 연결이 필요합니다.");
				return;
			}

			if (cameraFollow == null)
			{
				cameraFollow = FindFirstObjectByType<LocalPlayerCameraFollow>();
			}

			if (dashCooldownLogView == null)
			{
				dashCooldownLogView = GetComponent<DashCooldownLogView>();
			}

			if (cameraFollow == null || dashCooldownLogView == null)
			{
				Debug.LogError("Game Scene 테스트 컴포넌트 연결이 필요합니다. CameraFollow와 DashCooldownLogView를 확인하세요.");
				return;
			}

			// Game Scene 동안 사용할 입력, 스폰, 로컬 타겟 토스 검증 흐름을 등록한다.
			builder.RegisterComponent(activeInputReader);
			builder.RegisterComponent(cameraFollow);
			builder.RegisterComponent(dashCooldownLogView);
			builder.RegisterInstance(activeSpawnSettings);
			builder.Register<IInputService, InputService>(Lifetime.Scoped);
			builder.Register<PlayerSpawnService>(Lifetime.Scoped);
			builder.Register<LocalTargetTossPrototype>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
			builder.RegisterEntryPoint<StageManager>(Lifetime.Scoped);

			Debug.Log("GameLifetimeScope 등록 완료: 로컬 플레이어와 임시 더미 스폰 준비");
		}
	}
}
