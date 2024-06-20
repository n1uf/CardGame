using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTurnHealFriendlyCreatures : CreatureEffect {

	public EndTurnHealFriendlyCreatures(Player owner, CreatureLogic creature, int specialAmount):base(owner, creature, specialAmount){
	}

	public override void RegisterEventEffect()
	{
		owner.EndTurnEvent += CauseEventEffect;
		//owner.otherPlayer.EndTurnEvent += CauseEventEffect;
		Debug.Log("Registered buff effect!!!!");
	}

	public override void UnRegisterEventEffect()
	{
		owner.EndTurnEvent -= CauseEventEffect;
	}
	public override void CauseEventEffect()
	{
		CreatureLogic[] FriendlyCreatures = TurnManager.Instance.whoseTurn.table.CreaturesOnTable.ToArray();
		foreach (CreatureLogic cl in FriendlyCreatures) {
			new ChangeStatsCommand (cl.ID, 2, 0, cl.Attack, cl.MaxHealth + 2).AddToQueue ();
			cl.Attack += 0;
			cl.Health += 2;
		}



	}

}
