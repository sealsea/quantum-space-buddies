﻿using Cysharp.Threading.Tasks;
using QSB.Utility;
using QSB.WorldSync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.EchoesOfTheEye.Ghosts.WorldObjects;

public class QSBGhostBrain : WorldObject<GhostBrain>, IGhostObject
{
	#region World Object Stuff

	public override void SendInitialState(uint to)
	{

	}

	public override async UniTask Init(CancellationToken ct)
	{
		Awake();
		Start();
	}

	public override bool ShouldDisplayDebug()
		=> base.ShouldDisplayDebug()
		&& QSBCore.DebugSettings.DrawGhostAI;

	public override string ReturnLabel()
	{
		var label = $"Name:{AttachedObject.ghostName}" +
			$"\r\nAwareness:{AttachedObject.GetThreatAwareness()}" +
			$"\r\nCurrent action:{AttachedObject.GetCurrentActionName()}" +
			$"\r\nIllumination meter:{_data.illuminatedByPlayerMeter}";

		return label;
	}

	public override void DisplayLines()
	{
		ControllerLines(_controller);
		DataLines(_data, _controller);

		if (_currentAction != null)
		{
			_currentAction.DrawGizmos(true);
		}
	}

	private void ControllerLines(QSBGhostController controller)
	{
		Popcron.Gizmos.Sphere(controller.AttachedObject.transform.position, 2f, Color.white);

		if (controller._followNodePath)
		{
			for (var i = controller._nodePath.Count - 1; i >= 0; i--)
			{
				Popcron.Gizmos.Sphere(controller.LocalToWorldPosition(controller._nodePath[i].localPosition), 0.25f, Color.cyan, 3);

				var hasVisited = controller._pathIndex < i;
				var color = hasVisited ? Color.white : Color.cyan;

				if (i != 0)
				{
					Popcron.Gizmos.Line(controller.LocalToWorldPosition(controller._nodePath[i].localPosition), controller.LocalToWorldPosition(controller._nodePath[i - 1].localPosition), color);
				}
			}

			if (controller._hasFinalPathPosition)
			{
				Popcron.Gizmos.Sphere(controller.LocalToWorldPosition(controller._finalPathPosition), 0.3f, Color.red, 8);
			}
		}
	}

	private void DataLines(QSBGhostData data, QSBGhostController controller)
	{
		if (data.timeSincePlayerLocationKnown != float.PositiveInfinity)
		{
			Popcron.Gizmos.Line(controller.AttachedObject.transform.position, controller.LocalToWorldPosition(data.lastKnownPlayerLocation.localPosition), Color.magenta);
			Popcron.Gizmos.Sphere(controller.LocalToWorldPosition(data.lastKnownPlayerLocation.localPosition), 1f, Color.magenta);
		}
	}

	#endregion

	internal QSBGhostController _controller;
	internal QSBGhostData _data;
	private List<QSBGhostAction> _actionLibrary = new();
	private QSBGhostAction _currentAction;
	private QSBGhostAction _pendingAction;

	public OWEvent<GhostBrain, QSBGhostData> OnIdentifyIntruder = new(4);

	public GhostAction.Name GetCurrentActionName()
	{
		if (_currentAction == null)
		{
			return GhostAction.Name.None;
		}
		return _currentAction.GetName();
	}

	public QSBGhostAction GetCurrentAction()
	{
		return _currentAction;
	}

	public QSBGhostAction GetAction(GhostAction.Name actionName)
	{
		for (int i = 0; i < _actionLibrary.Count; i++)
		{
			if (_actionLibrary[i].GetName() == actionName)
			{
				return _actionLibrary[i];
			}
		}
		return null;
	}

	public GhostData.ThreatAwareness GetThreatAwareness()
	{
		return _data.threatAwareness;
	}

	public GhostEffects GetEffects()
	{
		return AttachedObject._effects;
	}

	public bool CheckDreadAudioConditions()
	{
		return _currentAction != null
			&& _data.playerLocation.distance < 10f
			&& _currentAction.GetName() != GhostAction.Name.Sentry
			&& _currentAction.GetName() != GhostAction.Name.Grab;
	}

	public bool CheckFearAudioConditions(bool fearAudioAlreadyPlaying)
	{
		if (_currentAction == null)
		{
			return false;
		}

		if (fearAudioAlreadyPlaying)
		{
			return _currentAction.GetName() is GhostAction.Name.Chase or GhostAction.Name.Grab;
		}

		return _currentAction.GetName() == GhostAction.Name.Chase;
	}

	public void Awake()
	{
		_controller = AttachedObject.GetComponent<GhostController>().GetWorldObject<QSBGhostController>();
		AttachedObject._sensors = AttachedObject.GetComponent<GhostSensors>();
		_data = new();
		if (AttachedObject._data != null)
		{
			_data.threatAwareness = AttachedObject._data.threatAwareness;
		}
	}

	public void Start()
	{
		AttachedObject.enabled = false;
		_controller.GetDreamLanternController().enabled = false;
		_controller.Initialize(AttachedObject._nodeLayer, AttachedObject._effects.GetWorldObject<QSBGhostEffects>());
		AttachedObject._sensors.GetWorldObject<QSBGhostSensors>().Initialize(_data, AttachedObject._guardVolume);
		AttachedObject._effects.GetWorldObject<QSBGhostEffects>().Initialize(_controller.GetNodeRoot(), _controller, _data);
		AttachedObject._effects.OnCallForHelp += AttachedObject.OnCallForHelp;
		_data.reducedFrights_allowChase = AttachedObject._reducedFrights_allowChase;
		_controller.SetLanternConcealed(AttachedObject._startWithLanternConcealed, false);
		AttachedObject._intruderConfirmedBySelf = false;
		AttachedObject._intruderConfirmPending = false;
		AttachedObject._intruderConfirmTime = 0f;

		DebugLog.DebugWrite($"{AttachedObject._name} setting up actions :");

		for (var i = 0; i < AttachedObject._actions.Length; i++)
		{
			DebugLog.DebugWrite($"- {AttachedObject._actions[i]}");
			var ghostAction = QSBGhostAction.CreateAction(AttachedObject._actions[i]);
			ghostAction.Initialize(this);
			_actionLibrary.Add(ghostAction);
		}

		ClearPendingAction();
	}

	public void OnDestroy()
	{
		AttachedObject._sensors.RemoveEventListeners();
		_controller.AttachedObject.OnArriveAtPosition -= AttachedObject.OnArriveAtPosition;
		_controller.AttachedObject.OnTraversePathNode -= AttachedObject.OnTraversePathNode;
		_controller.AttachedObject.OnFaceNode -= AttachedObject.OnFaceNode;
		_controller.AttachedObject.OnFinishFaceNodeList -= AttachedObject.OnFinishFaceNodeList;
		AttachedObject._effects.OnCallForHelp -= AttachedObject.OnCallForHelp;
		GlobalMessenger.RemoveListener("EnterDreamWorld", new Callback(AttachedObject.OnEnterDreamWorld));
		GlobalMessenger.RemoveListener("ExitDreamWorld", new Callback(AttachedObject.OnExitDreamWorld));
	}

	public void TabulaRasa()
	{
		AttachedObject._intruderConfirmedBySelf = false;
		AttachedObject._intruderConfirmPending = false;
		AttachedObject._intruderConfirmTime = 0f;
		AttachedObject._playResponseAudio = false;
		_data.TabulaRasa();
	}

	public void Die()
	{
		if (!_data.isAlive)
		{
			return;
		}

		_data.isAlive = false;
		_controller.StopMoving();
		_controller.StopFacing();
		_controller.ExtinguishLantern();
		_controller.GetCollider().GetComponent<OWCollider>().SetActivation(false);
		_controller.GetGrabController().ReleasePlayer();
		_pendingAction = null;
		_currentAction = null;
		_data.currentAction = GhostAction.Name.None;
		AttachedObject._effects.PlayDeathAnimation();
		AttachedObject._effects.PlayDeathEffects();
	}

	public void EscalateThreatAwareness(GhostData.ThreatAwareness newThreatAwareness)
	{
		DebugLog.DebugWrite($"{AttachedObject._name} Escalate threat awareness to {newThreatAwareness}");

		if (_data.threatAwareness < newThreatAwareness)
		{
			_data.threatAwareness = newThreatAwareness;
			if (_data.isAlive && _data.threatAwareness == GhostData.ThreatAwareness.IntruderConfirmed)
			{
				if (AttachedObject._intruderConfirmedBySelf)
				{
					AttachedObject._effects.PlayVoiceAudioFar(global::AudioType.Ghost_IntruderConfirmed, 1f);
					return;
				}

				if (AttachedObject._playResponseAudio)
				{
					AttachedObject._effects.PlayVoiceAudioFar(global::AudioType.Ghost_IntruderConfirmedResponse, 1f);
					AttachedObject._playResponseAudio = false;
				}
			}
		}
	}

	public void WakeUp()
	{
		DebugLog.DebugWrite($"Wake up!");
		_data.hasWokenUp = true;
	}

	public bool HearGhostCall(Vector3 playerLocalPosition, float reactDelay, bool playResponseAudio = false)
	{
		if (_data.isAlive && !_data.hasWokenUp)
		{
			return false;
		}

		if (_data.threatAwareness < GhostData.ThreatAwareness.IntruderConfirmed && !AttachedObject._intruderConfirmPending)
		{
			AttachedObject._intruderConfirmedBySelf = false;
			AttachedObject._intruderConfirmPending = true;
			AttachedObject._intruderConfirmTime = Time.time + reactDelay;
			AttachedObject._playResponseAudio = playResponseAudio;
			return true;
		}

		return false;
	}

	public bool HearCallForHelp(Vector3 playerLocalPosition, float reactDelay)
	{
		if (_data.isAlive && !_data.hasWokenUp)
		{
			return false;
		}

		DebugLog.DebugWrite($"{AttachedObject._name} Hear call for help!");

		if (_data.threatAwareness < GhostData.ThreatAwareness.IntruderConfirmed)
		{
			_data.threatAwareness = GhostData.ThreatAwareness.IntruderConfirmed;
			AttachedObject._intruderConfirmPending = false;
		}

		AttachedObject._effects.PlayRespondToHelpCallAudio(reactDelay);
		_data.reduceGuardUtility = true;
		_data.lastKnownPlayerLocation.UpdateLocalPosition(playerLocalPosition, _controller.AttachedObject);
		_data.wasPlayerLocationKnown = true;
		_data.timeSincePlayerLocationKnown = 0f;
		return true;
	}

	public void HintPlayerLocation()
	{
		HintPlayerLocation(_data.playerLocation.localPosition, Time.time);
	}

	public void HintPlayerLocation(Vector3 localPosition, float informationTime)
	{
		if (!_data.hasWokenUp || _data.isPlayerLocationKnown)
		{
			return;
		}

		if (informationTime > _data.timeLastSawPlayer)
		{
			_data.lastKnownPlayerLocation.UpdateLocalPosition(localPosition, _controller.AttachedObject);
			_data.wasPlayerLocationKnown = true;
			_data.timeSincePlayerLocationKnown = 0f;
		}
	}

	public void FixedUpdate()
	{
		if (!AttachedObject.enabled)
		{
			return;
		}
		_controller.FixedUpdate_Controller();
		AttachedObject._sensors.FixedUpdate_Sensors();
		_data.FixedUpdate_Data(_controller, AttachedObject._sensors);
		AttachedObject.FixedUpdate_ThreatAwareness();
		if (_currentAction != null)
		{
			_currentAction.FixedUpdate_Action();
		}
	}

	public void Update()
	{
		if (!AttachedObject.enabled)
		{
			return;
		}
		_controller.Update_Controller();
		AttachedObject._sensors.Update_Sensors();
		AttachedObject._effects.Update_Effects();
		var flag = false;
		if (_currentAction != null)
		{
			flag = _currentAction.Update_Action();
		}

		if (!flag && _currentAction != null)
		{
			_currentAction.ExitAction();
			_data.previousAction = _currentAction.GetName();
			_currentAction = null;
			_data.currentAction = GhostAction.Name.None;
		}

		if (_data.isAlive && !Locator.GetDreamWorldController().IsExitingDream())
		{
			AttachedObject.EvaluateActions();
		}
	}

	public void FixedUpdate_ThreatAwareness()
	{
		if (_data.threatAwareness == GhostData.ThreatAwareness.IntruderConfirmed)
		{
			return;
		}
		if (!AttachedObject._intruderConfirmPending && (_data.threatAwareness > GhostData.ThreatAwareness.EverythingIsNormal || _data.playerLocation.distance < 20f || _data.sensor.isPlayerIlluminatedByUs) && (_data.sensor.isPlayerVisible || _data.sensor.inContactWithPlayer))
		{
			AttachedObject._intruderConfirmedBySelf = true;
			AttachedObject._intruderConfirmPending = true;
			var num = Mathf.Lerp(0.1f, 1.5f, Mathf.InverseLerp(5f, 25f, _data.playerLocation.distance));
			AttachedObject._intruderConfirmTime = Time.time + num;
		}
		if (AttachedObject._intruderConfirmPending && Time.time > AttachedObject._intruderConfirmTime)
		{
			AttachedObject.EscalateThreatAwareness(GhostData.ThreatAwareness.IntruderConfirmed);
			OnIdentifyIntruder.Invoke(AttachedObject, _data);
		}
	}

	public void EvaluateActions()
	{
		if (_currentAction != null && !_currentAction.IsInterruptible())
		{
			return;
		}

		var num = float.NegativeInfinity;
		QSBGhostAction actionWithHighestUtility = null;
		for (var i = 0; i < _actionLibrary.Count; i++)
		{
			var num2 = _actionLibrary[i].CalculateUtility();
			if (num2 > num)
			{
				num = num2;
				actionWithHighestUtility = _actionLibrary[i];
			}
		}

		if (actionWithHighestUtility == null)
		{
			DebugLog.ToConsole($"Error - Couldn't find action with highest utility for {AttachedObject._name}?!", OWML.Common.MessageType.Error);
			return;
		}

		var flag = false;
		if (_pendingAction == null || (actionWithHighestUtility.GetName() != _pendingAction.GetName() && num > AttachedObject._pendingActionUtility))
		{
			_pendingAction = actionWithHighestUtility;
			AttachedObject._pendingActionUtility = num;
			AttachedObject._pendingActionTimer = _pendingAction.GetActionDelay();
			flag = true;
		}

		if (_pendingAction != null && _currentAction != null && _pendingAction.GetName() == _currentAction.GetName())
		{
			ClearPendingAction();
			flag = false;
		}

		if (flag)
		{
			_pendingAction.OnSetAsPending();
		}

		if (_pendingAction != null && AttachedObject._pendingActionTimer <= 0f)
		{
			ChangeAction(_pendingAction);
		}

		if (_pendingAction != null)
		{
			AttachedObject._pendingActionTimer -= Time.deltaTime;
		}
	}

	public void ChangeAction(QSBGhostAction action)
	{
		DebugLog.DebugWrite($"{AttachedObject._name} Change action to {action?.GetName()}");

		if (_currentAction != null)
		{
			_currentAction.ExitAction();
			_data.previousAction = _currentAction.GetName();
		}
		else
		{
			_data.previousAction = GhostAction.Name.None;
		}
		_currentAction = action;
		_data.currentAction = (action != null) ? action.GetName() : GhostAction.Name.None;
		if (_currentAction != null)
		{
			_currentAction.EnterAction();
			_data.OnEnterAction(_currentAction.GetName());
		}
		ClearPendingAction();
	}

	public void ClearPendingAction()
	{
		_pendingAction = null;
		AttachedObject._pendingActionUtility = -100f;
		AttachedObject._pendingActionTimer = 0f;
	}

	public void OnArriveAtPosition()
	{
		if (_currentAction != null)
		{
			_currentAction.OnArriveAtPosition();
		}
	}

	public void OnTraversePathNode(GhostNode node)
	{
		if (_currentAction != null)
		{
			_currentAction.OnTraversePathNode(node);
		}
	}

	public void OnFaceNode(GhostNode node)
	{
		if (_currentAction != null)
		{
			_currentAction.OnFaceNode(node);
		}
	}

	public void OnFinishFaceNodeList()
	{
		if (_currentAction != null)
		{
			_currentAction.OnFinishFaceNodeList();
		}
	}

	public void OnCallForHelp()
	{
		DebugLog.DebugWrite($"{AttachedObject._name} - iterating through helper list for callforhelp");

		if (AttachedObject._helperGhosts != null)
		{
			for (var i = 0; i < AttachedObject._helperGhosts.Length; i++)
			{
				AttachedObject._helperGhosts[i].HearCallForHelp(_data.playerLocation.localPosition, 3f);
			}
		}
	}

	public void OnEnterDreamWorld()
	{
		AttachedObject.enabled = true;
		_controller.GetDreamLanternController().enabled = true;
	}

	public void OnExitDreamWorld()
	{
		AttachedObject.enabled = false;
		_controller.GetDreamLanternController().enabled = false;
		ChangeAction(null);
		_data.OnPlayerExitDreamWorld();
	}
}
