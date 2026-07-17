using System;
using UnityEngine;
using VContainer.Unity;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
using Steamworks;
#endif

namespace BombRunner.Scripts.Multiplayer
{
	public sealed class SteamworksClientService : ISteamworksClientService, IStartable, ITickable, IDisposable
	{
		public bool IsInitialized { get; private set; }
		public ulong LocalSteamId { get; private set; }
		public string PersonaName { get; private set; } = string.Empty;

		public void Start()
		{
			Initialize();
		}

		public void Tick()
		{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			if (IsInitialized)
			{
				SteamAPI.RunCallbacks();
			}
#endif
		}

		public void Dispose()
		{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			if (IsInitialized)
			{
				SteamAPI.Shutdown();
			}
#endif

			IsInitialized = false;
			LocalSteamId = 0;
			PersonaName = string.Empty;
		}

		private void Initialize()
		{
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX
			if (IsInitialized)
			{
				return;
			}

			try
			{
				if (!Packsize.Test() || !DllCheck.Test())
				{
					Debug.LogError("Steamworks.NET binary check failed.");
					return;
				}

				IsInitialized = SteamAPI.Init();

				if (!IsInitialized)
				{
					Debug.LogWarning("SteamAPI.Init failed. Steam lobby flow will use local fallback.");
					return;
				}

				var steamId = SteamUser.GetSteamID();
				LocalSteamId = steamId.m_SteamID;
				PersonaName = SteamFriends.GetPersonaName();
				Debug.Log($"Steam initialized: {PersonaName} ({LocalSteamId})");
			}
			catch (Exception exception)
			{
				IsInitialized = false;
				Debug.LogWarning($"Steam initialization failed: {exception.Message}");
			}
#else
			Debug.LogWarning("Steamworks is disabled on this platform. Steam lobby flow will use local fallback.");
#endif
		}
	}
}
