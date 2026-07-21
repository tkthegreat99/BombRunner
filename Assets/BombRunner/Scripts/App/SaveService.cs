using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.App
{
	// 로컬 저장 데이터 초기화를 담당할 전역 서비스 자리.
	public sealed class SaveService
	{
		private bool isInitialized;

		public bool IsInitialized => isInitialized;

		// 로컬 저장 데이터와 유저 설정을 불러오기 위한 초기 진입점
		public UniTask InitializeAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			isInitialized = true;
			return UniTask.CompletedTask;
		}
	}
}
