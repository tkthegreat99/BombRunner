using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace BombRunner.Scripts.App
{
	public sealed class SceneLoader
	{
		// GameSettings에서 전달된 씬 이름만 사용해 raw string 분산 방지
		public async UniTask LoadSceneAsync(string sceneName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(sceneName))
			{
				throw new ArgumentException("로드할 씬 이름이 비어 있음", nameof(sceneName));
			}

			cancellationToken.ThrowIfCancellationRequested();

			var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

			if (operation == null)
			{
				throw new InvalidOperationException($"씬 로드 요청 실패: {sceneName}");
			}

			await operation.ToUniTask(cancellationToken: cancellationToken);
		}
	}
}
