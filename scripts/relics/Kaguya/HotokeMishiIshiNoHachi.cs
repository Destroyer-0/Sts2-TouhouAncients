using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 佛御石之钵：拾起时，失去30最大生命；战斗开始时，获得5敏捷与30格挡。
/// 当你受到伤害后，下回合获得10格挡。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HotokeMishiIshiNoHachi : TouhouAncientRelics
{
    private const decimal MaxHpLoss = 30m;
    private const decimal DexterityAmount = 5m;
    private const decimal StartBlock = 30m;
    private const decimal PostDamageBlock = 10m;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MaxHpLoss", MaxHpLoss),
        new DynamicVar("Dexterity", DexterityAmount),
        new DynamicVar("StartBlock", StartBlock),
        new DynamicVar("PostDamageBlock", PostDamageBlock),
    ];

    public override async Task AfterObtained()
    {
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), base.Owner.Creature, MaxHpLoss, isFromCard: false);
    }

    /// <summary>
    /// 战斗开始时，获得5敏捷与30格挡。
    /// </summary>
    public override async Task BeforeCombatStartLate()
    {
        var creature = base.Owner?.Creature;
        if (creature == null) return;

        await PowerCmd.Apply<DexterityPower>(creature, DexterityAmount, creature, null);
        await CreatureCmd.GainBlock(creature, StartBlock, this);
    }

    /// <summary>
    /// 受到伤害后，下回合获得10格挡。
    /// </summary>
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner?.Creature) return;
        if (result.TotalDamage <= 0) return;

        Flash();
        await PowerCmd.Apply<BlockNextTurnPower>(target, PostDamageBlock, target, null);
    }
}
