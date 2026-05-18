using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 伪灵异珠：每个回合开始时获得1能量。
/// 抽牌阶段抽到的牌中，有随机两张牌获得沉重（Weighted）。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class FakeSpiritOrb : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("Amount", 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        new HoverTip(
            ModelDb.Affliction<Weighted>().ToMutable(), WeightTip
        )
    ];

    private LocString WeightTip
    {
        get
        {
            var locString = new LocString("relics", Id.Entry + ".extra");
            locString.Add("energyPrefix", EnergyIconHelper.GetPrefix(this));
            return locString;
        }
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }

    private readonly HashSet<CardModel> _affectedCards = new();

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature?.CombatState == null) return;
        var candidates =
            CombatManager.Instance.History.Entries
                .OfType<CardDrawnEntry>()
                .Where(e => e.HappenedThisTurn(player.Creature.CombatState) && e.Card.Owner == player)
                .Select(x => x.Card)
                .Where(c => c.Affliction == null)
                .ToList();

        if (candidates.Count <= 0) return;

        Flash();
        // 随机选两张
        var selected = candidates
            .StableShuffle(player.RunState.Rng.CombatCardSelection)
            .Take(Math.Min(candidates.Count, (int)base.DynamicVars["Amount"].BaseValue))
            .ToList();

        foreach (var card in selected)
        {
            if (card.Affliction != null) continue;
            await CardCmd.Afflict<Weighted>(card, base.DynamicVars.Energy.IntValue);
            _affectedCards.Add(card);
        }
    }

    public override Task AfterTurnEndLate(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == CombatSide.Player)
        {
            foreach (var card in _affectedCards)
            {
                if (!card.IsInCombat) continue;
                if (card.Affliction is not Weighted) continue;
                CardCmd.ClearAffliction(card);
            }
        }

        _affectedCards.Clear();
        return Task.CompletedTask;
    }
}