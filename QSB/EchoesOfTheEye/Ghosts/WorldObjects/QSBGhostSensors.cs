﻿using QSB.EchoesOfTheEye.Ghosts.Messages;
using QSB.Messaging;
using QSB.Utility;
using QSB.WorldSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.EchoesOfTheEye.Ghosts.WorldObjects;

public class QSBGhostSensors : WorldObject<GhostSensors>, IGhostObject
{
	public override void SendInitialState(uint to)
	{

	}

	public override bool ShouldDisplayDebug() => false;

	public QSBGhostData _data;

	public void Initialize(QSBGhostData data, OWTriggerVolume guardVolume = null)
	{
		_data = data;
		AttachedObject._origEdgeCatcherSize = AttachedObject._contactEdgeCatcherShape.size;
		AttachedObject._guardVolume = guardVolume;
	}

	public bool CanGrabPlayer(GhostPlayer player)
		=> !PlayerState.IsAttached()
			&& player.playerLocation.distanceXZ < 2f + AttachedObject._grabDistanceBuff
			&& player.playerLocation.degreesToPositionXZ < 20f + AttachedObject._grabAngleBuff
			&& AttachedObject._animator.GetFloat("GrabWindow") > 0.5f;

	public void FixedUpdate_Sensors()
	{
		if (_data == null)
		{
			return;
		}

		foreach (var pair in _data.players)
		{
			var player = pair.Value;
			var lanternController = player.player.AssignedSimulationLantern.AttachedObject.GetLanternController();
			var playerLightSensor = Locator.GetPlayerLightSensor();
			player.sensor.isPlayerHoldingLantern = lanternController.IsHeldByPlayer();
			_data.isIlluminated = AttachedObject._lightSensor.IsIlluminated();
			player.sensor.isIlluminatedByPlayer = (lanternController.IsHeldByPlayer() && AttachedObject._lightSensor.IsIlluminatedByLantern(lanternController));
			player.sensor.isPlayerIlluminatedByUs = playerLightSensor.IsIlluminatedByLantern(AttachedObject._lantern);
			player.sensor.isPlayerIlluminated = playerLightSensor.IsIlluminated();
			player.sensor.isPlayerVisible = false;
			player.sensor.isPlayerHeldLanternVisible = false;
			player.sensor.isPlayerDroppedLanternVisible = false;
			player.sensor.isPlayerOccluded = false;

			if ((lanternController.IsHeldByPlayer() && !lanternController.IsConcealed()) || playerLightSensor.IsIlluminated())
			{
				var position = pair.Key.Camera.transform.position;
				if (AttachedObject.CheckPointInVisionCone(position))
				{
					if (AttachedObject.CheckLineOccluded(AttachedObject._sightOrigin.position, position))
					{
						player.sensor.isPlayerOccluded = true;
					}
					else
					{
						player.sensor.isPlayerVisible = playerLightSensor.IsIlluminated();
						player.sensor.isPlayerHeldLanternVisible = (lanternController.IsHeldByPlayer() && !lanternController.IsConcealed());
					}
				}
			}

			if (!lanternController.IsHeldByPlayer() && AttachedObject.CheckPointInVisionCone(lanternController.GetLightPosition()) && !AttachedObject.CheckLineOccluded(AttachedObject._sightOrigin.position, lanternController.GetLightPosition()))
			{
				player.sensor.isPlayerDroppedLanternVisible = true;
			}
		}

		if (!QSBCore.IsHost)
		{
			return;
		}

		var visiblePlayers = _data.players.Values.Where(x => x.sensor.isPlayerVisible || x.sensor.isPlayerHeldLanternVisible || x.sensor.inContactWithPlayer || x.sensor.isPlayerIlluminatedByUs);

		if (visiblePlayers.Count() == 0) // no players visible
		{
			visiblePlayers = _data.players.Values.Where(x => x.sensor.isIlluminatedByPlayer);
		}

		if (visiblePlayers.Count() == 0) // no players lighting us
		{
			return;
		}

		var closest = visiblePlayers.MinBy(x => x.playerLocation.distance);

		if (_data.interestedPlayer != closest)
		{
			DebugLog.DebugWrite($"CHANGE INTERESTED PLAYER!");
			_data.interestedPlayer = closest;
			this.SendMessage(new ChangeInterestedPlayerMessage(closest.player.PlayerId));
		}
	}

	public void OnEnterContactTrigger(GameObject hitObj)
	{
		if (hitObj.CompareTag("PlayerDetector"))
		{
			_data.localPlayer.sensor.inContactWithPlayer = true;
		}
	}

	public void OnExitContactTrigger(GameObject hitObj)
	{
		if (hitObj.CompareTag("PlayerDetector"))
		{
			_data.localPlayer.sensor.inContactWithPlayer = false;
		}
	}
}