using QSB.ClientServerStateSync.Messages;
using QSB.DeathSync.Messages;
using QSB.Messaging;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.Utility;
using System.Linq;
using UnityEngine;

namespace QSB.ClientServerStateSync
{
	internal class ServerStateManager : MonoBehaviour
	{
		public static ServerStateManager Instance { get; private set; }

		public event ChangeStateEvent OnChangeState;
		public delegate void ChangeStateEvent(ServerState newState);

		private ServerState _currentState;
		private bool _blockNextCheck;

		private void Awake()
			=> Instance = this;

		private void Start()
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
			GlobalMessenger.AddListener("TriggerSupernova", OnTriggerSupernova);

			Delay.RunWhen(() => PlayerTransformSync.LocalInstance != null,
				() => new ServerStateMessage(ForceGetCurrentState()).Send());
		}

		private void OnDestroy()
		{
			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
			GlobalMessenger.RemoveListener("TriggerSupernova", OnTriggerSupernova);
		}

		public void ChangeServerState(ServerState newState)
		{
			if (_currentState == newState)
			{
				return;
			}

			_currentState = newState;
			OnChangeState?.Invoke(newState);
		}

		public ServerState GetServerState()
			=> _currentState;

		private void OnSceneLoaded(QSBScene oldScene, QSBScene newScene, bool inUniverse)
		{
			switch (newScene)
			{
				case QSBScene.Credits_Fast:
				case QSBScene.Credits_Final:
				case QSBScene.PostCreditsScene:
					new ServerStateMessage(ServerState.Credits).Send();
					break;

				case QSBScene.TitleScreen:
					new ServerStateMessage(ServerState.NotLoaded).Send();
					break;

				case QSBScene.SolarSystem:
					if (oldScene == QSBScene.SolarSystem)
					{
						new ServerStateMessage(ServerState.WaitingForAllPlayersToReady).Send();
					}
					else
					{
						new ServerStateMessage(ServerState.InSolarSystem).Send();
					}

					break;

				case QSBScene.EyeOfTheUniverse:
					new ServerStateMessage(ServerState.WaitingForAllPlayersToReady).Send();
					break;

				case QSBScene.None:
				case QSBScene.Undefined:
				default:
					DebugLog.ToConsole($"Warning - newScene is {newScene}!", OWML.Common.MessageType.Warning);
					new ServerStateMessage(ServerState.NotLoaded).Send();
					break;
			}
		}

		private void OnTriggerSupernova()
		{
			if (QSBSceneManager.CurrentScene == QSBScene.SolarSystem)
			{
				new ServerStateMessage(ServerState.WaitingForAllPlayersToDie).Send();
			}
		}

		private ServerState ForceGetCurrentState()
		{
			var currentScene = LoadManager.GetCurrentScene();

			switch (currentScene)
			{
				case OWScene.SolarSystem:
					return ServerState.InSolarSystem;
				case OWScene.EyeOfTheUniverse:
					return ServerState.InEye;
				default:
					return ServerState.NotLoaded;
			}
		}

		private void Update()
		{
			if (!QSBCore.IsHost)
			{
				return;
			}

			if (_blockNextCheck)
			{
				_blockNextCheck = false;
				return;
			}

			if (_currentState == ServerState.WaitingForAllPlayersToReady)
			{
				if (QSBPlayerManager.PlayerList.All(x
					=> x.State is ClientState.WaitingForOthersToBeReady
						or ClientState.AliveInSolarSystem
						or ClientState.AliveInEye))
				{
					DebugLog.DebugWrite($"All ready!!");
					new StartLoopMessage().Send();
					if (QSBSceneManager.CurrentScene == QSBScene.SolarSystem)
					{
						new ServerStateMessage(ServerState.InSolarSystem).Send();
					}
					else if (QSBSceneManager.CurrentScene == QSBScene.EyeOfTheUniverse)
					{
						new ServerStateMessage(ServerState.InEye).Send();
					}
					else
					{
						DebugLog.ToConsole($"Error - All players were ready in non-universe scene!?", OWML.Common.MessageType.Error);
						new ServerStateMessage(ServerState.NotLoaded).Send();
					}

					_blockNextCheck = true;
				}
			}
		}
	}
}