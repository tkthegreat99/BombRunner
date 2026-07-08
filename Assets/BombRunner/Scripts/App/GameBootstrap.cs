using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace BombRunner.Scripts.App
{
	public sealed class GameBootstrap : MonoBehaviour
	{
		private SaveService saveService;
		private DataManager dataManager;
		private SoundManager soundManager;
		private UiManager uiManager;
		private SceneLoader sceneLoader;
		private GameSettings gameSettings;
		private bool isInitialized;
		private bool hasDependencies;

		[Inject]
		public void Construct(
			SaveService saveService,
			DataManager dataManager,
			SoundManager soundManager,
			UiManager uiManager,
			SceneLoader sceneLoader,
			GameSettings gameSettings)
		{
			this.saveService = saveService;
			this.dataManager = dataManager;
			this.soundManager = soundManager;
			this.uiManager = uiManager;
			this.sceneLoader = sceneLoader;
			this.gameSettings = gameSettings;
			hasDependencies = true;
		}

		private void Start()
		{
			if (!hasDependencies)
			{
				Debug.LogError("GameBootstrap 의존성 미주입. Init 씬의 ProjectLifetimeScope 설정 확인 필요");
				return;
			}

			BootstrapAsync(this.GetCancellationTokenOnDestroy()).Forget();
		}

		// 전역 서비스 초기화 후 MainMenu 씬으로 이동하는 앱 시작 흐름
		private async UniTask BootstrapAsync(CancellationToken cancellationToken)
		{
			if (isInitialized)
			{
				return;
			}

			isInitialized = true;

			try
			{
				await InitializeServicesAsync(cancellationToken);
				await sceneLoader.LoadSceneAsync(gameSettings.MainMenuSceneName, cancellationToken);
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				Debug.Log("GameBootstrap 초기화 취소");
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		// Save, Data, Sound, UI 순서로 전역 서비스를 준비
		private async UniTask InitializeServicesAsync(CancellationToken cancellationToken)
		{
			await saveService.InitializeAsync(cancellationToken);
			await dataManager.InitializeAsync(cancellationToken);
			await soundManager.InitializeAsync(cancellationToken);
			await uiManager.InitializeAsync(cancellationToken);
		}
	}
}
