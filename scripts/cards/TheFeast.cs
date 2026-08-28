using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 狂飨：技能，耗能1。消耗抽牌堆1~3张不为狂飨的牌，每消耗一张，获得2力量与1费。
/// 回合结束时，如果这张牌在你的手牌中，将2张这张牌的复制品加入弃牌堆。
/// </summary>
[Pool(typeof(EventCardPool))]
public class TheFeast : TouhouAncientCards
{
    public override string? Author => "イチルギ";

    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MinExhaust", 1),
        new DynamicVar("MaxExhaust", 3),
        new DynamicVar("Strength", 1m),
        new EnergyVar(1),
        new DynamicVar("Copies", 2),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.ForEnergy(this)
    ];
    
    public TheFeast() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Strength"].UpgradeValueBy(1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        var drawPile = PileType.Draw.GetPile(player);
        if (drawPile.IsEmpty) return;

        // 找抽牌堆中不为狂飨的牌
        var eligibleCards =  (from c in drawPile.Cards
            where c is not TheFeast
            orderby c.Rarity, c.Id
            select c).ToList();

        if (eligibleCards.Count == 0) return;

        // 从抽牌堆中选择1~3张
        var min = DynamicVars["MinExhaust"].IntValue;
        var max = DynamicVars["MaxExhaust"].IntValue;
        var actualMax = Math.Min(max, eligibleCards.Count);
        var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, min, actualMax);
        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            eligibleCards,
            player,
            prefs
            )).ToList();

        foreach (var card in selected)
        {
            // 消耗牌并移至消耗堆
            await CardCmd.Exhaust(choiceContext,card);
            // 获得2力量与1费
            await PowerCmd.Apply<StrengthPower>(choiceContext, player.Creature, DynamicVars["Strength"].BaseValue, player.Creature, null);
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, player);
        }
        
    }

    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;

        if (!PileType.Hand.GetPile(player).Cards.Contains(this))
        {
            return;
        }
        
        // 回合结束时，如果在手牌中，将2张复制品加入弃牌堆
        var copies = new List<CardModel>();
        var combatState = Owner.Creature.CombatState;
        for (int i = 0; i < DynamicVars["Copies"].IntValue; i++)
        {
            copies.Add(CreateClone());
        }
        var results = await CardPileCmd.AddGeneratedCardsToCombat(copies, PileType.Discard, creator: base.Owner, CardPilePosition.Random);
        CardCmd.PreviewCardPileAdd(results);
    }
}
