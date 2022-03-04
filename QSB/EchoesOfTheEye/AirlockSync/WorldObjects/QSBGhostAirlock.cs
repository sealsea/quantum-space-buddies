using Cysharp.Threading.Tasks;
using Mirror;
using QSB.WorldSync;
using System.Threading;
using UnityEngine;

namespace QSB.EchoesOfTheEye.AirlockSync.WorldObjects;

internal class QSBGhostAirlock : WorldObject<GhostAirlock>
{
	public override void SendInitialState(uint to) { }

	public override async UniTask Init(CancellationToken ct)
	{
		if (QSBCore.IsHost)
		{
			NetworkServer.Spawn(Object.Instantiate(QSBNetworkManager.singleton.AirlockPrefab));
		}
	}
}