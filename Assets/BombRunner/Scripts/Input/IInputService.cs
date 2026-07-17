using UnityEngine;

namespace BombRunner.Scripts.Input
{
	public interface IInputService
	{
		Vector2 Move { get; }

		bool DashPressed { get; }

		bool TauntHeld { get; }

		bool UseItemPressed { get; }
	}
}
