using System.Collections.Generic;
using BombRunner.Scripts.Input;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Player
{
	// NGO가 직접 생성하는 플레이어 프리팹에 런타임 설정을 전달하기 위한 임시 공유 경계.
	public static class NetworkPlayerRuntimeSettings
	{
		public static bool IsConfigured { get; private set; }
		public static PlayerInputReader InputReader { get; private set; }
		public static Vector3 SpawnPosition { get; private set; }
		public static float MoveSpeed { get; private set; }
		public static float RotationSpeed { get; private set; }
		public static float DownedMoveSpeedMultiplier { get; private set; }
		public static ulong LocalSteamId { get; private set; }
		public static string LocalDisplayName { get; private set; } = "Player";
		public static Transform NameplateCameraTransform { get; private set; }

		private static readonly Dictionary<ulong, int> spawnIndexes = new();

		// NGO PlayerPrefab은 NetworkManager가 직접 Instantiate하므로 VContainer 생성자 주입을 받을 수 없음.
		// 첫 친구 테스트에서는 이 static 캐시로 입력, 이동 수치, 이름표 카메라, Steam persona를 전달.
		// 최종 네트워크 구조에서는 scene-placed NetworkManager와 별도 player context 주입 방식으로 교체 예정.

		// VContainer로 생성되지 않는 NetworkBehaviour가 읽을 수 있는 매치 시작 설정 캐시.
		public static void Configure(
			PlayerInputReader inputReader,
			PlayerSpawnSettings spawnSettings,
			float downedMoveSpeedMultiplier,
			Transform nameplateCameraTransform,
			ulong localSteamId,
			string localDisplayName)
		{
			// NGO PlayerPrefab은 VContainer Instantiate 경로를 타지 않으므로, 스폰 직전 필요한 값을 정적 캐시에 보관.
			InputReader = inputReader;
			SpawnPosition = spawnSettings != null ? spawnSettings.SpawnPosition : Vector3.zero;
			MoveSpeed = spawnSettings != null ? spawnSettings.MoveSpeed : 5f;
			RotationSpeed = spawnSettings != null ? spawnSettings.RotationSpeed : 720f;
			DownedMoveSpeedMultiplier = Mathf.Clamp01(downedMoveSpeedMultiplier);
			NameplateCameraTransform = nameplateCameraTransform;
			LocalSteamId = localSteamId;
			LocalDisplayName = string.IsNullOrWhiteSpace(localDisplayName) ? $"Player {localSteamId}" : localDisplayName;
			spawnIndexes.Clear();
			IsConfigured = true;
		}

		public static Vector3 GetSpawnPosition(ulong ownerClientId)
		{
			// Host에서만 호출되는 임시 배치 규칙.
			// 연결 순서 기반 원형 배치라 완전한 spawn point 시스템은 아니지만, 두 명 이동 동기화 테스트에는 충분.
			var index = GetSpawnIndex(ownerClientId);

			// Host 자신의 기본 스폰 위치 보존.
			if (index == 0)
			{
				return SpawnPosition;
			}

			// 첫 동기화 테스트용 원형 배치.
			var angle = Mathf.PI * 2f * index / 8f;
			var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3f;
			return SpawnPosition + offset;
		}

		private static int GetSpawnIndex(ulong ownerClientId)
		{
			// NGO clientId는 FacepunchTransport 내부 연결 ID라 Steam ID와 다를 수 있음.
			// 따라서 오늘 단계에서는 "처음 본 ownerClientId 순서"를 안정적인 임시 spawn index로 사용.
			// Transport clientId가 Steam ID가 아닐 수 있어 연결 순서 기반 배정.
			if (spawnIndexes.TryGetValue(ownerClientId, out var index))
			{
				return index;
			}

			index = spawnIndexes.Count;
			spawnIndexes.Add(ownerClientId, index);
			return index;
		}
	}
}
