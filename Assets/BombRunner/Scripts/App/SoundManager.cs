using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.App
{
	public sealed class SoundManager
	{
		private bool isInitialized;

		public bool IsInitialized => isInitialized;

		// 반복 재생될 AudioSource 풀과 전역 오디오 상태를 준비하는 초기 진입점
		public UniTask InitializeAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			isInitialized = true;
			return UniTask.CompletedTask;
		}
	}
}
