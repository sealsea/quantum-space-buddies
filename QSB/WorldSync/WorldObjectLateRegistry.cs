using QSB.Messaging;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.WorldSync;

internal class WorldObjectLateRegistry : MonoBehaviour
{
	private static Dictionary<string, int> ConstantNameToIndex = new();

	public static void RegisterLateWorldObjectServer<TWorldObject, TUnityObject>(TUnityObject unityObject, string constantName)
		where TWorldObject : WorldObject<TUnityObject>, new()
		where TUnityObject : MonoBehaviour
	{
		var id = QSBWorldSync.InitSingle<TWorldObject, TUnityObject>(unityObject);

		new LateWorldObjectMessage(constantName, id).Send();
	}

	public static void RegisterLateWorldObjectClient<TWorldObject, TUnityObject>(TUnityObject unityObject, string constantName)
		where TWorldObject : WorldObject<TUnityObject>, new()
		where TUnityObject : MonoBehaviour
	{
		Delay.RunWhen(() => ConstantNameToIndex.ContainsKey(constantName), RegisterSingleWorldObject);

		void RegisterSingleWorldObject()
		{

		}
	}
}
