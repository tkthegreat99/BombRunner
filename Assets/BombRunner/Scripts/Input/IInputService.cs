using UnityEngine;

namespace BombRunner.Scripts.Input
{
	public interface IInputService
	{
		// 플레이어 이동 의도를 XZ 평면 이동으로 변환하기 위한 2D 입력값
		Vector2 Move { get; }

		// 대시를 시작해야 하는 단발 입력. 누른 프레임에만 true가 된다.
		bool DashPressed { get; }
	}
}
