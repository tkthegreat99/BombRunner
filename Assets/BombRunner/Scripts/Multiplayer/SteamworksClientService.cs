using System;
using Steamworks;
using UnityEngine;
using VContainer.Unity;

namespace BombRunner.Scripts.Multiplayer
{
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
				return;
			}

			try
			{
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
				SteamClient.RunCallbacks();
			}
		}

		public void Dispose()
		{
			if (SteamClient.IsValid)
			{
				SteamClient.Shutdown();
			}
		}
	}
}
