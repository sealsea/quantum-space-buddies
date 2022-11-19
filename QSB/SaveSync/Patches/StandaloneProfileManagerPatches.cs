using HarmonyLib;
using Newtonsoft.Json;
using QSB.Patches;
using QSB.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.SaveSync.Patches;

[HarmonyPatch(typeof(StandaloneProfileManager))]
internal class StandaloneProfileManagerPatches : QSBPatch
{
	public override QSBPatchTypes Type => QSBPatchTypes.OnModStart;
	public override GameVendor PatchVendor => GameVendor.Epic | GameVendor.Steam;

	[HarmonyPrefix]
	[HarmonyPatch(nameof(StandaloneProfileManager.BackupExistsForBrokenData))]
	public static bool BackupExistsForBrokenData(StandaloneProfileManager __instance, ref bool __result)
	{
		var str = __instance._profileBackupPath + "/" + __instance._currentProfile.profileName;
		var save = str + "/data_mult.owsave";
		var player = str + "/player.owsett";
		var graphics = str + "/graphics.owsett";
		var inputs = str + "/input_new.owsett";

		__result = (__instance._currentProfile.brokenSaveData && File.Exists(save))
			|| (__instance._currentProfile.brokenSettingsData && File.Exists(player))
			|| (__instance._currentProfile.brokenGfxSettingsData && File.Exists(graphics))
			|| (__instance._currentProfile.brokenRebindingData && File.Exists(inputs));

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(StandaloneProfileManager.DeleteProfile))]
	public static bool DeleteProfile(string profileName, StandaloneProfileManager __instance)
	{
		Debug.Log("DeleteProfile");
		var flag = false;
		var profileData = new StandaloneProfileManager.ProfileData
		{
			profileName = string.Empty
		};
		for (var i = 0; i < __instance._profiles.Count; i++)
		{
			if (profileName == __instance._profiles[i].profileName)
			{
				profileData = __instance._profiles[i];
				flag = true;
				break;
			}
		}

		if (!flag)
		{
			return false;
		}

		__instance.MarkBusyWithFileOps(true);
		var text = __instance._profilesPath + "/" + profileData.profileName + ".owprofile";
		var text2 = __instance._profilesPath + "/" + profileData.profileName;
		var text3 = text2 + "/data.owsave";
		var text4 = text2 + "/player.owsett";
		var text5 = text2 + "/graphics.owsett";
		var text6 = text2 + "/input.owsett";
		var text7 = text2 + "/input_new.owsett";
		var text8 = __instance._profileBackupPath + "/" + profileData.profileName;
		var text9 = text8 + "/data.owsave";
		var text10 = text8 + "/player.owsett";
		var text11 = text8 + "/graphics.owsett";
		var text12 = text8 + "/input.owsett";
		var text13 = text8 + "/input_new.owsett";
		Stream stream = null;
		try
		{
			if (File.Exists(text))
			{
				File.Delete(text);
				Debug.Log("Delete " + text);
			}

			if (File.Exists(text3))
			{
				File.Delete(text3);
				Debug.Log("Delete " + text3);
			}

			if (File.Exists(text4))
			{
				File.Delete(text4);
				Debug.Log("Delete " + text4);
			}

			if (File.Exists(text5))
			{
				File.Delete(text5);
				Debug.Log("Delete " + text5);
			}

			if (File.Exists(text6))
			{
				File.Delete(text6);
				Debug.Log("Delete " + text6);
			}

			if (File.Exists(text7))
			{
				File.Delete(text7);
				Debug.Log("Delete " + text7);
			}

			if (File.Exists(text9))
			{
				File.Delete(text9);
				Debug.Log("Delete " + text9);
			}

			if (File.Exists(text10))
			{
				File.Delete(text10);
				Debug.Log("Delete " + text10);
			}

			if (File.Exists(text11))
			{
				File.Delete(text11);
				Debug.Log("Delete " + text11);
			}

			if (File.Exists(text12))
			{
				File.Delete(text12);
				Debug.Log("Delete " + text12);
			}

			if (File.Exists(text13))
			{
				File.Delete(text13);
				Debug.Log("Delete " + text13);
			}

			__instance._profiles.Remove(profileData);
			var files = Directory.GetFiles(text2);
			var directories = Directory.GetDirectories(text2);
			if (files.Length == 0 && directories.Length == 0)
			{
				Directory.Delete(text2);
			}
			else
			{
				Debug.LogWarning(" Directory not empty. Cannot delete. ");
			}

			if (Directory.Exists(text8))
			{
				files = Directory.GetFiles(text8);
				directories = Directory.GetDirectories(text8);
				if (files.Length == 0 && directories.Length == 0)
				{
					Directory.Delete(text8);
				}
				else
				{
					Debug.LogWarning("Backup Directory not empty. Cannot delete.");
				}
			}

			__instance.RaiseEvent(nameof(__instance.OnUpdatePlayerProfiles));
		}
		catch (Exception ex)
		{
			if (stream != null)
			{
				stream.Close();
			}

			Debug.LogError("[" + ex.Message + "] Failed to delete all profile data");
			__instance.MarkBusyWithFileOps(false);
		}

		__instance.MarkBusyWithFileOps(false);

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(StandaloneProfileManager.LoadSaveFilesFromProfiles))]
	public static bool LoadSaveFilesFromProfiles(StandaloneProfileManager __instance)
	{
		__instance.MarkBusyWithFileOps(true);
		foreach (var profileData in __instance._profiles)
		{
			var path = __instance._profilesPath + "/" + profileData.profileName;
			GameSave gameSave = null;
			SettingsSave settingsSave = null;
			GraphicSettings graphicSettings = null;
			var text = "";
			if (Directory.Exists(path))
			{
				Stream stream = null;
				var directoryInfo = new DirectoryInfo(path);
				profileData.brokenSaveData = __instance.TryLoadSaveData<GameSave>(profileData, ref stream, "data.owsave", directoryInfo, out gameSave);
				profileData.brokenSettingsData = __instance.TryLoadSaveData<SettingsSave>(profileData, ref stream, "player.owsett", directoryInfo, out settingsSave);
				profileData.brokenGfxSettingsData = __instance.TryLoadSaveData<GraphicSettings>(profileData, ref stream, "graphics.owsett", directoryInfo, out graphicSettings);
				profileData.brokenRebindingData = __instance.TryLoadInputBindingsSave(profileData, ref stream, directoryInfo, out text);
			}

			var str = __instance._profileBackupPath + "/" + profileData.profileName;
			var path2 = str + "/data.owsave";
			var path3 = str + "/player.owsett";
			var path4 = str + "/graphics.owsett";
			var path5 = str + "/input_new.owsett";
			if (gameSave == null)
			{
				profileData.brokenSaveData = File.Exists(path2);
				gameSave = new GameSave();
				Debug.LogError("Could not find game save for " + profileData.profileName);
			}

			if (settingsSave == null)
			{
				profileData.brokenSettingsData = File.Exists(path3);
				settingsSave = new SettingsSave();
				Debug.LogError("Could not find game settings for " + profileData.profileName);
			}

			if (graphicSettings == null)
			{
				profileData.brokenGfxSettingsData = File.Exists(path4);
				graphicSettings = new GraphicSettings(true);
				Debug.LogError("Could not find graphics settings for " + profileData.profileName);
			}

			if (string.IsNullOrEmpty(text))
			{
				profileData.brokenRebindingData = File.Exists(path5);
				text = ((InputManager)OWInput.SharedInputManager).commandManager.DefaultInputActions.ToJson();
				Debug.LogError("Could not find input action settings for " + profileData.profileName);
			}

			profileData.gameSave = gameSave;
			profileData.settingsSave = settingsSave;
			profileData.graphicsSettings = graphicSettings;
			profileData.inputJSON = text;
		}

		__instance.MarkBusyWithFileOps(false);
		if (__instance.CurrentProfileHasBrokenData())
		{
			__instance.RaiseEvent(nameof(StandaloneProfileManager.OnBrokenDataExists));
		}

		__instance.RaiseEvent(nameof(StandaloneProfileManager.OnProfileReadDone));

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(StandaloneProfileManager.RestoreCurrentProfileBackup))]
	public static bool RestoreCurrentProfileBackup(StandaloneProfileManager __instance)
	{
		__instance.MarkBusyWithFileOps(true);
		var profilePath = __instance._profilesPath + "/" + __instance.currentProfile.profileName;
		var savePath = profilePath + "/" + "data_mult.owsave";
		var settingsPath = profilePath + "/" + StandaloneProfileManager._gameSettingsFilename;
		var graphicsPath = profilePath + "/" + StandaloneProfileManager._gfxSettingsFilename;
		var inputsPath = profilePath + "/" + StandaloneProfileManager._inputActionsSettingsFilename;

		var profileBackupPath = __instance._profileBackupPath + "/" + __instance.currentProfile.profileName;
		var saveBackupPath = profileBackupPath + "/" + "data_mult.owsave";
		var settingsBackupPath = profileBackupPath + "/" + StandaloneProfileManager._gameSettingsFilename;
		var graphicsBackupPath = profileBackupPath + "/" + StandaloneProfileManager._gfxSettingsFilename;
		var inputsBackupPath = profileBackupPath + "/" + StandaloneProfileManager._inputActionsSettingsFilename;

		Stream stream = null;
		try
		{
			if (!Directory.Exists(__instance._profilesPath))
			{
				Directory.CreateDirectory(__instance._profilesPath);
			}
			if (!Directory.Exists(__instance._profileTempPath))
			{
				Directory.CreateDirectory(__instance._profileTempPath);
			}
			if (!Directory.Exists(__instance._profileBackupPath))
			{
				Directory.CreateDirectory(__instance._profileBackupPath);
			}
			if (!Directory.Exists(profilePath))
			{
				Directory.CreateDirectory(profilePath);
			}
			if (!Directory.Exists(profileBackupPath))
			{
				Directory.CreateDirectory(profileBackupPath);
			}

			var di = new DirectoryInfo(profileBackupPath);

			if (__instance.currentProfile.brokenSaveData && File.Exists(saveBackupPath))
			{
				__instance.currentProfile.gameSave = LoadAndCopyBackupSave<GameSave>("data_mult.owsave", saveBackupPath, savePath);
			}

			if (__instance.currentProfile.brokenSettingsData && File.Exists(settingsBackupPath))
			{
				__instance.currentProfile.settingsSave = LoadAndCopyBackupSave<SettingsSave>(StandaloneProfileManager._gameSettingsFilename, settingsBackupPath, settingsPath);
			}

			if (__instance.currentProfile.brokenGfxSettingsData && File.Exists(graphicsBackupPath))
			{
				__instance.currentProfile.graphicsSettings = LoadAndCopyBackupSave<GraphicSettings>(StandaloneProfileManager._gfxSettingsFilename, graphicsBackupPath, graphicsPath);
			}

			if (__instance.currentProfile.brokenRebindingData && File.Exists(inputsBackupPath))
			{
				__instance.TryLoadInputBindingsSave(__instance.currentProfile, ref stream, di, out var inputJSON);
				if (inputJSON != "")
				{
					__instance.currentProfile.inputJSON = inputJSON;
					File.Copy(inputsBackupPath, inputsPath, overwrite: true);
				}
				else
				{
					Debug.LogError("Could not load backup input bindings save.");
				}

				stream?.Close();
				stream = null;
			}

			__instance.RaiseEvent(nameof(StandaloneProfileManager.OnBackupDataRestored));

			T LoadAndCopyBackupSave<T>(string fileName, string backupPath, string fullPath) where T : class
			{
				__instance.TryLoadSaveData<T>(__instance.currentProfile, ref stream, fileName, di, out var saveData);
				if (saveData != null)
				{
					File.Copy(backupPath, fullPath, overwrite: true);
				}
				else
				{
					Debug.LogError("Could not load backup " + typeof(T).Name + " save.");
				}

				stream?.Close();
				stream = null;
				return saveData;
			}
		}
		catch (Exception ex)
		{
			stream?.Close();
			Debug.LogError("Exception during backup restore: " + ex.Message);
			__instance.MarkBusyWithFileOps(false);
		}

		__instance.MarkBusyWithFileOps(false);

		return false;
	}

	[HarmonyPrefix]
	[HarmonyPatch(nameof(StandaloneProfileManager.TrySaveProfile))]
	public static bool TrySaveProfile(StandaloneProfileManager.ProfileData pd, GameSave gameSave, SettingsSave settingsSave, GraphicSettings graphicsSettings, string inputJson, StandaloneProfileManager __instance, ref bool __result)
	{
		__instance.MarkBusyWithFileOps(isBusy: true);
		var profilePath = __instance._profilesPath + "/" + pd.profileName;
		var profileManifestPath = __instance._profilesPath + "/" + pd.profileName + ".owprofile";
		var saveDataPath = profilePath + "/" + "data_mult.owsave";
		var settingsPath = profilePath + "/" + StandaloneProfileManager._gameSettingsFilename;
		var graphicsPath = profilePath + "/" + StandaloneProfileManager._gfxSettingsFilename;
		var inputsPath = profilePath + "/" + StandaloneProfileManager._inputActionsSettingsFilename;

		var tempProfilePath = __instance._profileTempPath + "/GameData";
		var tempProfileManifestPath = __instance._profileTempPath + "/CurrentProfile.owprofile";
		var tempSaveDataPath = tempProfilePath + "/" + "data_mult.owsave";
		var tempSettingsPath = tempProfilePath + "/" + StandaloneProfileManager._gameSettingsFilename;
		var tempGraphicsPath = tempProfilePath + "/" + StandaloneProfileManager._gfxSettingsFilename;
		var tempInputsPath = tempProfilePath + "/" + StandaloneProfileManager._inputActionsSettingsFilename;

		var backupProfilePath = __instance._profileBackupPath + "/" + pd.profileName;
		var backupSaveDataPath = backupProfilePath + "/" + "data_mult.owsave";
		var backupSettingsPath = backupProfilePath + "/" + StandaloneProfileManager._gameSettingsFilename;
		var backupGraphicsPath = backupProfilePath + "/" + StandaloneProfileManager._gfxSettingsFilename;
		var backupInputsPath = backupProfilePath + "/" + StandaloneProfileManager._inputActionsSettingsFilename;

		Stream stream = null;
		try
		{
			// Create folders if they don't exist

			if (!Directory.Exists(__instance._profilesPath))
			{
				Directory.CreateDirectory(__instance._profilesPath);
			}

			if (!Directory.Exists(__instance._profileTempPath))
			{
				Directory.CreateDirectory(__instance._profileTempPath);
			}

			if (!Directory.Exists(__instance._profileBackupPath))
			{
				Directory.CreateDirectory(__instance._profileBackupPath);
			}

			if (!Directory.Exists(profilePath))
			{
				Directory.CreateDirectory(profilePath);
			}

			if (!Directory.Exists(tempProfilePath))
			{
				Directory.CreateDirectory(tempProfilePath);
			}

			if (!Directory.Exists(backupProfilePath))
			{
				Directory.CreateDirectory(backupProfilePath);
			}

			// create temp files

			SaveData(tempProfileManifestPath, pd);
			if (gameSave != null)
			{
				pd.gameSave = SaveData(tempSaveDataPath, gameSave);
			}

			if (settingsSave != null)
			{
				pd.settingsSave = SaveData(tempSettingsPath, settingsSave);
			}

			if (graphicsSettings != null)
			{
				pd.graphicsSettings = SaveData(tempGraphicsPath, graphicsSettings);
			}

			if (inputJson != null)
			{
				File.WriteAllText(tempInputsPath, inputJson);
				pd.inputJSON = inputJson;
			}

			// create backups of old files

			if (File.Exists(saveDataPath))
			{
				File.Copy(saveDataPath, backupSaveDataPath, overwrite: true);
			}

			if (File.Exists(settingsPath))
			{
				File.Copy(settingsPath, backupSettingsPath, overwrite: true);
			}

			if (File.Exists(graphicsPath))
			{
				File.Copy(graphicsPath, backupGraphicsPath, overwrite: true);
			}

			if (File.Exists(inputsPath))
			{
				File.Copy(inputsPath, backupInputsPath, overwrite: true);
			}

			// delete old files and move temp files

			File.Delete(profileManifestPath);
			File.Move(tempProfileManifestPath, profileManifestPath);

			if (gameSave != null)
			{
				File.Delete(saveDataPath);
				File.Move(tempSaveDataPath, saveDataPath);
			}

			if (settingsSave != null)
			{
				File.Delete(settingsPath);
				File.Move(tempSettingsPath, settingsPath);
			}

			if (graphicsSettings != null)
			{
				File.Delete(graphicsPath);
				File.Move(tempGraphicsPath, graphicsPath);
			}

			if (inputJson != null)
			{
				File.Delete(inputsPath);
				File.Move(tempInputsPath, inputsPath);
			}

			__instance.RaiseEvent(nameof(StandaloneProfileManager.OnProfileDataSaved), true);
		}
		catch (Exception ex)
		{
			if (stream != null)
			{
				stream.Close();
			}

			__instance.RaiseEvent(nameof(StandaloneProfileManager.OnProfileDataSaved), false);

			Debug.LogError("[" + ex.Message + "] Error saving file for " + pd.profileName);
			__instance.MarkBusyWithFileOps(isBusy: false);
			return false;
		}

		__instance.MarkBusyWithFileOps(isBusy: false);
		return true;

		T SaveData<T>(string filePath, T data)
		{
			stream = File.Open(filePath, FileMode.Create);
			using (JsonWriter jsonWriter = new JsonTextWriter(new StreamWriter(stream)))
			{
				__instance._jsonSerializer.Serialize(jsonWriter, data);
			}

			stream = null;
			return data;
		}
	}
}
