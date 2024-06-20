using UnityEngine;
using System.Collections;

public class DrawCardsBattlecry : CreatureEffect
{
    public DrawCardsBattlecry(Player owner, CreatureLogic creature, int specialAmount): base(owner, creature, specialAmount)
    {}

    // BATTLECRY
    public override void WhenACreatureIsPlayed()
    {   
        owner.DrawACard();
        owner.DrawACard();
    }
}
