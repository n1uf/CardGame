using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTurnDamageBothHeroes : CreatureEffect {

	public EndTurnDamageBothHeroes(Player owner, CreatureLogic creature, int specialAmount):base(owner, creature, specialAmount)
	{}

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
		new DealDamageCommand(owner.otherPlayer.PlayerID, specialAmount, owner.otherPlayer.Health - specialAmount).AddToQueue();
		owner.otherPlayer.Health -= specialAmount;
        new DealDamageCommand(owner.PlayerID, specialAmount, owner.Health - specialAmount).AddToQueue();
        owner.Health -= specialAmount;				
	}


}
