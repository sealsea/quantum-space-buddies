﻿using QSB.EchoesOfTheEye.LightSensorSync.Messages;
using QSB.Messaging;
using QSB.Player;
using QSB.WorldSync;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSB.EchoesOfTheEye.LightSensorSync;

/// <summary>
/// stores a bit of extra data needed for player light sensor sync
///
///
///
/// 
/// todo you might be able to remove when you simplify light sensor after the fake sector thingy
/// </summary>
[RequireComponent(typeof(SingleLightSensor))]
public class QSBPlayerLightSensor : MonoBehaviour
{
	private SingleLightSensor _lightSensor;
	private PlayerInfo _player;

	internal bool _locallyIlluminated;
	internal readonly List<uint> _illuminatedBy = new();

	private void Awake()
	{
		_lightSensor = GetComponent<SingleLightSensor>();
		_player = QSBPlayerManager.PlayerList.First(x => x.LightSensor == _lightSensor);

		RequestInitialStatesMessage.SendInitialState += SendInitialState;
		QSBPlayerManager.OnRemovePlayer += OnPlayerLeave;
	}

	private void OnDestroy()
	{
		RequestInitialStatesMessage.SendInitialState -= SendInitialState;
		QSBPlayerManager.OnRemovePlayer -= OnPlayerLeave;
	}

	private void SendInitialState(uint to)
	{
		new PlayerIlluminatedByMessage(_player.PlayerId, _illuminatedBy.ToArray()) { To = to }.Send();
		if (_lightSensor._illuminatingDreamLanternList != null)
		{
			new PlayerIlluminatingLanternsMessage(_player.PlayerId, _lightSensor._illuminatingDreamLanternList) { To = to }.Send();
		}
	}

	private void OnPlayerLeave(PlayerInfo player) => SetIlluminated(player.PlayerId, false);

	public void SetIlluminated(uint playerId, bool locallyIlluminated)
	{
		var illuminated = _illuminatedBy.Count > 0;
		if (locallyIlluminated)
		{
			_illuminatedBy.SafeAdd(playerId);
		}
		else
		{
			_illuminatedBy.QuickRemove(playerId);
		}

		if (!illuminated && _illuminatedBy.Count > 0)
		{
			_lightSensor._illuminated = true;
			_lightSensor.OnDetectLight.Invoke();
		}
		else if (illuminated && _illuminatedBy.Count == 0)
		{
			_lightSensor._illuminated = false;
			_lightSensor.OnDetectDarkness.Invoke();
		}
	}
}