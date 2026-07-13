using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.App
{
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
			SetRequestedMatchMode(MatchMode.LocalInstantMatch);
			await sceneLoader.LoadSceneAsync(gameSettings.GameSceneName, cancellationToken);
		}

		public async UniTask LoadLocalQuickMatchWaitingAsync(CancellationToken cancellationToken)
		{
			SetRequestedMatchMode(MatchMode.LocalQuickMatchWaiting);
			await sceneLoader.LoadSceneAsync(gameSettings.QuickMatchWaitingSceneName, cancellationToken);
		}

		private void SetRequestedMatchMode(MatchMode matchMode)
		{
			requestedMatchMode = matchMode;
		}
	}
}
