using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 亚空穴：每场战斗你首次失去生命值时，获得2层无实体。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SubspaceHole : TouhouAncientRelics
{
    private bool _usedThisCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<IntangiblePower>(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<IntangiblePower>()
    ];

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner.Creature) return;
        if (!CombatManager.Instance.IsInProgress) return;
        if (result.UnblockedDamage <= 0) return;
        if (_usedThisCombat) return;

        _usedThisCombat = true;
        Flash();
        await PowerCmd.Apply<IntangiblePower>(target, base.DynamicVars["IntangiblePower"].BaseValue, target, null);
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _usedThisCombat = false;
        return Task.CompletedTask;
    }
}
