using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//这是AI的脚本

public class AITurnMaker: TurnMaker {

    public override void OnTurnStart()
    {
        base.OnTurnStart();
        // 显示AI回合的提醒 2秒
        new ShowMessageCommand("对方\n回合", 2.0f).AddToQueue();
        // AI抽卡
        p.DrawACard();
        StartCoroutine(MakeAITurn());
    }

    // AI的具体逻辑
    protected IEnumerator MakeAITurn()
    {
        // 决定是否要优先攻击
        bool strategyAttackFirst = false;
        if (Random.Range(0, 2) == 0)
            strategyAttackFirst = true;
        // 有可用操作就一直执行
        while (MakeOneAIMove(strategyAttackFirst))
        {
            yield return null;
        }
        // 暂停
        InsertDelay(1f);
        //结束回合
        TurnManager.Instance.EndTurn();
    }

    bool MakeOneAIMove(bool attackFirst)
    {
        // 抽卡时暂停 调用Command.cs
        if (Command.CardDrawPending())
            return true;
        // 攻击或出牌
        else if (attackFirst)
            return AttackWithACreature() || PlayACardFromHand() || UseHeroPower();
        else 
            return PlayACardFromHand() || AttackWithACreature() || UseHeroPower();
        // 上面的三个函数执行后分别返回，有任何一个调用成功true，全部失败false
    }

    bool PlayACardFromHand()
    {
        foreach (CardLogic c in p.hand.CardsInHand)
        {
            // 检查卡牌
            if (c.CanBePlayed)
            {
                if (c.ca.MaxHealth == 0)
                {
                    // 用法术牌生命为0来判断类型
                    // 判断有无作用目标
                    if (c.ca.Targets == TargetingOptions.NoTarget)
                    {
                        p.PlayASpellFromHand(c, null);
                        InsertDelay(1.5f);
                        //Debug.Log("Card: " + c.ca.name + " 可用");
                        return true;
                    }
                    // 目标是对面人物或者随从
                    else if (c.ca.Targets == TargetingOptions.AllCharacters)
                    {
                        int index = Random.Range(0, p.otherPlayer.table.CreaturesOnTable.Count + 2);
                        //Debug.Log(index);
                        if (index >= p.otherPlayer.table.CreaturesOnTable.Count)
                        {
                            p.PlayASpellFromHand(c, p.otherPlayer);
                            //Debug.Log("攻击人物");
                            InsertDelay(1.5f);
                            return true;
                        }
                        else
                        {
                            CreatureLogic targetCreature = p.otherPlayer.table.CreaturesOnTable[index];
                            //int creatureUniqueID = targetCreature.UniqueCreatureID;
                            p.PlayASpellFromHand(c, targetCreature);
                            //Debug.Log("攻击随从");
                            InsertDelay(1.5f);
                            return true;
                        }
                    }

                    else if (c.ca.Targets == TargetingOptions.YourCreatures)
                    {
                        int index = Random.Range(0, p.table.CreaturesOnTable.Count);
                        //Debug.Log(index);

                        CreatureLogic targetCreature = p.table.CreaturesOnTable[index];
                        //int creatureUniqueID = targetCreature.UniqueCreatureID;
                        p.PlayASpellFromHand(c, targetCreature);
                        //Debug.Log("攻击友军随从");
                        InsertDelay(1.5f);
                        return true;
                    }

                    else if (c.ca.Targets == TargetingOptions.YourCharacters)
                    {
                        int index = Random.Range(0, p.table.CreaturesOnTable.Count + 2);
                        //Debug.Log(index);
                        if (index >= p.table.CreaturesOnTable.Count)
                        {
                            p.PlayASpellFromHand(c, p);
                            //Debug.Log("随机攻击人物");
                            InsertDelay(1.5f);
                            return true;
                        }
                        else
                        {
                            CreatureLogic targetCreature = p.table.CreaturesOnTable[index];
                            //int creatureUniqueID = targetCreature.UniqueCreatureID;
                            p.PlayASpellFromHand(c, targetCreature);
                            //Debug.Log("随机攻击随从");
                            InsertDelay(1.5f);
                            return true;
                        }
                    }


                }
                else
                {
                    // 随从卡
                    p.PlayACreatureFromHand(c, 0);
                    InsertDelay(1.5f);
                    return true;
                }

            }
            //Debug.Log("Card: " + c.ca.name + " 不可用");
        }
        return false;
    }

    bool UseHeroPower()
    {
        if (p.ManaLeft >= 2 && !p.usedHeroPowerThisTurn)
        {
            p.UseHeroPower();
            InsertDelay(1.5f);
            //Debug.Log("AI 使用人物技能");
            return true;
        }
        return false;
    }

    bool AttackWithACreature()
    {
        foreach (CreatureLogic cl in p.table.CreaturesOnTable)
        {
            if (cl.AttacksLeftThisTurn > 0) // 检查攻击次数剩余
            {
                // 随机目标
                if (p.otherPlayer.table.CreaturesOnTable.Count > 0)
                {
                    // 检查对手随从
                    List<int> creaturesWithTaunt = new List<int>();
                    for (int i = 0; i < p.otherPlayer.table.CreaturesOnTable.Count; i++) {
                        if (p.otherPlayer.table.CreaturesOnTable[i].ca.Taunt == true) creaturesWithTaunt.Add(i);
                    }
                    if (creaturesWithTaunt.Count == 0)
                    {
                        //Debug.Log("无可攻击目标");
                        int index = Random.Range(0, p.otherPlayer.table.CreaturesOnTable.Count);
                        CreatureLogic targetCreature = p.otherPlayer.table.CreaturesOnTable[index];
                        cl.AttackCreature(targetCreature);
                    }
                    else {
                        //Debug.Log("攻击");
                        int index = Random.Range(0, creaturesWithTaunt.Count);
                        CreatureLogic targetCreature = p.otherPlayer.table.CreaturesOnTable[creaturesWithTaunt[index]];
                        cl.AttackCreature(targetCreature);
                        // 调用AttackCreature方法
                    }

                }                    
                else
                    cl.GoFace();
                    // 攻击人物
                
                InsertDelay(1f);
                //Debug.Log("随从攻击完成");
                return true;
            }
        }
        return false;
    }

    protected void InsertDelay(float delay)
    {
        new DelayCommand(delay).AddToQueue();
    }

}
