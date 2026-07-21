using UnityEngine;

namespace BombRunner.Scripts.Input
{
	// gameplay 코드가 New Input System에 직접 의존하지 않게 하는 입력 경계.
	public interface IInputService
	{
		Vector2 Move { get; }

		bool DashPressed { get; }

		bool TauntHeld { get; }

		bool UseItemPressed { get; }
	}
}
