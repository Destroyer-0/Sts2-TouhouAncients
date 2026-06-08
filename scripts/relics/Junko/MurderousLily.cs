using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 杀意的百合 — 将两张杀戮灵气加入牌组。
/// 如果战斗结束时，没有任何单位死于杀戮灵气，将两张杀戮灵气加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MurderousLily : TouhouAncientRelics
{
    [SavedProperty] private bool TouhouAncients_KillingAuraKilledThisCombat { get; set; }

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
    /// 拾起时，将两张杀戮灵气加入牌组
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
        TouhouAncients_KillingAuraKilledThisCombat = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 检测杀戮灵气是否击杀了敌人
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (wasRemovalPrevented) return;
        if (base.Owner.Creature.CombatState == null) return;

        // 检查战斗中是否有杀戮灵气（只要有杀戮灵气在牌组中就算）
        var playerState = base.Owner.PlayerCombatState;
        if (playerState == null) return;

        var allCombatCards = playerState.Hand.Cards
            .Concat(playerState.DrawPile.Cards)
            .Concat(playerState.DiscardPile.Cards)
            .Concat(playerState.ExhaustPile.Cards);

        if (allCombatCards.OfType<KillingAura>().Any())
        {
            TouhouAncients_KillingAuraKilledThisCombat = true;
        }
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        if (TouhouAncients_KillingAuraKilledThisCombat) return;

        // 没有单位死于杀戮灵气，再加两张
        var player = base.Owner;
        var results = new List<CardPileAddResult>();

        for (int i = 0; i < 2; i++)
        {
            var card = player.RunState.CreateCard(ModelDb.Card<KillingAura>(), player);
            results.Add(await CardPileCmd.Add(card, PileType.Deck));
        }

        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}
