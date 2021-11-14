﻿using System.Collections.Generic;
using QSB.MeteorSync.WorldObjects;
using QSB.Syncs.Unsectored.Rigidbodies;
using QSB.Utility;
using QSB.WorldSync;
using UnityEngine;

namespace QSB.MeteorSync.TransformSync
{
	public class MeteorTransformSync : UnsectoredRigidbodySync
	{
		public override bool IsReady => MeteorManager.MeteorsReady;
		public override bool UseInterpolation => false;

		private QSBMeteor _qsbMeteor;
		private static readonly List<MeteorTransformSync> _instances = new List<MeteorTransformSync>();

		protected override OWRigidbody GetRigidbody()
			=> _qsbMeteor.AttachedObject.owRigidbody;

		public override void Start()
		{
			_instances.Add(this);
			base.Start();
		}

		protected override void OnDestroy()
		{
			_instances.Remove(this);
			base.OnDestroy();
		}

		public override float GetNetworkSendInterval()
			=> 5f;

		protected override void Init()
		{
			_qsbMeteor = QSBWorldSync.GetWorldFromId<QSBMeteor>(_instances.IndexOf(this));
			_qsbMeteor.TransformSync = this;

			base.Init();
			SetReferenceTransform(Locator._brittleHollow.transform);

			DebugLog.DebugWrite($"{LogName} - init");
		}

		protected override bool UpdateTransform()
		{
			// DebugLog.DebugWrite($"{LogName} - {Time.time - _lastClientSendTime}");
			if (Time.time - _lastClientSendTime > GetNetworkSendInterval())
			{
				return base.UpdateTransform();
			}
			return false;
		}


		protected override void OnRenderObject()
		{
			if (!QSBCore.WorldObjectsReady
				|| !QSBCore.DebugMode
				|| !QSBCore.ShowLinesInDebug
				|| !IsReady
				|| ReferenceTransform == null
				|| _intermediaryTransform.GetReferenceTransform() == null
				|| _qsbMeteor.AttachedObject.isSuspended)
			{
				return;
			}

			base.OnRenderObject();
		}
	}
}
