using System;
using System.Threading;
using BombRunner.Scripts.Bomb;
using BombRunner.Scripts.Camera;
using BombRunner.Scripts.Data;
using BombRunner.Scripts.Gameplay.Authority;
using BombRunner.Scripts.Gameplay.Player;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace BombRunner.Scripts.Gameplay.Match
{
	// 로컬 프로토타입 매치의 폭탄 루프와 승리 종료를 관리하는 서비스.
	public sealed class LocalMatchFlowService : IDisposable
	{
		private readonly BombSpawnService bombSpawnService;
		private readonly IMatchAuthorityService matchAuthorityService;
		private readonly GameBalanceSettings balanceSettings;
		private readonly LocalPlayerCameraFollow cameraFollow;
		private readonly LocalMatchFeedbackView matchFeedbackView;
		private readonly LocalBombSpawnCameraFocusView bombSpawnCameraFocusView;
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private PlayerStateController[] players;
		private bool isInitialized;
		private bool isMatchEnded;
		private bool isSpawningBomb;

		public LocalMatchFlowService(
			BombSpawnService bombSpawnService,
			IMatchAuthorityService matchAuthorityService,
			GameBalanceSettings balanceSettings,
			LocalPlayerCameraFollow cameraFollow,
			LocalMatchFeedbackView matchFeedbackView,
			LocalBombSpawnCameraFocusView bombSpawnCameraFocusView)
		{
			this.bombSpawnService = bombSpawnService;
			this.matchAuthorityService = matchAuthorityService;
			this.balanceSettings = balanceSettings;
			this.cameraFollow = cameraFollow;
			this.matchFeedbackView = matchFeedbackView;
			this.bombSpawnCameraFocusView = bombSpawnCameraFocusView;
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

			matchAuthorityService.TrySetAnyAliveBombTarget(players, null);
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

			ShowExplosionDecision(controller, downedPlayer);

			var aliveCount = CountAlivePlayers();

			if (aliveCount <= 1)
			{
				EndMatch(controller);
				return;
			}

			matchAuthorityService.TrySetAnyAliveBombTarget(players, downedPlayer);
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
			matchFeedbackView.ShowSpawnCue(
				spawnPosition,
				balanceSettings.SpawnCueRadius,
				balanceSettings.SpawnCueHeight,
				balanceSettings.SpawnCueDurationSeconds);

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
				if (!isMatchEnded)
				{
					cameraFollow.SetTarget(GetCameraReturnTarget(), true);
					SetPlayersInputEnabled(true);
				}

				isSpawningBomb = false;
			}
		}

		private void ShowExplosionDecision(BombCreatureController controller, PlayerStateController downedPlayer)
		{
			if (controller == null)
			{
				return;
			}

			matchFeedbackView.ShowExplosionDecision(
				controller.transform.position,
				balanceSettings.ExplosionRadius,
				balanceSettings.ExplosionRingHeight,
				balanceSettings.ExplosionRingExpandDurationSeconds,
				balanceSettings.ExplosionRingHoldSeconds,
				downedPlayer != null ? downedPlayer.transform : null);
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
			var countdown = Mathf.CeilToInt(balanceSettings.BombStartCountdownSeconds);
			await matchFeedbackView.ShowBombStartCountdownAsync(
				spawnPosition,
				countdown,
				cameraFollow.transform,
				cancellationToken);
		}

		private async UniTask DelaySecondsAsync(float seconds, CancellationToken cancellationToken)
		{
			var delay = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
			await UniTask.Delay(delay, cancellationToken: cancellationToken);
		}

		private void FocusCameraOnSpawn(Vector3 spawnPosition)
		{
			bombSpawnCameraFocusView.SetPosition(spawnPosition);
			cameraFollow.SetTarget(bombSpawnCameraFocusView.Target, false);
			Debug.Log("Local match flow: camera focused on next bomb spawn");
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

				if (player.TryGetComponent<PlayerTauntController>(out var tauntController))
				{
					tauntController.SetInputEnabled(shouldEnableInput);
				}

				if (!shouldEnableInput)
				{
					player.SetTaunting(false);
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
			matchAuthorityService.ClearBombTarget();
			cameraFollow.SetTarget(GetCameraReturnTarget(), true);

			var winner = GetWinner();
			var winnerLabel = winner != null ? winner.PlayerLabel : "None";
			matchFeedbackView.ShowMatchEnded(winnerLabel, GetCameraReturnTarget(), cameraFollow.transform);
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
