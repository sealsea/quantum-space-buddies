﻿using OWML.Common;
using QSB.DeathSync;
using QSB.Events;
using QSB.TimeSync.Events;
using QSB.Utility;
using QuantumUNET;
using UnityEngine;

namespace QSB.TimeSync
{
	public class WakeUpSync : QNetworkBehaviour
	{
		public static WakeUpSync LocalInstance { get; private set; }

		private const float PauseOrFastForwardThreshold = 0.5f;
		private const float ChangeTimescaleThreshold = 0.1f;
		private const float TimescaleBounds = 0.3f;

		private const float MaxFastForwardSpeed = 60f;
		private const float MaxFastForwardDiff = 20f;
		private const float MinFastForwardSpeed = 2f;

		private enum State { NotLoaded, Loaded, FastForwarding, Pausing }

		private State _state = State.NotLoaded;

		private float _sendTimer;
		private float _serverTime;
		private float _timeScale;
		private bool _isFirstFastForward = true;
		private int _localLoopCount;
		private int _serverLoopCount;

		public override void OnStartLocalPlayer() => LocalInstance = this;

		public void Start()
		{
			if (!IsLocalPlayer)
			{
				return;
			}

			if (QSBSceneManager.IsInUniverse)
			{
				_isFirstFastForward = false;
				Init();
			}
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;

			GlobalMessenger.AddListener(EventNames.RestartTimeLoop, OnLoopStart);
			GlobalMessenger.AddListener(EventNames.WakeUp, OnWakeUp);
		}

		public float GetTimeDifference()
		{
			var myTime = Time.timeSinceLevelLoad;
			return myTime - _serverTime;
		}

		private void OnWakeUp()
		{
			DebugLog.DebugWrite($"OnWakeUp", MessageType.Info);
			if (QNetworkServer.active)
			{
				QSBCore.HasWokenUp = true;
				RespawnOnDeath.Instance.Init();
			}
		}

		public void OnDestroy()
		{
			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
			GlobalMessenger.RemoveListener(EventNames.RestartTimeLoop, OnLoopStart);
			GlobalMessenger.RemoveListener(EventNames.WakeUp, OnWakeUp);
		}

		private void OnSceneLoaded(OWScene scene, bool isInUniverse)
		{
			if (isInUniverse)
			{
				Init();
			}
			else
			{
				_state = State.NotLoaded;
			}
		}

		private void OnLoopStart() => _localLoopCount++;

		private void Init()
		{
			QSBEventManager.FireEvent(EventNames.QSBPlayerStatesRequest);
			_state = State.Loaded;
			gameObject.AddComponent<PreserveTimeScale>();
			if (IsServer)
			{
				SendServerTime();
			}
			else
			{
				WakeUpOrSleep();
			}
		}

		private void SendServerTime() => QSBEventManager.FireEvent(EventNames.QSBServerTime, _serverTime, _localLoopCount);

		public void OnClientReceiveMessage(ServerTimeMessage message)
		{
			_serverTime = message.ServerTime;
			_serverLoopCount = message.LoopCount;
		}

		private void WakeUpOrSleep()
		{
			if (_state == State.NotLoaded || _localLoopCount != _serverLoopCount)
			{
				return;
			}

			var myTime = Time.timeSinceLevelLoad;
			var diff = myTime - _serverTime;

			if (diff > PauseOrFastForwardThreshold)
			{
				StartPausing();
				return;
			}

			if (diff < -PauseOrFastForwardThreshold)
			{
				StartFastForwarding();
			}
		}

		private void StartFastForwarding()
		{
			if (_state == State.FastForwarding)
			{
				TimeSyncUI.TargetTime = _serverTime;
				return;
			}
			DebugLog.DebugWrite($"START FASTFORWARD (Target:{_serverTime} Current:{Time.timeSinceLevelLoad})", MessageType.Info);
			if (Locator.GetActiveCamera() != null)
			{
				Locator.GetActiveCamera().enabled = false;
			}
			_timeScale = MaxFastForwardSpeed;
			_state = State.FastForwarding;
			OWTime.SetMaxDeltaTime(0.033333335f);
			OWTime.SetFixedTimestep(0.033333335f);
			TimeSyncUI.TargetTime = _serverTime;
			TimeSyncUI.Start(TimeSyncType.Fastforwarding);
		}

		private void StartPausing()
		{
			if (_state == State.Pausing)
			{
				return;
			}
			DebugLog.DebugWrite($"START PAUSING (Target:{_serverTime} Current:{Time.timeSinceLevelLoad})", MessageType.Info);
			Locator.GetActiveCamera().enabled = false;
			_timeScale = 0f;
			_state = State.Pausing;
			SpinnerUI.Show();
			TimeSyncUI.Start(TimeSyncType.Pausing);
		}

		private void ResetTimeScale()
		{
			_timeScale = 1f;
			OWTime.SetMaxDeltaTime(0.06666667f);
			OWTime.SetFixedTimestep(0.01666667f);
			Locator.GetActiveCamera().enabled = true;
			_state = State.Loaded;

			DebugLog.DebugWrite($"RESET TIMESCALE", MessageType.Info);
			_isFirstFastForward = false;
			QSBCore.HasWokenUp = true;
			Physics.SyncTransforms();
			SpinnerUI.Hide();
			TimeSyncUI.Stop();
			QSBEventManager.FireEvent(EventNames.QSBPlayerStatesRequest);
			RespawnOnDeath.Instance.Init();
		}

		public void Update()
		{
			if (IsServer)
			{
				UpdateServer();
			}
			else if (IsLocalPlayer)
			{
				UpdateLocal();
			}
		}

		private void UpdateServer()
		{
			_serverTime = Time.timeSinceLevelLoad;
			if (_state != State.Loaded)
			{
				return;
			}

			_sendTimer += Time.unscaledDeltaTime;
			if (_sendTimer > 1)
			{
				SendServerTime();
				_sendTimer = 0;
			}
		}

		private void UpdateLocal()
		{
			_serverTime += Time.unscaledDeltaTime;

			if (_state == State.NotLoaded)
			{
				return;
			}

			if (_state == State.FastForwarding)
			{
				if (Locator.GetPlayerCamera() != null && !Locator.GetPlayerCamera().enabled)
				{
					Locator.GetPlayerCamera().enabled = false;
				}
				var diff = _serverTime - Time.timeSinceLevelLoad;
				Time.timeScale = Mathf.SmoothStep(MinFastForwardSpeed, MaxFastForwardSpeed, Mathf.Abs(diff) / MaxFastForwardDiff);

				if (QSBSceneManager.CurrentScene == OWScene.SolarSystem && _isFirstFastForward)
				{
					var spawnPoint = Locator.GetPlayerBody().GetComponent<PlayerSpawner>().GetInitialSpawnPoint().transform;
					Locator.GetPlayerTransform().position = spawnPoint.position;
					Locator.GetPlayerTransform().rotation = spawnPoint.rotation;
					Physics.SyncTransforms();
				}
			}
			else
			{
				Time.timeScale = _timeScale;
			}

			var isDoneFastForwarding = _state == State.FastForwarding && Time.timeSinceLevelLoad >= _serverTime;
			var isDonePausing = _state == State.Pausing && Time.timeSinceLevelLoad < _serverTime;

			if (isDoneFastForwarding || isDonePausing)
			{
				ResetTimeScale();
			}

			if (_state == State.Loaded)
			{
				CheckTimeDifference();
			}
		}

		private void CheckTimeDifference()
		{
			var diff = GetTimeDifference();

			if (diff > PauseOrFastForwardThreshold || diff < -PauseOrFastForwardThreshold)
			{
				WakeUpOrSleep();
			}

			if (diff < ChangeTimescaleThreshold && diff > -ChangeTimescaleThreshold)
			{
				return;
			}

			var mappedTimescale = diff.Map(-PauseOrFastForwardThreshold, PauseOrFastForwardThreshold, 1 + TimescaleBounds, 1 - TimescaleBounds);
			OWTime.SetTimeScale(mappedTimescale);
		}
	}
}