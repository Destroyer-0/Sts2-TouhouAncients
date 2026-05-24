using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class SoulButterfly : TouhouAncientRelics
{
    private int _dormantRemaining;
    private bool _wasUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars => [ new DynamicVar("ReviveHp", 5m), new DynamicVar("IntangibleAmount", 3m), new DynamicVar("DormantTurns", 8m) ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<IntangiblePower>()];

    public override bool ShowCounter => _dormantRemaining > 0;

    public override int DisplayAmount => _dormantRemaining;

    /// <summary>
    /// 在死亡判定之前拦截，阻止死亡
    /// </summary>
    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner.Creature) return true;
        if (_dormantRemaining > 0) return true;  // 休眠中，正常死亡
        if (_wasUsed) return true;               // 已用过，正常死亡
        return false;                            // 阻止死亡，触发 AfterPreventingDeath
    }

    /// <summary>
    /// 阻止死亡后触发：回血 + 无实体 + 进入休眠
    /// </summary>
    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        _wasUsed = true;
        await CreatureCmd.Heal(creature, base.DynamicVars["ReviveHp"].BaseValue);
        await PowerCmd.Apply<IntangiblePower>(creature, base.DynamicVars["IntangibleAmount"].BaseValue, creature, null);
        _dormantRemaining = (int)base.DynamicVars["DormantTurns"].BaseValue;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return Task.CompletedTask;
        if (_dormantRemaining > 0)
        {
            _dormantRemaining--;
            InvokeDisplayAmountChanged();
        }
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        _dormantRemaining = 0;
        _wasUsed = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
