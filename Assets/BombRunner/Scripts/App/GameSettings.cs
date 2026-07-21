using UnityEngine;

namespace BombRunner.Scripts.App
{
	[CreateAssetMenu(fileName = "GameSettings", menuName = "Boom Runner/Game Settings")]
	// 씬 이름과 빠른 대전 프로토타입 값을 모아두는 전역 설정.
	public sealed class GameSettings : ScriptableObject
	{
		// 부트스트랩이 시작되는 Init 씬 이름
		[SerializeField] private string initSceneName = "Init";

		// 초기화 완료 후 진입할 메인 메뉴 씬 이름
		[SerializeField] private string mainMenuSceneName = "MainMenu";

		// 실제 게임 플레이 씬 이름
		[SerializeField] private string gameSceneName = "Game";

		// 대기장 전용 씬을 만들기 전까지 비워두면 Game 씬의 임시 대기장 모드 사용
		[SerializeField] private string quickMatchWaitingSceneName = "";

		// 로컬 빠른 대전 대기장 프로토타입 설정
		[SerializeField] private int quickMatchMaxParticipants = 8;
		[SerializeField] private int quickMatchCountdownSeconds = 3;
		[SerializeField] private float quickMatchLocalJoinHoldSeconds = 0.45f;
		[SerializeField] private float quickMatchDummyJoinIntervalSeconds = 0.35f;
		[SerializeField] private float quickMatchStartHoldSeconds = 0.35f;

		public string InitSceneName => initSceneName;
		public string MainMenuSceneName => mainMenuSceneName;
		public string GameSceneName => gameSceneName;
		public string QuickMatchWaitingSceneName =>
			string.IsNullOrWhiteSpace(quickMatchWaitingSceneName)
				? gameSceneName
				: quickMatchWaitingSceneName;
		public bool UsesTemporaryQuickMatchWaitingMode => string.IsNullOrWhiteSpace(quickMatchWaitingSceneName);
		public int QuickMatchMaxParticipants => Mathf.Max(1, quickMatchMaxParticipants);
		public int QuickMatchCountdownSeconds => Mathf.Max(0, quickMatchCountdownSeconds);
		public float QuickMatchLocalJoinHoldSeconds => Mathf.Max(0f, quickMatchLocalJoinHoldSeconds);
		public float QuickMatchDummyJoinIntervalSeconds => Mathf.Max(0f, quickMatchDummyJoinIntervalSeconds);
		public float QuickMatchStartHoldSeconds => Mathf.Max(0f, quickMatchStartHoldSeconds);
	}
}
