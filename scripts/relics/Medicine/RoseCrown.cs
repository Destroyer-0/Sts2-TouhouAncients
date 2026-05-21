using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 蔷薇皇冠：
/// 在每场战斗开始时，获得 ThornsAmount 层荆棘与 PlatingAmount 层覆甲。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RoseCrown : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ThornsAmount", 5),
        new DynamicVar("PlatingAmount", 5)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => 
    [
        HoverTipFactory.FromPower<ThornsPower>(),
        HoverTipFactory.FromPower<PlatingPower>(),
    ];

    public override async Task BeforeCombatStart()
    {
        var creature = base.Owner.Creature;
        if (creature?.CombatState == null) return;

        Flash();

        // ThornsAmount 层荆棘
        await PowerCmd.Apply<ThornsPower>(creature, base.DynamicVars["ThornsAmount"].BaseValue, creature, null);

        // PlatingAmount 层覆甲
        await PowerCmd.Apply<PlatingPower>(creature, base.DynamicVars["PlatingAmount"].BaseValue, creature, null);
    }
}
