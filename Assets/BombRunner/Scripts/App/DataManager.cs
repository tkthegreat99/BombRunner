using System.Threading;
using Cysharp.Threading.Tasks;

namespace BombRunner.Scripts.App
{
	// 밸런스와 맵, 아이템 정의 데이터를 불러올 서비스 자리.
	public sealed class DataManager
	{
		private bool isInitialized;

		public bool IsInitialized => isInitialized;

		// 밸런스, 맵, 아이템 정의 데이터를 읽기 위한 초기 진입점
		public UniTask InitializeAsync(CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			isInitialized = true;
			return UniTask.CompletedTask;
		}
	}
}
