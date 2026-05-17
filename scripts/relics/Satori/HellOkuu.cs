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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 地狱鸦羽：在每个回合开始时获得1能量。如果你结束回合时能量为0，将一张灼伤置入抽牌堆，并获得2力量。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HellOkuu : TouhouAncientRelics
{
    private bool _endedWithZeroEnergy;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Strength", 2m), new EnergyVar(1)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Burn>().Append(HoverTipFactory.FromPower<StrengthPower>());

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player) return;
        if (base.Owner.PlayerCombatState == null) return;
        if (base.Owner.PlayerCombatState.Energy > 0) return;
        Flash();
        await CardPileCmd.AddToCombatAndPreview<Burn>(base.Owner.Creature, PileType.Draw, 1, true);
        await PowerCmd.Apply<StrengthPower>(base.Owner.Creature, base.DynamicVars["Strength"].BaseValue,
            base.Owner.Creature, null);
        await Cmd.Wait(0.5f);
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }
}