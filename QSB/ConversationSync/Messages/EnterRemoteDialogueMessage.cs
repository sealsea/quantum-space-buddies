﻿using QSB.ConversationSync.WorldObjects;
using QSB.Messaging;

namespace QSB.ConversationSync.Messages;

internal class EnterRemoteDialogueMessage : QSBWorldObjectMessage<QSBRemoteDialogueTrigger, int>
{
	public EnterRemoteDialogueMessage(int dialogueIndex) : base(dialogueIndex) { }

	public override void OnReceiveRemote()
		=> WorldObject.RemoteEnterDialogue(Data);
}