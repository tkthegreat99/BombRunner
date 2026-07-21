using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace BombRunner.Scripts.Localization
{
	// Resources localization json을 읽고 키 기반 문자열을 제공하는 서비스.
	public sealed class LocalizationService
	{
		private const string DefaultLanguageCode = "en";
		private const string SecondaryFallbackLanguageCode = "ko";
		private const string ResourceFolder = "Localization";

		private readonly Dictionary<string, string> activeEntries = new();
		private readonly Dictionary<string, string> defaultEntries = new();
		private readonly Dictionary<string, string> secondaryFallbackEntries = new();
		private string currentLanguageCode = DefaultLanguageCode;

		public string CurrentLanguageCode => currentLanguageCode;

		public LocalizationService()
		{
			LoadLanguageInto(DefaultLanguageCode, defaultEntries);
			LoadLanguageInto(SecondaryFallbackLanguageCode, secondaryFallbackEntries);
			SetLanguage(DefaultLanguageCode);
		}

		public void SetLanguage(string languageCode)
		{
			var requestedLanguageCode = NormalizeLanguageCode(languageCode);
			activeEntries.Clear();

			if (!LoadLanguageInto(requestedLanguageCode, activeEntries))
			{
				requestedLanguageCode = DefaultLanguageCode;
				CopyEntries(defaultEntries, activeEntries);
			}

			currentLanguageCode = requestedLanguageCode;
		}

		public string Get(string key, params object[] args)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return string.Empty;
			}

			var template = ResolveTemplate(key);

			if (args == null || args.Length == 0)
			{
				return template;
			}

			try
			{
				return string.Format(CultureInfo.CurrentCulture, template, args);
			}
			catch (FormatException exception)
			{
				Debug.LogWarning($"Localization format failed. key={key}, language={currentLanguageCode}, message={exception.Message}");
				return template;
			}
		}

		private string ResolveTemplate(string key)
		{
			if (activeEntries.TryGetValue(key, out var activeValue))
			{
				return activeValue;
			}

			if (defaultEntries.TryGetValue(key, out var defaultValue))
			{
				return defaultValue;
			}

			if (secondaryFallbackEntries.TryGetValue(key, out var fallbackValue))
			{
				return fallbackValue;
			}

			Debug.LogWarning($"Localization key missing. key={key}, language={currentLanguageCode}");
			return key;
		}

		private static string NormalizeLanguageCode(string languageCode)
		{
			if (string.IsNullOrWhiteSpace(languageCode))
			{
				return DefaultLanguageCode;
			}

			var normalizedCode = languageCode.Trim().ToLowerInvariant();

			if (normalizedCode == DefaultLanguageCode || normalizedCode == SecondaryFallbackLanguageCode)
			{
				return normalizedCode;
			}

			Debug.LogWarning($"Unsupported language code. language={languageCode}, fallback={DefaultLanguageCode}");
			return DefaultLanguageCode;
		}

		private static bool LoadLanguageInto(string languageCode, Dictionary<string, string> target)
		{
			target.Clear();

			var textAsset = Resources.Load<TextAsset>($"{ResourceFolder}/{languageCode}");

			if (textAsset == null)
			{
				Debug.LogWarning($"Localization resource missing. path=Resources/{ResourceFolder}/{languageCode}.json");
				return false;
			}

			LocalizationTable table;

			try
			{
				table = JsonUtility.FromJson<LocalizationTable>(textAsset.text);
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"Localization resource parse failed. language={languageCode}, message={exception.Message}");
				return false;
			}

			if (table == null || table.entries == null)
			{
				Debug.LogWarning($"Localization resource has no entries. language={languageCode}");
				return false;
			}

			for (var i = 0; i < table.entries.Length; i++)
			{
				var entry = table.entries[i];

				if (entry == null || string.IsNullOrWhiteSpace(entry.key))
				{
					continue;
				}

				target[entry.key] = entry.value ?? string.Empty;
			}

			return target.Count > 0;
		}

		private static void CopyEntries(Dictionary<string, string> source, Dictionary<string, string> target)
		{
			foreach (var pair in source)
			{
				target[pair.Key] = pair.Value;
			}
		}

		[Serializable]
		private sealed class LocalizationTable
		{
			// Unity JsonUtility가 읽는 localization entry 배열.
			public LocalizationEntry[] entries;
		}

		[Serializable]
		// 단일 localization key/value 레코드.
		private sealed class LocalizationEntry
		{
			public string key;
			public string value;
		}
	}
}
