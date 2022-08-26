﻿using Mirror;
using QSB.EchoesOfTheEye;
using QSB.StationaryProbeLauncherSync.WorldObjects;
using UnityEngine;

namespace QSB.StationaryProbeLauncherSync.TransformSync;

public class StationaryProbeLauncherTransformSync : RotatingElementsVariableSyncer<QSBStationaryProbeLauncher>
{
	protected override Transform[] RotatingElements => new Transform[] { WorldObject.AttachedObject.transform };

	protected override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);

		var launcher = (WorldObject.AttachedObject as StationaryProbeLauncher);

		writer.Write(launcher.transform.localRotation);
		writer.Write(launcher._degreesX);
		writer.Write(launcher._degreesY);
		writer.Write(launcher._audioSource.GetLocalVolume());
	}

	protected override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);

		var launcher = (WorldObject.AttachedObject as StationaryProbeLauncher);

		launcher.transform.localRotation = reader.Read<Quaternion>();
		launcher._degreesX = reader.Read<float>();
		launcher._degreesY = reader.Read<float>();
		launcher._audioSource.SetLocalVolume(reader.Read<float>());
	}
}
