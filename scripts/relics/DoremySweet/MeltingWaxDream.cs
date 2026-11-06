using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 熔蜡之梦：每回合首次失去生命时，若生命值低于50%，获得3层再生。
/// 如果你在一场战斗中通过此方式累计获得9点再生，则此遗物永久失效。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MeltingWaxDream : TouhouAncientRelics
{
    private bool _isExpired;

    /// <summary>此遗物是否已永久失效。由 [SavedProperty] 标记，会随存档保存。</summary>
    [SavedProperty]
    public bool TouhouAncients_Expired
    {
        get=>_isExpired;
        set
        {
            AssertMutable();
            _isExpired = value;
            InvokeDisplayAmountChanged();
            if (IsUsedUp)
            {
                base.Status = RelicStatus.Disabled;
            }
        }
    }

    /// <summary>本场战斗中通过此遗物累计获得的再生点数（仅用于判断是否达到上限）。</summary>
    private int _regenGainedThisCombat;

    /// <summary>永久失效后，遗物会被标记为已用尽。</summary>
    public override bool IsUsedUp => TouhouAncients_Expired;

    public override bool ShowCounter => !IsUsedUp;

    public override int DisplayAmount => _regenGainedThisCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpThreshold", 50),
        new DynamicVar("RegenGain", 3),
        new DynamicVar("RegenLimit", 9)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<RegenPower>()];

    public override Task BeforeCombatStart()
    {
        // 累计点数只在单场战斗内有效，但一旦永久失效就一直是失效状态
        _regenGainedThisCombat = 0;
        base.Status = TouhouAncients_Expired ? RelicStatus.Disabled : RelicStatus.Normal;
        return Task.CompletedTask;
    }

    /// <summary>本回合是否已消耗掉唯一一次判定（一次判定 = 该回合首次失去生命）。</summary>
    private bool _triggeredThisTurn;

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        // 「回合」以玩家回合起点为界，敌人行动与本回合共用一次判定
        if (player == base.Owner)
            _triggeredThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != base.Owner?.Creature) return;
        if (TouhouAncients_Expired) return;
        if (delta >= 0m) return;
        if (_triggeredThisTurn) return;
        if (creature.IsDead) return;

        // 首次失去生命即消耗判定，之后即使未达阈值本回合也不再触发
        _triggeredThisTurn = true;

        var threshold = base.DynamicVars["HpThreshold"].BaseValue;
        var currentPercent = creature.MaxHp > 0
            ? (decimal)creature.CurrentHp / creature.MaxHp * 100m
            : 0m;
        if (currentPercent >= threshold) return;

        var gain = (int)base.DynamicVars["RegenGain"].BaseValue;
        var limit = (int)base.DynamicVars["RegenLimit"].BaseValue;

        Flash();

        await PowerCmd.Apply<RegenPower>(new ThrowingPlayerChoiceContext(), creature, gain, creature, null);

        _regenGainedThisCombat += gain;

        if (_regenGainedThisCombat >= limit)
        {
            // 达到上限 → 永久失效
            TouhouAncients_Expired = true;
            base.Status = RelicStatus.Disabled;
        }
        InvokeDisplayAmountChanged();
    }
}
