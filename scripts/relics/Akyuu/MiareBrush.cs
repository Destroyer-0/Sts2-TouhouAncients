using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 御阿礼·笔：拾起时，在接下来的3场战斗结束后，额外获得1组卡牌奖励。
/// 普通/精英/Boss 都算（<see cref="RoomTypeExtensions.IsCombatRoom"/>），计数走 SavedProperty。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class MiareBrush : TouhouAncientRelics
{
    private const int MaxCombats = 3;

    private int _timesUsed;

    /// <summary>本次奖励生成里是否真的加过一组奖励（TryModifyRewards 与 AfterModifyingRewards 配对）。</summary>
    private bool _grantedThisGeneration;

    public override bool IsUsedUp => TimesUsed >= MaxCombats;
    public override bool ShowCounter => !IsUsedUp;
    public override int DisplayAmount => MaxCombats - TimesUsed;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Combats", MaxCombats)];

    [SavedProperty]
    public int TimesUsed
    {
        get => _timesUsed;
        set
        {
            AssertMutable();
            _timesUsed = value;
            InvokeDisplayAmountChanged();
            CheckIfUsedUp();
        }
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (IsUsedUp) return false;
        if (room == null || !room.RoomType.IsCombatRoom()) return false;

        rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
        _grantedThisGeneration = true;
        return true;
    }

    /// <summary>奖励集生成完才算用掉一次；没真的加过就不计数（避免别的修改钩子蹭到计数）。</summary>
    public override Task AfterModifyingRewards()
    {
        if (!_grantedThisGeneration) return Task.CompletedTask;

        _grantedThisGeneration = false;
        TimesUsed++;
        return Task.CompletedTask;
    }

    private void CheckIfUsedUp()
    {
        if (IsUsedUp) base.Status = RelicStatus.Disabled;
    }
}
