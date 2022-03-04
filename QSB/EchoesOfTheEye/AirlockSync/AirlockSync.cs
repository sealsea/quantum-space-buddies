using Mirror;
using QSB.Utility.VariableSync;
using UnityEngine;

namespace QSB.EchoesOfTheEye.AirlockSync;

internal class AirlockSync : NetworkBehaviour
{
	private AirlockInterface _airlockInterface;

	public Vector3VariableSyncer[] RotationElementsRotationSyncers;

	public void Init(AirlockInterface airlockInterface)
	{
		_airlockInterface =	airlockInterface;
	}

	private void Update()
	{
		if (_airlockInterface == null)
		{
			return;
		}

		if (hasAuthority)
		{
			UpdateFromLocal();
			return;
		}

		UpdateToLocal();
	}

	private void UpdateFromLocal()
	{
		for (var i = 0; i < _airlockInterface._rotatingElements.Length; i++)
		{
			RotationElementsRotationSyncers[i].Value = _airlockInterface._rotatingElements[i].localRotation.eulerAngles;
		}
	}

	private void UpdateToLocal()
	{
		for (var i = 0; i < _airlockInterface._rotatingElements.Length; i++)
		{
			_airlockInterface._rotatingElements[i].localRotation = Quaternion.Euler(RotationElementsRotationSyncers[i].Value);
		}
	}
}
