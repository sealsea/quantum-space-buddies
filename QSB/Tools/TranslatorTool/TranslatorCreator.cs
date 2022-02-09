using QSB.Player;
using QSB.Utility;

namespace QSB.Tools.TranslatorTool
{
	internal static class TranslatorCreator
	{
		internal static void CreateTranslator(PlayerInfo player)
		{
			DebugLog.DebugWrite($"CREATE TRANSLATOR");

			DebugLog.DebugWrite($"get REMOTE_NomaiTranslatorProp");
			var REMOTE_NomaiTranslatorProp = player.CameraBody.transform.Find("REMOTE_NomaiTranslatorProp").gameObject;

			DebugLog.DebugWrite($"get TranslatorGroup");
			var TranslatorGroup = REMOTE_NomaiTranslatorProp.transform.Find("TranslatorGroup");

			DebugLog.DebugWrite($"get QSBNomaiTranslator on REMOTE_NomaiTranslatorProp");
			var tool = REMOTE_NomaiTranslatorProp.GetComponent<QSBNomaiTranslator>();
			DebugLog.DebugWrite($"set Type");
			tool.Type = ToolType.Translator;
			DebugLog.DebugWrite($"set ToolGameObject");
			tool.ToolGameObject = TranslatorGroup.gameObject;
			tool.Player = player;
		}
	}
}
