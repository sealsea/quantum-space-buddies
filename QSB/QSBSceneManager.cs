using OWML.Common;
using QSB.Utility;
using QSB.WorldSync;
using System;
using UnityEngine.SceneManagement;

namespace QSB
{
	public static class QSBSceneManager
	{
		public static QSBScene CurrentScene => SceneManager.GetActiveScene().ToQSBScene();

		public static bool IsInUniverse => InUniverse(CurrentScene);

		public static event Action<QSBScene, QSBScene, bool> OnSceneLoaded;
		public static event Action<QSBScene, QSBScene> OnUniverseSceneLoaded;

		private static QSBScene oldScene;

		static QSBSceneManager()
		{
			SceneManager.sceneUnloaded += OnUnloadScene;
			SceneManager.sceneLoaded += OnCompleteSceneLoad;
			DebugLog.DebugWrite("Scene Manager ready.", MessageType.Success);
		}

		private static void OnUnloadScene(Scene scene)
		{
			DebugLog.DebugWrite($"UNLOAD SCENE {scene.name}");
			oldScene = scene.ToQSBScene();
		}

		private static void OnCompleteSceneLoad(Scene scene, LoadSceneMode mode)
		{
			var newScene = scene.ToQSBScene();
			DebugLog.DebugWrite($"COMPLETE SCENE LOAD ({newScene})", MessageType.Info);
			QSBWorldSync.RemoveWorldObjects();
			var universe = InUniverse(newScene);
			if (QSBCore.IsInMultiplayer && universe)
			{
				// So objects have time to be deleted, made, whatever
				Delay.RunNextFrame(() => QSBWorldSync.BuildWorldObjects(newScene).Forget());
			}

			OnSceneLoaded?.SafeInvoke(oldScene, newScene, universe);
			if (universe)
			{
				OnUniverseSceneLoaded?.SafeInvoke(oldScene, newScene);
			}

			if (newScene == QSBScene.TitleScreen && QSBCore.IsInMultiplayer)
			{
				QSBNetworkManager.singleton.StopHost();
			}
		}

		private static bool InUniverse(QSBScene scene) =>
			scene is QSBScene.SolarSystem or QSBScene.EyeOfTheUniverse or QSBScene.DebugScene;
	}
}