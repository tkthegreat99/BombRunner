using UnityEngine;

namespace BombRunner.Scripts.App
{
	[CreateAssetMenu(fileName = "GameSettings", menuName = "Boom Runner/Game Settings")]
	public sealed class GameSettings : ScriptableObject
	{
		// 부트스트랩이 시작되는 Init 씬 이름
		[SerializeField] private string initSceneName = "Init";

		// 초기화 완료 후 진입할 메인 메뉴 씬 이름
		[SerializeField] private string mainMenuSceneName = "MainMenu";

		// 실제 게임 플레이 씬 이름
		[SerializeField] private string gameSceneName = "Game";

		public string InitSceneName => initSceneName;
		public string MainMenuSceneName => mainMenuSceneName;
		public string GameSceneName => gameSceneName;
	}
}
