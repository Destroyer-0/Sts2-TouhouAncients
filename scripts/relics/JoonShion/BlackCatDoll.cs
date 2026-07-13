using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TouhouAncients.Scripts.Afflictions;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 黑猫玩偶：每回合获得1费；每回合开始时，随机一张手牌被侵蚀为抵押物。
/// 抵押物：无法被打出并获得保留，在手中累计停留2回合后移除此侵蚀。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BlackCatDoll : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new CardsVar(1),
        new DynamicVar("CollateralNum", 2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new[] { HoverTipFactory.ForEnergy(this) }.Concat(
                HoverTipFactory.FromAffliction<TouhouAncientsCollateral>(base.DynamicVars["CollateralNum"].IntValue))
            .Append(HoverTipFactory.FromKeyword(CardKeyword.Retain))
            .Append(HoverTipFactory.FromKeyword(CardKeyword.Unplayable));


    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.PlayerCombatState == null) return;

        Flash();

        // 随机一张手牌被侵蚀为抵押物
        var hand = player.PlayerCombatState.Hand;
        if (hand.IsEmpty || hand.Cards.Count(x => x.Affliction is null) == 0) return;

        var rng = player.RunState.Rng.CombatCardSelection;
        var targetCard = hand.Cards.Where(x => x.Affliction is null).TakeRandom(base.DynamicVars.Cards.IntValue, rng).FirstOrDefault();
        if (targetCard == null) return;

        // 添加抵押物侵蚀
        await CardCmd.AfflictAndPreview<TouhouAncientsCollateral>([targetCard], base.DynamicVars["CollateralNum"].IntValue);
    }
}