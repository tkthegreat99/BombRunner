using UnityEngine;

namespace BombRunner.Scripts.Input
{
	// PlayerInputReader 값을 gameplay용 인터페이스로 노출하는 어댑터.
	public sealed class InputService : IInputService
	{
		private readonly PlayerInputReader inputReader;

		public InputService(PlayerInputReader inputReader)
		{
			this.inputReader = inputReader;
		}

		public Vector2 Move => inputReader.Move;

		public bool DashPressed => inputReader.DashPressedThisFrame;

		public bool TauntHeld => inputReader.TauntHeld;

		public bool UseItemPressed => inputReader.UseItemPressedThisFrame;
	}
}
