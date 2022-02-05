using QSB.ClientServerStateSync.Messages;
using QSB.Messaging;
using QSB.Player;
using QSB.Player.TransformSync;
using QSB.Utility;
using UnityEngine;

namespace QSB.ClientServerStateSync
{
	internal class ClientStateManager : MonoBehaviour
	{
		public static ClientStateManager Instance { get; private set; }

		public event ChangeStateEvent OnChangeState;
		public delegate void ChangeStateEvent(ClientState newState);

		private void Awake()
			=> Instance = this;

		private void Start()
		{
			QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
			Delay.RunWhen(() => PlayerTransformSync.LocalInstance != null,
				() => new ClientStateMessage(ForceGetCurrentState()).Send());
		}

		private void OnDestroy() =>
			QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;

		public void ChangeClientState(ClientState newState)
		{
			if (PlayerTransformSync.LocalInstance == null || QSBPlayerManager.LocalPlayer.State == newState)
			{
				return;
			}

			QSBPlayerManager.LocalPlayer.State = newState;
			OnChangeState?.Invoke(newState);
		}

		private void OnSceneLoaded(QSBScene oldScene, QSBScene newScene, bool inUniverse)
		{
			var serverState = ServerStateManager.Instance.GetServerState();

			ClientState newState;

			if (QSBCore.IsHost)
			{

				switch (newScene)
				{
					case QSBScene.TitleScreen:
						newState = ClientState.InTitleScreen;
						break;
					case QSBScene.Credits_Fast:
						newState = ClientState.WatchingShortCredits;
						break;
					case QSBScene.Credits_Final:
					case QSBScene.PostCreditsScene:
						newState = ClientState.WatchingLongCredits;
						break;
					case QSBScene.SolarSystem:
						if (oldScene == QSBScene.SolarSystem)
						{
							// reloading scene
							newState = ClientState.WaitingForOthersToBeReady;
						}
						else
						{
							// loading in from title screen
							newState = ClientState.AliveInSolarSystem;
						}

						break;
					case QSBScene.EyeOfTheUniverse:
						newState = ClientState.AliveInEye;
						break;
					default:
						newState = ClientState.NotLoaded;
						break;
				}
			}
			else
			{
				switch (newScene)
				{
					case QSBScene.TitleScreen:
						newState = ClientState.InTitleScreen;
						break;
					case QSBScene.Credits_Fast:
						newState = ClientState.WatchingShortCredits;
						break;
					case QSBScene.Credits_Final:
					case QSBScene.PostCreditsScene:
						newState = ClientState.WatchingLongCredits;
						break;
					case QSBScene.SolarSystem:
						if (serverState == ServerState.WaitingForAllPlayersToDie)
						{
							newState = ClientState.WaitingForOthersToBeReady;
							break;
						}

						if (oldScene == QSBScene.SolarSystem)
						{
							// reloading scene
							newState = ClientState.WaitingForOthersToBeReady;
						}
						else
						{
							// loading in from title screen
							if (serverState == ServerState.WaitingForAllPlayersToReady)
							{
								newState = ClientState.WaitingForOthersToBeReady;
							}
							else
							{
								newState = ClientState.AliveInSolarSystem;
							}
						}

						break;
					case QSBScene.EyeOfTheUniverse:
						if (serverState == ServerState.WaitingForAllPlayersToReady)
						{
							newState = ClientState.WaitingForOthersToBeReady;
						}
						else
						{
							newState = ClientState.AliveInEye;
						}

						break;
					default:
						newState = ClientState.NotLoaded;
						break;
				}
			}

			new ClientStateMessage(newState).Send();
		}

		public void OnDeath()
		{
			var currentScene = QSBSceneManager.CurrentScene;
			if (currentScene == QSBScene.SolarSystem)
			{
				new ClientStateMessage(ClientState.DeadInSolarSystem).Send();
			}
			else if (currentScene == QSBScene.EyeOfTheUniverse)
			{
				DebugLog.ToConsole($"Error - You died in the Eye? HOW DID YOU DO THAT?!", OWML.Common.MessageType.Error);
			}
			else
			{
				// whaaaaaaaaa
				DebugLog.ToConsole($"Error - You died... in a menu? In the credits? In any case, you should never see this. :P", OWML.Common.MessageType.Error);
			}
		}

		public void OnRespawn()
		{
			var currentScene = QSBSceneManager.CurrentScene;
			if (currentScene == QSBScene.SolarSystem)
			{
				DebugLog.DebugWrite($"RESPAWN!");
				new ClientStateMessage(ClientState.AliveInSolarSystem).Send();
			}
			else
			{
				DebugLog.ToConsole($"Error - Player tried to respawn in scene {currentScene}", OWML.Common.MessageType.Error);
			}
		}

		private static ClientState ForceGetCurrentState()
			=> QSBSceneManager.CurrentScene switch
			{
				QSBScene.TitleScreen => ClientState.InTitleScreen,
				QSBScene.Credits_Fast => ClientState.WatchingShortCredits,
				QSBScene.Credits_Final or QSBScene.PostCreditsScene => ClientState.WatchingLongCredits,
				QSBScene.SolarSystem => ClientState.AliveInSolarSystem,
				QSBScene.EyeOfTheUniverse => ClientState.AliveInEye,
				_ => ClientState.NotLoaded
			};
	}
}