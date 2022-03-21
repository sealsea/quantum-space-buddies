using GhostEnums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QSB.EchoesOfTheEye.Ghosts.Actions;

internal class QSBChaseAction : QSBGhostAction
{
	private float _lastScreamTime;

	public override GhostAction.Name GetName()
	{
		return GhostAction.Name.Chase;
	}

	public override float CalculateUtility()
	{
		if (this._data.threatAwareness < GhostData.ThreatAwareness.IntruderConfirmed || (PlayerData.GetReducedFrights() && !this._data.reducedFrights_allowChase))
		{
			return -100f;
		}
		bool flag = this._data.sensors[_data.InterestedPlayer].isPlayerVisible || this._data.sensors[_data.InterestedPlayer].isPlayerHeldLanternVisible || this._data.sensors[_data.InterestedPlayer].inContactWithPlayer;
		if ((this._running && this._data.timeSincePlayerLocationKnown[_data.InterestedPlayer] < 5f) || (flag && this._data.playerLocation[_data.InterestedPlayer].distance < this._data.playerMinLanternRange + 0.5f))
		{
			return 95f;
		}
		return -100f;
	}

	protected override void OnEnterAction()
	{
		this._controller.SetLanternConcealed(false, true);
		this._effects.AttachedObject.SetMovementStyle(GhostEffects.MovementStyle.Chase);
		if (Time.time > this._lastScreamTime + 10f && !PlayerData.GetReducedFrights())
		{
			this._effects.AttachedObject.PlayVoiceAudioNear(global::AudioType.Ghost_Chase, 1f);
			this._lastScreamTime = Time.time;
		}
	}

	public override bool Update_Action()
	{
		if (this._data.playerLocation[_data.InterestedPlayer].distance > 10f && !this._controller.GetNodeMap().CheckLocalPointInBounds(this._data.lastKnownPlayerLocation[_data.InterestedPlayer].localPosition))
		{
			return false;
		}
		this._controller.PathfindToLocalPosition(this._data.lastKnownPlayerLocation[_data.InterestedPlayer].localPosition, MoveType.CHASE);
		this._controller.FaceLocalPosition(this._data.lastKnownPlayerLocation[_data.InterestedPlayer].localPosition, TurnSpeed.FAST);
		return true;
	}
}
