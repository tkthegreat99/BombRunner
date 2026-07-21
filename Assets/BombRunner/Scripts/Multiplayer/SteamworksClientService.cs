using System;
using Steamworks;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Multiplayer
{
	// Facepunch Steam client lifetime과 callback pump를 관리하는 전역 서비스.
	// FacepunchTransport도 같은 SteamClient를 사용하므로, 앱 안에서 SteamClient.Init/Shutdown 소유자는 이 서비스 하나로 유지.
	public sealed class SteamworksClientService : ISteamworksClientService, IStartable, ITickable, IDisposable
	{
		private const uint TestAppId = 480;

		public bool IsInitialized => SteamClient.IsValid;
		public ulong LocalSteamId => SteamClient.IsValid ? SteamClient.SteamId.Value : 0;
		public string PersonaName => SteamClient.IsValid ? SteamClient.Name : string.Empty;

		public void Start()
		{
			if (SteamClient.IsValid)
			{
				// Steam이 이미 살아 있는 경우 재초기화하면 Facepunch가 예외를 던지므로 기존 세션 재사용.
				return;
			}

			try
			{
				// 친구 테스트 단계에서는 Spacewar 테스트 App ID 사용. 실제 Steam App ID가 생기면 이 상수 교체.
				SteamClient.Init(TestAppId, false);
				Debug.Log($"Facepunch Steam initialized: {PersonaName} ({LocalSteamId})");
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"Facepunch Steam initialization failed: {exception.Message}");
			}
		}

		public void Tick()
		{
			if (SteamClient.IsValid)
			{
				// Steam invite, lobby member change, transport callback이 메인 스레드에서 처리되도록 매 프레임 pump.
				SteamClient.RunCallbacks();
			}
		}

		public void Dispose()
		{
			if (SteamClient.IsValid)
			{
				// 앱 전역 Steam 세션 소유자이므로 LifetimeScope 종료 시점에 한 번만 Shutdown.
				SteamClient.Shutdown();
			}
		}
	}
}
