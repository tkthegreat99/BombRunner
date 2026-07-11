using System;
using System.Threading;
using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	public sealed class LocalMatchFlowService : IDisposable
	{
		private readonly BombSpawnService bombSpawnService;
		private readonly BombTargetService bombTargetService;
		private readonly GameBalanceSettings balanceSettings;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private PlayerStateController[] players;
		private GameObject cameraFocusTarget;
		private bool isInitialized;
		private bool isMatchEnded;
		private bool isSpawningBomb;

		public LocalMatchFlowService(
			BombSpawnService bombSpawnService,
			BombTargetService bombTargetService,
			GameBalanceSettings balanceSettings,
			LocalPlayerCameraFollow cameraFollow)
		{
			this.bombSpawnService = bombSpawnService;
			this.bombTargetService = bombTargetService;
			this.balanceSettings = balanceSettings;
			this.cameraFollow = cameraFollow;
		}

		public void Initialize(PlayerStateController[] players)
		{
			this.players = players;
			isInitialized = players != null && players.Length > 0;
			isMatchEnded = false;
			isSpawningBomb = false;

			if (!isInitialized)
			{
				return;
			}

			bombTargetService.TrySetAnyAliveTarget(null);
			RunBombSpawnSequenceAsync(null, cancellationTokenSource.Token).Forget();
		}

		public void Dispose()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}

		private void OnBombExploded(BombCreatureController controller, PlayerStateController downedPlayer)
		{
			if (!isInitialized || isMatchEnded)
			{
				return;
			}

			if (controller != null)
			{
				controller.Exploded -= OnBombExploded;
			}

			var aliveCount = CountAlivePlayers();

			if (aliveCount <= 1)
			{
				EndMatch(controller);
				return;
			}

			bombTargetService.TrySetAnyAliveTarget(downedPlayer);
			RunBombSpawnSequenceAsync(controller, cancellationTokenSource.Token).Forget();
		}

		private async UniTaskVoid RunBombSpawnSequenceAsync(
			BombCreatureController previousController,
			CancellationToken cancellationToken)
		{
			if (isSpawningBomb || isMatchEnded)
			{
				return;
			}

			isSpawningBomb = true;
			SetPlayersInputEnabled(false);

			var spawnPosition = bombSpawnService.GetLocalBombSpawnPosition(players);
			FocusCameraOnSpawn(spawnPosition);
			bombSpawnService.ShowLocalSpawnCue(players);

			try
			{
				await DelaySecondsAsync(balanceSettings.BombSpawnCameraFocusSeconds, cancellationToken);

				bombSpawnService.DespawnLocalBomb(previousController);
				var dropStartPosition = spawnPosition + Vector3.up * balanceSettings.BombDropHeight;
				var controller = bombSpawnService.SpawnLocalBomb(players, dropStartPosition);

				if (controller == null)
				{
					return;
				}

				controller.Exploded += OnBombExploded;
				await DropBombAsync(controller.transform, dropStartPosition, spawnPosition, cancellationToken);
				await RunStartCountdownAsync(spawnPosition, cancellationToken);
				controller.Activate();
				Debug.Log("Local match flow: bomb activated");
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				return;
			}
			finally
			{
				ClearCameraFocus();

				if (!isMatchEnded)
				{
					cameraFollow.SetTarget(GetCameraReturnTarget(), false);
					SetPlayersInputEnabled(true);
				}

				isSpawningBomb = false;
			}
		}

		private async UniTask DropBombAsync(
			Transform bombTransform,
			Vector3 startPosition,
			Vector3 endPosition,
			CancellationToken cancellationToken)
		{
			var duration = Mathf.Max(0.01f, balanceSettings.BombDropDurationSeconds);
			var elapsedTime = 0f;

			while (elapsedTime < duration)
			{
				cancellationToken.ThrowIfCancellationRequested();
				elapsedTime += Time.deltaTime;
				var t = Mathf.Clamp01(elapsedTime / duration);
				var eased = t * t * (3f - 2f * t);
				bombTransform.position = Vector3.Lerp(startPosition, endPosition, eased);
				await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
			}

			bombTransform.position = endPosition;
		}

		private async UniTask RunStartCountdownAsync(Vector3 spawnPosition, CancellationToken cancellationToken)
		{
			var countdownTextObject = CreateCountdownText(spawnPosition);
			var countdownText = countdownTextObject.GetComponent<TextMesh>();
			var countdown = Mathf.CeilToInt(balanceSettings.BombStartCountdownSeconds);

			try
			{
				for (var i = countdown; i > 0; i--)
				{
					cancellationToken.ThrowIfCancellationRequested();
					countdownText.text = i.ToString();
					FaceCamera(countdownTextObject.transform);
					Debug.Log($"Bomb starts in {i}");
					await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
				}
			}
			finally
			{
				UnityEngine.Object.Destroy(countdownTextObject);
			}
		}

		private async UniTask DelaySecondsAsync(float seconds, CancellationToken cancellationToken)
		{
			var delay = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
			await UniTask.Delay(delay, cancellationToken: cancellationToken);
		}

		private void FocusCameraOnSpawn(Vector3 spawnPosition)
		{
			ClearCameraFocus();
			cameraFocusTarget = new GameObject("Temporary Bomb Spawn Camera Focus");
			cameraFocusTarget.transform.position = spawnPosition;
			cameraFollow.SetTarget(cameraFocusTarget.transform, false);
			Debug.Log("Local match flow: camera focused on next bomb spawn");
		}

		private GameObject CreateCountdownText(Vector3 spawnPosition)
		{
			var countdownTextObject = new GameObject("Temporary Bomb Start Countdown");
			countdownTextObject.transform.position = spawnPosition + Vector3.up * 2.2f;
			var textMesh = countdownTextObject.AddComponent<TextMesh>();
			textMesh.anchor = TextAnchor.MiddleCenter;
			textMesh.alignment = TextAlignment.Center;
			textMesh.characterSize = 1.2f;
			textMesh.fontSize = 96;
			textMesh.color = new Color(1f, 0.85f, 0.12f, 1f);
			FaceCamera(countdownTextObject.transform);
			return countdownTextObject;
		}

		private void FaceCamera(Transform target)
		{
			var cameraTransform = cameraFollow.transform;
			var direction = target.position - cameraTransform.position;

			if (direction.sqrMagnitude <= 0.0001f)
			{
				return;
			}

			target.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		}

		private void ClearCameraFocus()
		{
			if (cameraFocusTarget == null)
			{
				return;
			}

			UnityEngine.Object.Destroy(cameraFocusTarget);
			cameraFocusTarget = null;
		}

		private Transform GetCameraReturnTarget()
		{
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player != null && player.PlayerLabel == "Local Player")
				{
					return player.transform;
				}
			}

			return players[0] != null ? players[0].transform : null;
		}

		private void SetPlayersInputEnabled(bool isInputEnabled)
		{
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null)
				{
					continue;
				}

				var shouldEnableInput = isInputEnabled && player.PlayerLabel == "Local Player";

				if (player.TryGetComponent<PlayerMovementController>(out var movementController))
				{
					movementController.SetInputEnabled(shouldEnableInput);
				}

				if (player.TryGetComponent<PlayerDashController>(out var dashController))
				{
					dashController.SetInputEnabled(shouldEnableInput);
				}
			}
		}

		private int CountAlivePlayers()
		{
			var aliveCount = 0;

			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player == null || !player.IsAlive)
				{
					continue;
				}

				aliveCount++;
			}

			return aliveCount;
		}

		private void EndMatch(BombCreatureController controller)
		{
			isMatchEnded = true;
			SetPlayersInputEnabled(false);
			bombSpawnService.DespawnLocalBomb(controller);
			bombTargetService.ClearTarget();
			ClearCameraFocus();
			cameraFollow.SetTarget(GetCameraReturnTarget(), false);

			var winner = GetWinner();
			var winnerLabel = winner != null ? winner.PlayerLabel : "None";
			Debug.Log($"Local match flow: match ended. Winner: {winnerLabel}");
		}

		private PlayerStateController GetWinner()
		{
			for (var i = 0; i < players.Length; i++)
			{
				var player = players[i];

				if (player != null && player.IsAlive)
				{
					return player;
				}
			}

			return null;
		}
	}
}
