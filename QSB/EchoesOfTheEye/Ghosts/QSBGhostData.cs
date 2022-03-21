using QSB.EchoesOfTheEye.Ghosts.WorldObjects;
using QSB.Player;
using QSB.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.EchoesOfTheEye.Ghosts;

public class QSBGhostData
{
	public Dictionary<PlayerInfo, GhostLocationData> playerLocation = new();
	public Dictionary<PlayerInfo, GhostLocationData> lastKnownPlayerLocation = new();
	public Dictionary<PlayerInfo, GhostSensorData> sensors = new();
	public Dictionary<PlayerInfo, GhostSensorData> lastKnownSensors = new();
	private GhostSensorData firstUnknownSensor = new GhostSensorData();
	public GhostData.ThreatAwareness threatAwareness;
	public GhostAction.Name currentAction = GhostAction.Name.None;
	public GhostAction.Name previousAction = GhostAction.Name.None;
	public bool isAlive = true;
	public bool hasWokenUp;
	public Dictionary<PlayerInfo, bool> isPlayerLocationKnown = new();
	public Dictionary<PlayerInfo, bool> wasPlayerLocationKnown;
	public bool reduceGuardUtility;
	public bool fastStalkUnlocked;
	public float timeLastSawPlayer;
	public Dictionary<PlayerInfo, float> timeSincePlayerLocationKnown = QSBPlayerManager.PlayerList.IntoDict(float.PositiveInfinity);
	public float playerMinLanternRange;
	public float illuminatedByPlayerMeter;
	public bool reducedFrights_allowChase;

	public bool LostPlayerDueToOcclusion(PlayerInfo player)
		=> !isPlayerLocationKnown[player] && !lastKnownSensors[player].isPlayerOccluded && firstUnknownSensor.isPlayerOccluded;

	public PlayerInfo InterestedPlayer
	{
		get
		{
			var playersIlluminatingMe = sensors.Where(x => x.Value.isIlluminatedByPlayer);
			if (playersIlluminatingMe.Count() == 1)
			{
				return playersIlluminatingMe.First().Key;
			}

			var visiblePlayers = sensors.Where(x => x.Value.isPlayerVisible || x.Value.isPlayerHeldLanternVisible);

			if (visiblePlayers.Count() == 0)
			{
				return playerLocation.MinBy(x => x.Value.distance).Key;
			}

			return visiblePlayers.MinBy(x => playerLocation[x.Key].distance).Key;
		}
	}

	public void TabulaRasa()
	{
		threatAwareness = GhostData.ThreatAwareness.EverythingIsNormal;
		isPlayerLocationKnown = QSBPlayerManager.PlayerList.IntoDict(false);
		wasPlayerLocationKnown = isPlayerLocationKnown;
		reduceGuardUtility = false;
		fastStalkUnlocked = false;
		timeLastSawPlayer = 0f;
		timeSincePlayerLocationKnown = QSBPlayerManager.PlayerList.IntoDict(float.PositiveInfinity);
		playerMinLanternRange = 0f;
		illuminatedByPlayerMeter = 0f;
	}

	public void OnPlayerExitDreamWorld()
	{
		isPlayerLocationKnown[QSBPlayerManager.LocalPlayer] = false;
		wasPlayerLocationKnown[QSBPlayerManager.LocalPlayer] = false;
		reduceGuardUtility = false;
		fastStalkUnlocked = false;
		timeSincePlayerLocationKnown[QSBPlayerManager.LocalPlayer] = float.PositiveInfinity;
	}

	public void OnEnterAction(GhostAction.Name actionName)
	{
		if (actionName == GhostAction.Name.IdentifyIntruder || actionName - GhostAction.Name.Chase <= 2)
		{
			reduceGuardUtility = true;
		}
	}

	public void FixedUpdate_Data(GhostController controller)
	{
		wasPlayerLocationKnown = isPlayerLocationKnown;

		foreach (var player in QSBPlayerManager.PlayerList)
		{
			isPlayerLocationKnown[player] = sensors[player].isPlayerVisible || sensors[player].isPlayerHeldLanternVisible || sensors[player].isIlluminatedByPlayer || sensors[player].inContactWithPlayer;
		}

		if (!reduceGuardUtility && sensors.Any(x => x.Value.isIlluminatedByPlayer))
		{
			reduceGuardUtility = true;
		}

		foreach (var player in QSBPlayerManager.PlayerList)
		{
			if (!playerLocation.ContainsKey(player))
			{
				playerLocation.Add(player, new());
				lastKnownPlayerLocation.Add(player, new());
				timeSincePlayerLocationKnown.Add(player, float.PositiveInfinity);
			}

			var worldPosition = player.Body.transform.position - player.Body.transform.up;
			var worldVelocity = Vector3.zero; // TODO - get velocity
			playerLocation[player].Update(worldPosition, worldVelocity, controller);
			playerMinLanternRange = 0f; // TODO - get range

			if (isPlayerLocationKnown[player])
			{
				lastKnownPlayerLocation[player].CopyFromOther(playerLocation[player]);
				lastKnownSensors[player].CopyFromOther(sensors[player]);
				timeLastSawPlayer = Time.time;
				timeSincePlayerLocationKnown[player] = 0f;
			}
			else
			{
				if (wasPlayerLocationKnown[player])
				{
					firstUnknownSensor.CopyFromOther(sensors[player]); // TODO - ???
				}

				lastKnownPlayerLocation[player].Update(controller);
				timeSincePlayerLocationKnown[player] += Time.deltaTime;
			}
		}

		if (threatAwareness >= GhostData.ThreatAwareness.IntruderConfirmed && sensors.Any(x => x.Value.isIlluminatedByPlayer) && !PlayerData.GetReducedFrights())
		{
			illuminatedByPlayerMeter += Time.deltaTime;
			return;
		}

		illuminatedByPlayerMeter = Mathf.Max(0f, illuminatedByPlayerMeter - (Time.deltaTime * 0.5f));
	}
}
