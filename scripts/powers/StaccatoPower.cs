using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 断奏：每造成 2 点未被格挡的伤害获得 1 气氛，有敌人死亡时获得 14 气氛。
/// </summary>
public sealed class StaccatoPower : TouhouAncientPowerModel
{
    private const int DamagePerAtmosphere = 3;
    private const int AtmosphereGain = 1;
    private const int DeathAtmosphere = 14;

    private int _damageProgress;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("DamagePerAtmosphere", DamagePerAtmosphere),
        new DynamicVar("AtmosphereGain", AtmosphereGain),
        new DynamicVar("DeathAtmosphere", DeathAtmosphere),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientHyped)];

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (base.Owner.Player is not { } player) return;
        if (target.Side != CombatSide.Enemy) return;
        if (!props.IsPoweredAttack()) return;
        if (!TouhouAncientCmd.IsPlayerDamageIncludePet(player, dealer)) return;

        _damageProgress += result.UnblockedDamage;
        while (_damageProgress >= DamagePerAtmosphere)
        {
            _damageProgress -= DamagePerAtmosphere;
            await KoishiPerformanceCmd.GainAtmosphere(choiceContext, player, AtmosphereGain);
        }
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength)
    {
        if (base.Owner.Player is not { } player) return;
        if (creature.Side != CombatSide.Enemy) return;

        Flash();
        await KoishiPerformanceCmd.GainAtmosphere(choiceContext, player, DeathAtmosphere);
    }
}