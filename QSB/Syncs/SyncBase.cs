﻿using Mirror;
using OWML.Common;
using QSB.Player;
using QSB.Utility;
using QSB.WorldSync;
using System;
using System.Linq;
using UnityEngine;
using Gizmos = Popcron.Gizmos;

namespace QSB.Syncs;
/*
 * Rewrite number : 11
 * God has cursed me for my hubris, and my work is never finished.
 */

public abstract class SyncBase : QSBNetworkTransform
{
	/// <summary>
	/// valid if IsPlayerObject, otherwise null
	/// </summary>
	public PlayerInfo Player
	{
		get
		{
			if (_player == null)
			{
				DebugLog.ToConsole($"Error - trying to get SyncBase.Player for {netId} before Start has been called! "
				                   + "this really should not be happening!\n"
				                   + Environment.StackTrace,
					MessageType.Error);
			}

			return _player;
		}
	}
	private PlayerInfo _player;

	private bool IsInitialized;

	protected virtual bool CheckReady()
	{
		if (netId is uint.MaxValue or 0)
		{
			return false;
		}

		if (!QSBWorldSync.AllObjectsAdded)
		{
			return false;
		}

		if (IsPlayerObject)
		{
			if (_player == null)
			{
				return false;
			}

			if (!isLocalPlayer && !_player.IsReady)
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// can be true with null reference transform. <br/>
	/// can be true with inactive attached object.
	/// </summary>
	public bool IsValid { get; private set; }

	protected virtual bool CheckValid()
	{
		if (!IsInitialized)
		{
			return false;
		}

		if (!AttachedTransform)
		{
			DebugLog.ToConsole($"Error - AttachedObject {this} is null!", MessageType.Error);
			return false;
		}

		if (!AllowInactiveAttachedObject && !AttachedTransform.gameObject.activeInHierarchy)
		{
			return false;
		}

		if (!AllowNullReferenceTransform && !ReferenceTransform)
		{
			DebugLog.ToConsole($"Warning - {this}'s ReferenceTransform is null.", MessageType.Warning);
			return false;
		}

		return true;
	}

	protected abstract bool UseInterpolation { get; }
	protected virtual bool AllowInactiveAttachedObject => false;
	protected abstract bool AllowNullReferenceTransform { get; }
	protected virtual bool IsPlayerObject => false;
	protected virtual bool OnlyApplyOnDeserialize => false;

	public Transform AttachedTransform { get; private set; }
	public Transform ReferenceTransform { get; private set; }

	public string Name => AttachedTransform ? AttachedTransform.name : "<NullObject!>";

	public override string ToString() => (IsPlayerObject ? $"{Player.PlayerId}." : string.Empty)
	                                     + $"{netId}:{GetType().Name} ({Name})";

	protected virtual float DistanceChangeThreshold => 5f;
	private float _prevDistance;
	protected const float SmoothTime = 0.1f;
	private Vector3 _positionSmoothVelocity;
	private Quaternion _rotationSmoothVelocity;
	protected Vector3 SmoothPosition { get; private set; }
	protected Quaternion SmoothRotation { get; private set; }

	protected abstract Transform InitAttachedTransform();
	protected abstract void GetFromAttached();
	protected abstract void ApplyToAttached();

	public override void OnStartClient()
	{
		if (IsPlayerObject)
		{
			// get player objects spawned before this object (or is this one)
			// and use the closest one
			_player = QSBPlayerManager.PlayerList
				.Where(x => x.PlayerId <= netId)
				.MaxBy(x => x.PlayerId);
		}

		DontDestroyOnLoad(gameObject);
		QSBSceneManager.OnSceneLoaded += OnSceneLoaded;
	}

	public override void OnStopClient()
	{
		QSBSceneManager.OnSceneLoaded -= OnSceneLoaded;
		if (IsInitialized)
		{
			SafeUninit();
		}
	}

	private void OnSceneLoaded(OWScene oldScene, OWScene newScene, bool isInUniverse)
	{
		if (IsInitialized)
		{
			SafeUninit();
		}
	}

	private const float _pauseTimerDelay = 1;
	private float _pauseTimer;

	private void SafeInit()
	{
		this.Try("initializing", () =>
		{
			Init();
			IsInitialized = true;
		});
		if (!IsInitialized)
		{
			_pauseTimer = _pauseTimerDelay;
		}
	}

	private void SafeUninit()
	{
		this.Try("uninitializing", () =>
		{
			Uninit();
			IsInitialized = false;
			IsValid = false;
		});
		if (IsInitialized)
		{
			_pauseTimer = _pauseTimerDelay;
		}
	}

	protected virtual void Init() =>
		AttachedTransform = InitAttachedTransform();

	protected virtual void Uninit()
	{
		if (IsPlayerObject && !hasAuthority && AttachedTransform)
		{
			Destroy(AttachedTransform.gameObject);
		}
	}

	private bool _shouldApply;

	protected override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		if (OnlyApplyOnDeserialize)
		{
			_shouldApply = true;
		}
	}

	protected sealed override void Update()
	{
		if (_pauseTimer > 0)
		{
			_pauseTimer = Mathf.Max(0, _pauseTimer - Time.unscaledDeltaTime);
			return;
		}

		if (!IsInitialized && CheckReady())
		{
			SafeInit();
		}
		else if (IsInitialized && !CheckReady())
		{
			SafeUninit();
		}

		IsValid = CheckValid();
		if (!IsValid)
		{
			return;
		}

		if (!hasAuthority && UseInterpolation)
		{
			Interpolate();
		}

		if (hasAuthority)
		{
			GetFromAttached();
		}
		else if (!OnlyApplyOnDeserialize || _shouldApply)
		{
			_shouldApply = false;
			ApplyToAttached();
		}

		base.Update();
	}

	private Vector3 _prevSmoothPosition;

	private void Interpolate()
	{
		if (Vector3.Distance(SmoothPosition, _prevSmoothPosition) > DistanceChangeThreshold)
		{
			DebugLog.DebugWrite($"{this} POS teleport (change = {Vector3.Distance(SmoothPosition, _prevSmoothPosition):F4}");
		}

		var distance = Vector3.Distance(SmoothPosition, transform.position);
		if (Mathf.Abs(distance - _prevDistance) > DistanceChangeThreshold)
		{
			DebugLog.DebugWrite($"{this} DIST teleport (change = {Mathf.Abs(distance - _prevDistance):F4}");
			SmoothPosition = transform.position;
			SmoothRotation = transform.rotation;
		}
		else
		{
			SmoothPosition = Vector3.SmoothDamp(SmoothPosition, transform.position, ref _positionSmoothVelocity, SmoothTime);
			SmoothRotation = QuaternionHelper.SmoothDamp(SmoothRotation, transform.rotation, ref _rotationSmoothVelocity, SmoothTime);
		}

		_prevDistance = distance;
		_prevSmoothPosition = SmoothPosition;
	}

	public virtual void SetReferenceTransform(Transform referenceTransform)
	{
		if (ReferenceTransform == referenceTransform)
		{
			return;
		}

		DebugLog.DebugWrite($"{this} sector {ReferenceTransform} -> {referenceTransform}");

		ReferenceTransform = referenceTransform;

		if (!hasAuthority && AttachedTransform)
		{
			if (IsPlayerObject)
			{
				AttachedTransform.parent = ReferenceTransform;
				AttachedTransform.localScale = Vector3.one;
				transform.position = SmoothPosition = AttachedTransform.localPosition;
				transform.rotation = SmoothRotation = AttachedTransform.localRotation;
			}
			else
			{
				transform.position = SmoothPosition = ReferenceTransform.ToRelPos(AttachedTransform.position);
				transform.rotation = SmoothRotation = ReferenceTransform.ToRelRot(AttachedTransform.rotation);
			}
		}
	}

	protected virtual void OnRenderObject()
	{
		if (!QSBCore.DebugSettings.DrawLines
		    || !IsValid
		    || !ReferenceTransform)
		{
			return;
		}

		/* Red Cube = Where visible object should be
		 * Green cube = Where visible object is
		 * Magenta cube = Reference transform
		 * Red Line = Connection between Red Cube and Green Cube
		 * Cyan Line = Connection between Green cube and reference transform
		 */

		Gizmos.Cube(ReferenceTransform.FromRelPos(transform.position), ReferenceTransform.FromRelRot(transform.rotation), Vector3.one / 8, Color.red);
		Gizmos.Line(ReferenceTransform.FromRelPos(transform.position), AttachedTransform.transform.position, Color.red);
		Gizmos.Cube(AttachedTransform.transform.position, AttachedTransform.transform.rotation, Vector3.one / 6, Color.green);
		Gizmos.Cube(ReferenceTransform.position, ReferenceTransform.rotation, Vector3.one / 8, Color.magenta);
		Gizmos.Line(AttachedTransform.transform.position, ReferenceTransform.position, Color.cyan);
	}

	private void OnGUI()
	{
		if (!QSBCore.DebugSettings.DrawLabels
		    || Event.current.type != EventType.Repaint
		    || !IsValid
		    || !ReferenceTransform)
		{
			return;
		}

		DebugGUI.DrawLabel(AttachedTransform.transform, ToString());
	}
}
