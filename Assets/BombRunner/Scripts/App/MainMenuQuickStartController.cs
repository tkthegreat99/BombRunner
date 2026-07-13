using System;
using System.Threading;
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
		private bool isLoading;
		private bool hasSceneFlowService;

		[Inject]
		public void Construct(SceneFlowService sceneFlowService)
		{
			this.sceneFlowService = sceneFlowService;
			hasSceneFlowService = true;
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

			if (!WasStartRequested())
			{
				return;
			}

			LoadQuickMatchWaitingAsync(this.GetCancellationTokenOnDestroy()).Forget();
		}

		private bool WasStartRequested()
		{
			var keyboard = Keyboard.current;
			return keyboard.enterKey.wasPressedThisFrame
				|| keyboard.spaceKey.wasPressedThisFrame
				|| keyboard.gKey.wasPressedThisFrame;
		}

		// 임시 MainMenu 진입 경로. Steam Lobby 전까지 로컬 대기장 모드를 요청.
		private async UniTaskVoid LoadQuickMatchWaitingAsync(CancellationToken cancellationToken)
		{
			if (sceneFlowService == null)
			{
				Debug.LogError("MainMenuQuickStartController에 SceneFlowService가 주입되지 않았습니다. Init 씬부터 실행해 ProjectLifetimeScope를 유지해야 합니다.");
				return;
			}

			isLoading = true;

			try
			{
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
	}
}
