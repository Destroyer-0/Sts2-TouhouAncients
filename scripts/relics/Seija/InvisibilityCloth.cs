using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 隐身布：战斗开始时，获得壁垒与15格挡。首次受伤时，失去壁垒进入昏眩。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class InvisibilityCloth : TouhouAncientRelics
{
    private bool _firstDamageTaken;
    //private bool _triggerRingingPowerNextRound;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BarricadePower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromPower<RingingPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(15m, ValueProp.Unblockable)];

    public override Task BeforeCombatStart()
    {
        _firstDamageTaken = false;
        //_triggerRingingPowerNextRound = false;
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStartLate()
    {
        if (base.Owner?.Creature?.CombatState == null) return;
        Flash();
        base.Status = RelicStatus.Active;

        // 给予壁垒状态
        await PowerCmd.Apply<BarricadePower>(base.Owner.Creature, 1m, base.Owner.Creature, null);

        // 获得15格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, 15m, ValueProp.Unpowered, null, fast: false);

        await PowerCmd.Apply<DexterityPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
        _firstDamageTaken = false;
    }

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner?.Creature) return;
        if (target.CombatState == null) return;
        if (_firstDamageTaken) return;
        if (result.UnblockedDamage <= 0) return;
        if (props.HasFlag(ValueProp.Unblockable)) return;

        _firstDamageTaken = true;
        //_triggerRingingPowerNextRound = true;

        Flash();
        // 失去壁垒状态
        var barricade = target.GetPower<BarricadePower>();
        if (barricade != null)
        {
            await PowerCmd.Remove(barricade);
        }

        await PowerCmd.Apply<RingingPower>(target, 1m, target, null);
    }
}