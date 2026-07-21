using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.App
{
	// 메뉴 선택 결과를 다음 씬의 StageManager까지 전달하는 씬 흐름 서비스.
	public sealed class SceneFlowService
	{
		private readonly SceneLoader sceneLoader;
		private readonly GameSettings gameSettings;
		private MatchMode requestedMatchMode = MatchMode.LocalInstantMatch;

		public MatchMode RequestedMatchMode => requestedMatchMode;

		public SceneFlowService(SceneLoader sceneLoader, GameSettings gameSettings)
		{
			this.sceneLoader = sceneLoader;
			this.gameSettings = gameSettings;
		}

		public async UniTask LoadMainMenuAsync(CancellationToken cancellationToken)
		{
			await sceneLoader.LoadSceneAsync(gameSettings.MainMenuSceneName, cancellationToken);
		}

		public async UniTask LoadLocalInstantMatchAsync(CancellationToken cancellationToken)
		{
			// 즉시 로컬 매치 진입 요청 기록.
			SetRequestedMatchMode(MatchMode.LocalInstantMatch);
			await sceneLoader.LoadSceneAsync(gameSettings.GameSceneName, cancellationToken);
		}

		public async UniTask LoadLocalQuickMatchWaitingAsync(CancellationToken cancellationToken)
		{
			// 대기장 표시 후 로컬/Steam 매치 진입 요청 기록.
			SetRequestedMatchMode(MatchMode.LocalQuickMatchWaiting);
			await sceneLoader.LoadSceneAsync(gameSettings.QuickMatchWaitingSceneName, cancellationToken);
		}

		private void SetRequestedMatchMode(MatchMode matchMode)
		{
			requestedMatchMode = matchMode;
		}
	}
}
