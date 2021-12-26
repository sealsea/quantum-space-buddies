﻿namespace QSB.Events
{
	public static class EventNames
	{
		// Built into Outer Wilds -- don't change unless they change in-game!
		public const string TurnOnFlashlight = nameof(TurnOnFlashlight);
		public const string TurnOffFlashlight = nameof(TurnOffFlashlight);
		public const string ProbeLauncherEquipped = nameof(ProbeLauncherEquipped);
		public const string ProbeLauncherUnequipped = nameof(ProbeLauncherUnequipped);
		public const string EquipSignalscope = nameof(EquipSignalscope);
		public const string UnequipSignalscope = nameof(UnequipSignalscope);
		public const string SuitUp = nameof(SuitUp);
		public const string RemoveSuit = nameof(RemoveSuit);
		public const string EquipTranslator = nameof(EquipTranslator);
		public const string UnequipTranslator = nameof(UnequipTranslator);
		public const string WakeUp = nameof(WakeUp);
		public const string DialogueConditionChanged = nameof(DialogueConditionChanged);
		public const string PlayerEnterQuantumMoon = nameof(PlayerEnterQuantumMoon);
		public const string PlayerExitQuantumMoon = nameof(PlayerExitQuantumMoon);
		public const string EnterRoastingMode = nameof(EnterRoastingMode);
		public const string ExitRoastingMode = nameof(ExitRoastingMode);
		public const string EnterFlightConsole = nameof(EnterFlightConsole);
		public const string ExitFlightConsole = nameof(ExitFlightConsole);
		public const string EnterShip = nameof(EnterShip);
		public const string ExitShip = nameof(ExitShip);
		public const string EyeStateChanged = nameof(EyeStateChanged);
	}
}
