using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 炼狱之烬：在你的回合开始时（在抽牌阶段开始前），从消耗堆中选择任意张牌放入手牌，
/// 并向抽牌堆中加入等量张灼伤。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PurgatoryEmbers : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Burn>().Append(
            HoverTipFactory.FromKeyword(CardKeyword.Exhaust)).Append(
            HoverTipFactory.FromKeyword(CardKeyword.Ethereal));


    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;
        if (!participants.Contains(base.Owner.Creature)) return;
        if (base.Owner.Creature.CombatState == null) return;

        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        if (exhaustPile.IsEmpty) return;

        var exhaustCards = exhaustPile.Cards.ToList();
        if (exhaustCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 0, exhaustCards.Count);
        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            exhaustCards,
            Owner,
            prefs
        )).ToList();

        if (selected.Count == 0) return;

        Flash();

        // 将选中的牌移入手牌
        foreach (var card in selected)
        {
            card.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.Add(card, PileType.Hand);
        }

        // 向抽牌堆中加入等量灼伤
        var burns = new List<CardModel>();
        for (int i = 0; i < selected.Count; i++)
        {
            burns.Add(combatState.CreateCard<Burn>(Owner));
        }

        await CardPileCmd.AddGeneratedCardsToCombat(burns, PileType.Draw, creator: base.Owner, CardPilePosition.Random);
    }
}