using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 彗星加速器：在每个回合开始时，额外抽2张牌，并将2张晕眩加入弃牌堆。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class CometAccelerator : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DazeNum", 2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Dazed>();

    public override decimal ModifyHandDraw(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + 2m;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner) return;

        Flash();
        await CardPileCmd.AddToCombatAndPreview<Dazed>(base.Owner.Creature, PileType.Discard, DynamicVars["DazeNum"].IntValue, true);
    }
}
