#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BombRunner.Scripts.Editor
{
	// Steam 친구 초대와 NGO 이동 동기화를 확인하기 위한 Windows 테스트 빌드 자동화.
	public static class FriendTestBuild
	{
		private const string BuildRoot = "Builds";
		private const string BuildFolderName = "FriendTest_NGO";
		private const string ExecutableName = "BombRunner.exe";

		[MenuItem("BombRunner/Build/Friend Test Windows")]
		public static void BuildWindowsFromMenu()
		{
			// 에디터 메뉴와 CLI가 같은 설정을 쓰도록 단일 빌드 진입점 사용.
			BuildWindows();
		}

		public static void BuildWindows()
		{
			var enabledScenes = GetEnabledScenes();

			if (enabledScenes.Length == 0)
			{
				throw new Exception("Friend test build failed: enabled scenes are missing.");
			}

			var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;

			if (string.IsNullOrEmpty(projectRoot))
			{
				throw new Exception("Friend test build failed: project root is missing.");
			}

			var absoluteBuildRoot = Path.Combine(projectRoot, BuildRoot);
			var absoluteOutputFolder = Path.Combine(absoluteBuildRoot, BuildFolderName);
			var absoluteExecutablePath = Path.Combine(absoluteOutputFolder, ExecutableName);

			// 이전 테스트 산출물과 섞이지 않도록 같은 출력 폴더를 먼저 정리.
			if (Directory.Exists(absoluteOutputFolder))
			{
				Directory.Delete(absoluteOutputFolder, true);
			}

			Directory.CreateDirectory(absoluteOutputFolder);

			var buildPlayerOptions = new BuildPlayerOptions
			{
				scenes = enabledScenes,
				locationPathName = absoluteExecutablePath,
				target = BuildTarget.StandaloneWindows64,
				options = BuildOptions.Development
			};

			var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
			var summary = report.summary;

			if (summary.result != BuildResult.Succeeded)
			{
				throw new Exception($"Friend test build failed: result={summary.result}, errors={summary.totalErrors}");
			}

			CopySteamAppId(absoluteOutputFolder);
			Debug.Log($"Friend test build complete: output={absoluteOutputFolder}, size={summary.totalSize}");
		}

		private static string[] GetEnabledScenes()
		{
			// Build Settings에 등록된 Init/MainMenu/Game 순서를 그대로 사용해 실제 진입 흐름 보존.
			var scenes = EditorBuildSettings.scenes;
			var enabledScenes = new string[scenes.Length];
			var enabledSceneCount = 0;

			for (var i = 0; i < scenes.Length; i++)
			{
				if (!scenes[i].enabled)
				{
					continue;
				}

				enabledScenes[enabledSceneCount] = scenes[i].path;
				enabledSceneCount++;
			}

			Array.Resize(ref enabledScenes, enabledSceneCount);
			return enabledScenes;
		}

		private static void CopySteamAppId(string absoluteOutputFolder)
		{
			// Steam 밖에서 실행해도 Facepunch Steam이 테스트 App ID 480으로 초기화되도록 exe 옆에 배치.
			var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;

			if (string.IsNullOrEmpty(projectRoot))
			{
				Debug.LogWarning("Project root is missing. steam_appid.txt cannot be copied.");
				return;
			}

			var sourcePath = Path.Combine(projectRoot, "steam_appid.txt");

			if (!File.Exists(sourcePath))
			{
				Debug.LogWarning("steam_appid.txt is missing. Steam client initialization may fail outside Steam.");
				return;
			}

			var targetPath = Path.Combine(absoluteOutputFolder, "steam_appid.txt");
			File.Copy(sourcePath, targetPath, true);
		}
	}
}
#endif
