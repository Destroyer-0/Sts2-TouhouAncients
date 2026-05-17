using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Afflictions;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 哔哩电池：每场战斗第一回合开始时，从抽牌堆中选择至多3张能力牌置入手牌，
/// 这些牌本回合费用降至1，并拥有流电（Galvanized）。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class BatteryBili : TouhouAncientRelics
{
    private bool _activated;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Amount",3m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromAffliction<Galvanized>(6);

    public override Task BeforeCombatStart()
    {
        _activated = false;
        return Task.CompletedTask;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner) return;
        if (_activated) return;
        if (player.Creature.CombatState == null) return;
        if (player.Creature.CombatState.RoundNumber > 1) return;

        _activated = true;

        // 从抽牌堆中选至多3张能力牌
        var drawPile = PileType.Draw.GetPile(player);

        Flash();
        await CardPileCmd.ShuffleIfNecessary(choiceContext, player);
        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext,
            (from c in PileType.Draw.GetPile(player).Cards
                where c.Type == CardType.Power
                orderby c.Rarity, c.Id
                select c).ToList(), player,
            new CardSelectorPrefs(base.SelectionScreenPrompt, 0, base.DynamicVars["Amount"].IntValue))).ToList();
        await CardPileCmd.Add(selected, PileType.Hand);


        // var powerCards = drawPile.Cards
        //     .Where(c => c.Type == CardType.Power)
        //     .ToList();
        //
        // if (powerCards.Count <= 0) return;


        if (selected.Count <= 0) return;

        foreach (var card in selected)
        {
            // 本回合费用降至1
            card.EnergyCost.SetThisTurn(1, true);

            // 附加流电 Affliction
            await CardCmd.Afflict<Galvanized>(card, 6m);
        }
    }
}