using QSB.Utility;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace QSB.Inputs
{
	public class QSBInputManager : MonoBehaviour
	{
		// TODO : finish instruments - disabled for 0.7.0 release
		public static event Action ChertTaunt;
		public static event Action EskerTaunt;
		public static event Action RiebeckTaunt;
		public static event Action GabbroTaunt;
		public static event Action FeldsparTaunt;
		public static event Action ExitTaunt;

		public void Update()
		{
			if (Keyboard.current[Key.T].isPressed)
			{
				// Listed order is from sun to dark bramble
				if (Keyboard.current[Key.Digit1].wasPressedThisFrame)
				{
					ChertTaunt?.Invoke();
				}
				else if (Keyboard.current[Key.Digit2].wasPressedThisFrame)
				{
					EskerTaunt?.Invoke();
				}
				else if (Keyboard.current[Key.Digit3].wasPressedThisFrame)
				{
					RiebeckTaunt?.Invoke();
				}
				else if (Keyboard.current[Key.Digit4].wasPressedThisFrame)
				{
					GabbroTaunt?.Invoke();
				}
				else if (Keyboard.current[Key.Digit5].wasPressedThisFrame)
				{
					FeldsparTaunt?.Invoke();
				}
			}

			if (OWInput.GetAxisValue(InputLibrary.moveXZ, InputMode.None) != Vector2.zero
				|| OWInput.GetValue(InputLibrary.jump, InputMode.None) != 0f)
			{
				ExitTaunt?.Invoke();
			}
		}

		public static QSBInputManager Instance { get; private set; }

		public void Start()
			=> Instance = this;

		public bool InputsEnabled { get; private set; } = true;

		public void SetInputsEnabled(bool enabled)
		{
			DebugLog.DebugWrite($"INPUTS ENABLED? : {enabled}");
			InputsEnabled = enabled;
		}
	}
}