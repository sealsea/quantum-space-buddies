﻿using QSB.EchoesOfTheEye.DreamRafts.WorldObjects;
using QSB.Messaging;
using QSB.Patches;

namespace QSB.EchoesOfTheEye.DreamRafts.Messages;

public class ExtinguishImmediatelyMessage : QSBWorldObjectMessage<QSBDreamRaftProjector>
{
	public override void OnReceiveRemote() =>
		QSBPatch.RemoteCall(WorldObject.AttachedObject.ExtinguishImmediately);
}