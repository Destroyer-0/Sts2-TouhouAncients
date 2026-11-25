using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 深秘之梦：在每场战斗的第 N 回合开始时，从 3 张随机塔罗牌中选择一张加入你的手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MysteriousDream : TouhouAncientRelics
{
    private const int OfferCount = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Turns", 4)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Tarot)];

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (player.PlayerCombatState?.TurnNumber != base.DynamicVars["Turns"].IntValue) return;

        Flash();

        // TarotCards.All 是写死的固定顺序表，联机各端 UnstableShuffle 结果一致
        List<CardModel> options = TarotCards.All
            .UnstableShuffle(player.RunState.Rng.CombatCardSelection)
            .Where(card => card.IsAllowedInRun(player.RunState))
            .Take(OfferCount)
            .Select(card => combatState.CreateCard(card, player))
            .ToList();
        if (options.Count == 0) return;

        CardModel? selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext, options, player, canSkip: true);
        if (selected == null) return;

        await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, creator: base.Owner);
    }
}
