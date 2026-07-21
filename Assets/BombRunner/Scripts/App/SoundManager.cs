using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.App
{
	// 전역 사운드 상태와 오디오 풀을 담당할 서비스 자리.
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
