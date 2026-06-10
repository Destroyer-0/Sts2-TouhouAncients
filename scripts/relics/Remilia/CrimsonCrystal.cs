using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 绯红晶石：
/// 在你的回合开始时，如果你的生命值不低于50%，获得1能量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CrimsonCrystal : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpThreshold", 50),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.ForEnergy(this)];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var creature = player.Creature;
        if (creature.MaxHp <= 0) return;

        decimal hpPercent = creature.CurrentHp * 100m / creature.MaxHp;
        if (hpPercent >= base.DynamicVars["HpThreshold"].BaseValue)
        {
            Flash();
            await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, player);
        }
    }
}