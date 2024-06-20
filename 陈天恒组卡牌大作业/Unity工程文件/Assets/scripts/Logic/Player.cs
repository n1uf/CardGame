using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour, ICharacter
{
    // PUBLIC 
    // int ID 来自 ID factory
    public int PlayerID;
    // Character Asset 存储角色数据
    public CharacterAsset charAsset;
    // 玩家区域的视觉
    public PlayerArea PArea;
    // 英雄技能
    public SpellEffect HeroPowerEffect;
    // flag 一回合只能使用一次
    public bool usedHeroPowerThisTurn = false;

    // 属于玩家的逻辑部分
    public Deck deck;
    public Hand hand;
    public Table table;

    // 存储两个玩家
    public static Player[] Players;

    // coin 法术（水晶）
    private int bonusManaThisTurn = 0;


    // PROPERTIES 
    // 玩家界面
    public int ID
    {
        get{ return PlayerID; }
    }

    // 返回对手
    public Player otherPlayer
    {
        get
        {
            if (Players[0] == this)
                return Players[1];
            else
                return Players[0];
        }
    }

    // 理论水晶数量
    private int manaThisTurn;
    public int ManaThisTurn
    {
        get{ return manaThisTurn;}
        set
        {
            if (value < 0)
                manaThisTurn = 0;
            else if (value > PArea.ManaBar.Crystals.Length)
                manaThisTurn = PArea.ManaBar.Crystals.Length;
            else
                manaThisTurn = value;
            //PArea.ManaBar.TotalCrystals = manaThisTurn;
            new UpdateManaCrystalsCommand(this, manaThisTurn, manaLeft).AddToQueue();
        }
    }

    // 可用水晶数量
    private int manaLeft;
    public int ManaLeft
    {
        get
        { return manaLeft;}
        set
        {
            if (value < 0)
                manaLeft = 0;
            else if (value > PArea.ManaBar.Crystals.Length)
                manaLeft = PArea.ManaBar.Crystals.Length;
            else
                manaLeft = value;
            
            // 更新水晶状态
            //PArea.ManaBar.AvailableCrystals = manaLeft;
            new UpdateManaCrystalsCommand(this, ManaThisTurn, manaLeft).AddToQueue();
            //Debug.Log(ManaLeft);
            if (TurnManager.Instance.whoseTurn == this)
                HighlightPlayableCards();
        }
    }

    private int health;
    public int Health
    {
        get { return health;}
        set
        {
            // 防止治疗过度
            if (value > charAsset.MaxHealth)
                health = charAsset.MaxHealth;
            else
                health = value;
            if (value <= 0)
                Die(); 
        }
    }

    private int overload = 0;
    public int Overload {
        get { return overload; }
        set
        {
            overload = value;
        }
    }

    // CODE FOR EVENTS TO LET CREATURES KNOW WHEN TO CAUSE EFFECTS
    public delegate void VoidWithNoArguments();
    //public event VoidWithNoArguments CreaturePlayedEvent;
    //public event VoidWithNoArguments SpellPlayedEvent;
    //public event VoidWithNoArguments StartTurnEvent;
    public event VoidWithNoArguments EndTurnEvent;



    // ALL METHODS
    void Awake()
    {
        // 存储角色需要的脚本
        // (only 2 players)
        Players = GameObject.FindObjectsOfType<Player>();
        PlayerID = IDFactory.GetUniqueID();

        if (deck==null){
            deck = GameObject.Find("DeckLogic").GetComponent<Deck>();
            deck.cards.Shuffle();
        }

        if(charAsset == null){
            CharAssetLogic charAssetLogic = GameObject.Find("CharLogic").GetComponent<CharAssetLogic>();
            charAsset = charAssetLogic.playerChar;
        }
    }

    public virtual void OnTurnStart()
    {
        // 水晶+1
        Debug.Log("In ONTURNSTART for "+ gameObject.name);
        usedHeroPowerThisTurn = false;
        ManaThisTurn++;
        ManaLeft = System.Math.Max(0,ManaThisTurn - Overload);
        Overload = 0;
        foreach (CreatureLogic cl in table.CreaturesOnTable)
            cl.OnTurnStart();
            //调用cl来实现更新随从牌使其能再次攻击
        PArea.HeroPower.WasUsedThisTurn = false;
        //显示技能不可用
    }

    public void OnTurnEnd()
    {
        if(EndTurnEvent != null)
            EndTurnEvent.Invoke();
        ManaThisTurn -= bonusManaThisTurn;
        bonusManaThisTurn = 0;
        GetComponent<TurnMaker>().StopAllCoroutines();
    }

    // 玩家动作

    // 得到奖励水晶 
    public void GetBonusMana(int amount)
    {
        bonusManaThisTurn += amount;
        ManaThisTurn += amount;
        ManaLeft += amount;
    }

    // TEST绑定键位
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
            DrawACard();
    }

    // 从deck抽一张卡
    public void DrawACard(bool fast = false)
    {
        //先检查deck有没有卡
        if (deck.cards.Count > 0)
        {
            //再检查位置够不够
            if (hand.CardsInHand.Count < PArea.handVisual.slots.Children.Length)
            {
                // 1) 创建一张卡到手牌里
                CardLogic newCard = new CardLogic(deck.cards[0]);
                newCard.owner = this;
                hand.CardsInHand.Insert(0, newCard);
                // Debug.Log(hand.CardsInHand.Count);
                // 从deck里移除
                deck.cards.RemoveAt(0);
                // 2) 向视觉系统指令，展示抽卡
                new DrawACardCommand(hand.CardsInHand[0], this, fast, fromDeck: true).AddToQueue(); 
            }
        }
        else
        {
            // deck没有卡自己受到伤害（待定）
        }
        
    }

    // 随机得到一张卡
    public void GetACardNotFromDeck(CardAsset cardAsset)
    {
        if (hand.CardsInHand.Count < PArea.handVisual.slots.Children.Length)
        {
            // 1) 创建一张卡到手牌里
            CardLogic newCard = new CardLogic(cardAsset);
            newCard.owner = this;
            hand.CardsInHand.Insert(0, newCard);
            // 2) 视觉
            new DrawACardCommand(hand.CardsInHand[0], this, fast: true, fromDeck: false).AddToQueue(); 
        }
    }

    // 法术牌
    public void PlayASpellFromHand(int SpellCardUniqueID, int TargetUniqueID)
    {
        if (TargetUniqueID < 0) //无目标
            PlayASpellFromHand(CardLogic.CardsCreatedThisGame[SpellCardUniqueID], null);
        else if (TargetUniqueID == ID)
        {
            PlayASpellFromHand(CardLogic.CardsCreatedThisGame[SpellCardUniqueID], this);
        }
        else if (TargetUniqueID == otherPlayer.ID)
        {
            PlayASpellFromHand(CardLogic.CardsCreatedThisGame[SpellCardUniqueID], this.otherPlayer);
        }
        else
        {
            // 目标是随从
            PlayASpellFromHand(CardLogic.CardsCreatedThisGame[SpellCardUniqueID], CreatureLogic.CreaturesCreatedThisGame[TargetUniqueID]);
        }
        
    }

    // AI
    public void PlayASpellFromHand(CardLogic playedCard, ICharacter target)
    {
        ManaLeft -= playedCard.CurrentManaCost;
        Overload += playedCard.ca.OverloadCost;
        if (playedCard.effect != null)
            playedCard.effect.ActivateEffect(playedCard.ca.specialSpellAmount, target);
        else
        {
            Debug.LogWarning("No effect" + playedCard.ca.name);
        }
        // 出牌到table
        new PlayASpellCardCommand(this, playedCard).AddToQueue();
        // 从hand移除
        hand.CardsInHand.Remove(playedCard);
    }

    // 随从牌
    public void PlayACreatureFromHand(int UniqueID, int tablePos)
    {
        PlayACreatureFromHand(CardLogic.CardsCreatedThisGame[UniqueID], tablePos);
    }

    public void PlayACreatureFromHand(CardLogic playedCard, int tablePos)
    {
        // Debug.Log(ManaLeft);
        // Debug.Log(playedCard.CurrentManaCost);
        ManaLeft -= playedCard.CurrentManaCost;
        Overload += playedCard.ca.OverloadCost;
        // Debug.Log("Mana Left after played a creature: " + ManaLeft);
        // 创建新的随从牌
        CreatureLogic newCreature = new CreatureLogic(this, playedCard.ca);
        table.CreaturesOnTable.Insert(tablePos, newCreature);
        // 随从牌效果
        new PlayACreatureCommand(playedCard, this, tablePos, newCreature.UniqueCreatureID).AddToQueue();
        // 战吼
        if (newCreature.effect != null)
            newCreature.effect.WhenACreatureIsPlayed();
        // 移除
        hand.CardsInHand.Remove(playedCard);
        HighlightPlayableCards();
    }

    public void Die()
    {
        // 游戏结束
        // 禁止双方输入
        PArea.ControlsON = false;
        otherPlayer.PArea.ControlsON = false;
        TurnManager.Instance.StopTheTimer();
        new GameOverCommand(this).AddToQueue();
    }

    // 技能
    public void UseHeroPower()
    {
        ManaLeft -= 2;
        usedHeroPowerThisTurn = true;
        HeroPowerEffect.ActivateEffect();
    }

    // 高亮可用牌
    public void HighlightPlayableCards(bool removeAllHighlights = false)
    {
        //Debug.Log("HighlightPlayable remove: "+ removeAllHighlights);
        foreach (CardLogic cl in hand.CardsInHand)
        {
            GameObject g = IDHolder.GetGameObjectWithID(cl.UniqueCardID);
            if (g!=null)
                g.GetComponent<OneCardManager>().CanBePlayedNow = (cl.CurrentManaCost <= ManaLeft) && !removeAllHighlights;
        }

        foreach (CreatureLogic crl in table.CreaturesOnTable)
        {
            GameObject g = IDHolder.GetGameObjectWithID(crl.UniqueCreatureID);
            if(g!= null)
                g.GetComponent<OneCreatureManager>().CanAttackNow = (crl.AttacksLeftThisTurn > 0) && !removeAllHighlights;
        }   
        // highlight hero power
        PArea.HeroPower.Highlighted = (!usedHeroPowerThisTurn) && (ManaLeft > 1) && !removeAllHighlights;
    }

    // 初始化加载角色
    public void LoadCharacterInfoFromAsset()
    {
        Health = charAsset.MaxHealth;
        // change the visuals 
        PArea.Portrait.charAsset = charAsset;
        PArea.Portrait.ApplyLookFromAsset();

        if (charAsset.HeroPowerName != null && charAsset.HeroPowerName != "")
        {
            HeroPowerEffect = System.Activator.CreateInstance(System.Type.GetType(charAsset.HeroPowerName)) as SpellEffect;
        }
        else
        {
            Debug.LogWarning("Check" + charAsset.ClassName);
        }
    }

    // 玩家信息与视觉系统
    public void TransmitInfoAboutPlayerToVisual()
    {
        PArea.Portrait.gameObject.AddComponent<IDHolder>().UniqueID = PlayerID;
        if (GetComponent<TurnMaker>() is AITurnMaker)
        {
            // 不可使用
            PArea.AllowedToControlThisPlayer = false;
        }
        else
        {
            PArea.AllowedToControlThisPlayer = true;
        }
    }
}
