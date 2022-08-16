﻿using QSB.Messaging;
using QSB.Player;

namespace QSB.AuthoritySync;

/// <summary>
/// request ownership of a world object
/// </summary>
public class WorldObjectAuthMessage : QSBWorldObjectMessage<IAuthWorldObject, uint>
{
	public WorldObjectAuthMessage(uint owner) : base(owner) { }

	public override bool ShouldReceive
	{
		get
		{
			if (!base.ShouldReceive)
			{
				return false;
			}

			// Deciding if to change the object's owner
			//		  Message
			//	   | = 0 | > 0 |
			// = 0 | No  | Yes |
			// > 0 | Yes | No  |
			// if Obj==Message then No
			// Obj

			return (WorldObject.Owner == 0 || Data == 0) && WorldObject.Owner != Data;
		}
	}

	public override void OnReceiveLocal() => WorldObject.Owner = Data;

	public override void OnReceiveRemote()
	{
		WorldObject.Owner = Data;
		if (WorldObject.Owner == 0 && WorldObject.CanOwn)
		{
			// object has no owner, but is still active for this player. request ownership
			WorldObject.SendMessage(new WorldObjectAuthMessage(QSBPlayerManager.LocalPlayerId));
		}
	}
}