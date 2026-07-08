using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BombRunner.Scripts.App
{
	public sealed class MainMenuQuickStartController : MonoBehaviour
	{
		[SerializeField] private GameSettings gameSettings;

		private readonly SceneLoader sceneLoader = new();
		private bool isLoading;

		private void Start()
		{
			Debug.Log("MainMenu 테스트 진입: Enter, Space, G 중 하나를 누르면 Game 씬으로 이동합니다.");
		}

		private void Update()
		{
			if (isLoading || Keyboard.current == null)
			{
				return;
			}

			if (!WasStartRequested())
			{
				return;
			}

			LoadGameAsync(this.GetCancellationTokenOnDestroy()).Forget();
		}

		private bool WasStartRequested()
		{
			var keyboard = Keyboard.current;
			return keyboard.enterKey.wasPressedThisFrame
				|| keyboard.spaceKey.wasPressedThisFrame
				|| keyboard.gKey.wasPressedThisFrame;
		}

		// 임시 MainMenu 진입 경로. 씬 이름은 GameSettings를 통해서만 가져온다.
		private async UniTaskVoid LoadGameAsync(CancellationToken cancellationToken)
		{
			if (gameSettings == null)
			{
				Debug.LogError("MainMenuQuickStartController에 GameSettings가 연결되지 않았습니다.");
				return;
			}

			isLoading = true;

			try
			{
				await sceneLoader.LoadSceneAsync(gameSettings.GameSceneName, cancellationToken);
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
	}
}
