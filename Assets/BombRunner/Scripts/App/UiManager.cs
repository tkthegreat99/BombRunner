using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.App
{
	// 전역 UI View 관리와 화면 전환을 담당할 서비스 자리.
	public sealed class UiManager
	{
		private bool isInitialized;

		public bool IsInitialized => isInitialized;

		// 미리 배치되거나 프리팹화된 UI View를 관리하기 위한 초기 진입점
		public UniTask InitializeAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			isInitialized = true;
			return UniTask.CompletedTask;
		}
	}
}
