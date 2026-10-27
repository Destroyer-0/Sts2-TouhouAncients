using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 蔷薇之笼：回合结束时按气氛一半获得格挡（上限 14）；在嗨到爆下结束回合则保留到下一回合。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RoseCage : TouhouAncientRelics
{
    private const int MaxBlock = 14;

    private bool _retainBlock;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("MaxBlock", MaxBlock)];

    public override Task BeforeCombatStart()
    {
        _retainBlock = false;
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        var player = base.Owner;
        if (player?.Creature == null) return;
        if (side != player.Creature.Side || !participants.Contains(player.Creature)) return;

        var block = Math.Min(KoishiPerformanceCmd.GetAtmosphere(player) / 2, MaxBlock);
        // 没格挡可保留时不要留标记，否则会误保下一回合
        _retainBlock = block > 0 && KoishiPerformanceCmd.IsHyped(player);

        if (block > 0)
        {
            Flash();
            await CreatureCmd.GainBlock(player.Creature, block, ValueProp.Unpowered, null);
        }
    }

    public override bool ShouldClearBlock(Creature creature)
    {
        if (creature != base.Owner?.Creature) return true;
        return !_retainBlock;
    }

    public override Task AfterPreventingBlockClear(AbstractModel preventer, Creature creature)
    {
        if (this != preventer || creature != base.Owner?.Creature) return Task.CompletedTask;

        _retainBlock = false;
        Flash();
        return Task.CompletedTask;
    }
}
