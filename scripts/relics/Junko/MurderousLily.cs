using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 杀意的百合 — 将两张杀戮灵气加入牌组。
/// 如果战斗结束时，没有任何单位死于杀戮灵气，将两张杀戮灵气加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MurderousLily : TouhouAncientRelics
{
    private bool killingAuraKilledThisCombat { get; set; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<KillingAura>()
    ];

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 拾起时，将1张杀戮灵气加入牌组
    /// </summary>
    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var results = new List<CardPileAddResult>();

        for (int i = 0; i < 1; i++)
        {
            var card = player.RunState.CreateCard(ModelDb.Card<KillingAura>(), player);
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }

        CardCmd.PreviewCardPileAdd(results, 2f);
    }

    public override Task BeforeCombatStart()
    {
        killingAuraKilledThisCombat = false;
        base.Status = RelicStatus.Active;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
    // public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    // {
    //     if (command.Attacker != Owner.Creature)
    //     {
    //         return;
    //     }
    //
    //     if (command.ModelSource is not KillingAura)
    //     {
    //         return;
    //     }
    //
    //     if (command.Results.SelectMany(r => r).Any((DamageResult r) => r.WasTargetKilled))
    //     {
    //         killingAuraKilledThisCombat = true;
    //         base.Status = RelicStatus.Normal;
    //         InvokeDisplayAmountChanged();
    //     }
    //}

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props,
        Creature target, CardModel? cardSource)
    {
        if (cardSource != null && cardSource.Owner == Owner && cardSource is KillingAura)
        {
            if (result.WasTargetKilled)
            {
                killingAuraKilledThisCombat = true;
                base.Status = RelicStatus.Normal;
                InvokeDisplayAmountChanged();
            }
        }

        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        if (killingAuraKilledThisCombat) return;

        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
        // 没有单位死于杀戮灵气，再加两张
        var player = base.Owner;
        var results = new List<CardPileAddResult>();
        Flash();
        for (int i = 0; i < 2; i++)
        {
            var card = player.RunState.CreateCard(ModelDb.Card<KillingAura>(), player);
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }

        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}
