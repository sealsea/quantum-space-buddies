using QSB.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QSB.WorldSync;

internal class LateWorldObjectMessage : QSBMessage<(string knownName, int index)>
{
	public LateWorldObjectMessage(string knownName, int index) : base((knownName, index)) { }
}
