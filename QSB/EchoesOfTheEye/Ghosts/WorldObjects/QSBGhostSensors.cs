using QSB.Player;
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

	private QSBGhostData _data;

	public void Initialize(QSBGhostData data, OWTriggerVolume guardVolume = null)
	{
		_data = data;
		AttachedObject._origEdgeCatcherSize = AttachedObject._contactEdgeCatcherShape.size;
		AttachedObject._guardVolume = guardVolume;
	}

	public bool CanGrabPlayer(PlayerInfo player)
		=> !PlayerState.IsAttached()
			&& _data.playerLocation[player].distanceXZ < 2f + AttachedObject._grabDistanceBuff
			&& _data.playerLocation[player].degreesToPositionXZ < 20f + AttachedObject._grabAngleBuff
			&& AttachedObject._animator.GetFloat("GrabWindow") > 0.5f;

	public void FixedUpdate_Sensors()
	{
		if (_data == null)
		{
			return;
		}

		foreach (var player in QSBPlayerManager.PlayerList)
		{
			if (!_data.sensors.ContainsKey(player))
			{
				_data.sensors.Add(player, new());
				_data.lastKnownSensors.Add(player, new());
			}

			var lanternController = Locator.GetDreamWorldController().GetPlayerLantern().GetLanternController(); // TODO - get this
			var playerLightSensor = Locator.GetPlayerLightSensor(); // TODO - get this
			_data.sensors[player].isPlayerHoldingLantern = lanternController.IsHeldByPlayer();
			_data.sensors[player].isIlluminated = AttachedObject._lightSensor.IsIlluminated();
			_data.sensors[player].isIlluminatedByPlayer = (lanternController.IsHeldByPlayer() && AttachedObject._lightSensor.IsIlluminatedByLantern(lanternController));
			_data.sensors[player].isPlayerIlluminatedByUs = playerLightSensor.IsIlluminatedByLantern(AttachedObject._lantern);
			_data.sensors[player].isPlayerIlluminated = playerLightSensor.IsIlluminated();
			_data.sensors[player].isPlayerVisible = false;
			_data.sensors[player].isPlayerHeldLanternVisible = false;
			_data.sensors[player].isPlayerDroppedLanternVisible = false;
			_data.sensors[player].isPlayerOccluded = false;

			if ((lanternController.IsHeldByPlayer() && !lanternController.IsConcealed()) || playerLightSensor.IsIlluminated())
			{
				var position = player.Camera.transform.position;
				if (AttachedObject.CheckPointInVisionCone(position))
				{
					if (AttachedObject.CheckLineOccluded(AttachedObject._sightOrigin.position, position))
					{
						_data.sensors[player].isPlayerOccluded = true;
					}
					else
					{
						_data.sensors[player].isPlayerVisible = playerLightSensor.IsIlluminated();
						_data.sensors[player].isPlayerHeldLanternVisible = (lanternController.IsHeldByPlayer() && !lanternController.IsConcealed());
					}
				}
			}

			if (!lanternController.IsHeldByPlayer() && AttachedObject.CheckPointInVisionCone(lanternController.GetLightPosition()) && !AttachedObject.CheckLineOccluded(AttachedObject._sightOrigin.position, lanternController.GetLightPosition()))
			{
				_data.sensors[player].isPlayerDroppedLanternVisible = true;
			}
		}
	}

	public void OnEnterContactTrigger(GameObject hitObj)
	{
		if (hitObj.CompareTag("PlayerDetector"))
		{
			_data.sensors[QSBPlayerManager.LocalPlayer].inContactWithPlayer = true;
		}
	}

	public void OnExitContactTrigger(GameObject hitObj)
	{
		if (hitObj.CompareTag("PlayerDetector"))
		{
			_data.sensors[QSBPlayerManager.LocalPlayer].inContactWithPlayer = false;
		}
	}
}