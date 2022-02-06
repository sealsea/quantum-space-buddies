using HarmonyLib;
using QSB.Patches;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace QSB.DebugScene.Patches
{
	internal class DebugScenePatches : QSBPatch
	{
		public override QSBPatchTypes Type => QSBPatchTypes.OnModStart;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Locator), nameof(Locator.Awake))]
		public static bool Awake(Locator __instance)
		{
			GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(__instance.OnSwitchActiveCamera));
			if (!SceneManager.GetActiveScene().name.Contains("SolarSystem") && !SceneManager.GetActiveScene().name.Contains("DebugScene"))
			{
				__instance.LocateSceneObjects();
			}

			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Locator), nameof(Locator.Start))]
		public static bool Start(Locator __instance)
		{
			if (SceneManager.GetActiveScene().name.Contains("SolarSystem") || SceneManager.GetActiveScene().name.Contains("DebugScene"))
			{
				__instance.LocateSceneObjects();
			}

			return false;
		}
	}
}
