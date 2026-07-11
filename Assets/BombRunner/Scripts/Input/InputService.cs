using UnityEngine;

namespace BombRunner.Scripts.Input
{
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
	}
}
