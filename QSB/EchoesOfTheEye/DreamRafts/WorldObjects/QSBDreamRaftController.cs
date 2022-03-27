﻿using Cysharp.Threading.Tasks;
using QSB.WorldSync;
using System.Threading;

namespace QSB.EchoesOfTheEye.DreamRafts.WorldObjects;

public class QSBDreamRaftController : WorldObject<DreamRaftController>
{
	public override void SendInitialState(uint to) { }

	public override async UniTask Init(CancellationToken ct) =>
		EnableDisableDetector.Add(AttachedObject.gameObject, this);
}