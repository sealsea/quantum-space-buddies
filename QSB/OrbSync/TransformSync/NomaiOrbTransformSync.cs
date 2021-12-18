﻿using OWML.Common;
using QSB.Syncs.Unsectored.Transforms;
using QSB.Utility;
using QSB.WorldSync;
using System.Collections.Generic;
using QSB.AuthoritySync;
using QSB.OrbSync.WorldObjects;
using UnityEngine;

namespace QSB.OrbSync.TransformSync
{
	public class NomaiOrbTransformSync : UnsectoredTransformSync
	{
		public override bool IsReady => WorldObjectManager.AllObjectsAdded;
		public override bool UseInterpolation => true;
		public override bool IsPlayerObject => false;
		protected override float DistanceLeeway => 1f;

		protected override Transform InitLocalTransform() => _qsbOrb.AttachedObject.transform;
		protected override Transform InitRemoteTransform() => _qsbOrb.AttachedObject.transform;

		private QSBOrb _qsbOrb;
		private static readonly List<NomaiOrbTransformSync> _instances = new();

		public override void Start()
		{
			_instances.Add(this);
			base.Start();
		}

		protected override void OnDestroy()
		{
			_instances.Remove(this);
			base.OnDestroy();

			if (QSBCore.IsHost)
			{
				NetIdentity.UnregisterAuthQueue();
			}
			var body = AttachedObject.GetAttachedOWRigidbody();
			if (body)
			{
				body.OnUnsuspendOWRigidbody -= OnUnsuspend;
				body.OnSuspendOWRigidbody -= OnSuspend;
			}
		}

		protected override void Init()
		{
			var index = _instances.IndexOf(this);
			if (!OrbManager.Orbs.TryGet(index, out var orb))
			{
				DebugLog.ToConsole($"Error - No orb at index {index}.", MessageType.Error);
				return;
			}
			_qsbOrb = QSBWorldSync.GetWorldFromUnity<QSBOrb>(orb);
			_qsbOrb.TransformSync = this;

			base.Init();
			var body = AttachedObject.GetAttachedOWRigidbody();
			SetReferenceTransform(body.GetOrigParent());

			if (body.GetOrigParent() == Locator.GetRootTransform())
			{
				DebugLog.DebugWrite($"{LogName} with AttachedObject {AttachedObject.name} had it's original parent as SolarSystemRoot - Disabling...");
				enabled = false;
				return;
			}

			if (QSBCore.IsHost)
			{
				NetIdentity.RegisterAuthQueue();
			}
			body.OnUnsuspendOWRigidbody += OnUnsuspend;
			body.OnSuspendOWRigidbody += OnSuspend;
			NetIdentity.FireAuthQueue(body.IsSuspended() ? AuthQueueAction.Remove : AuthQueueAction.Add);
		}

		private void OnUnsuspend(OWRigidbody suspendedBody) => NetIdentity.FireAuthQueue(AuthQueueAction.Add);
		private void OnSuspend(OWRigidbody suspendedBody) => NetIdentity.FireAuthQueue(AuthQueueAction.Remove);
	}
}
