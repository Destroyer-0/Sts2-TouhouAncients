using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 授业儒冠：每6场战斗，额外掉落1组稀有卡牌奖励。事件战斗不计入，
/// 判定靠 <see cref="CombatRoom.ParentEventId"/>（事件发起的战斗才有值）。
/// 计数与徽标照原版钓鱼竿（<c>DisplayAmount</c> 取模）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class ScholarCrown : TouhouAncientRelics
{
    private const string CombatsKey = "Combats";
    private const int CardCount = 3;

    private int _combatsSeen;

    /// <summary>本次奖励生成里是否真的加过一组奖励（TryModifyRewards 与 AfterModifyingRewards 配对）。</summary>
    private bool _grantedThisGeneration;

    public override bool ShowCounter => true;

    public override int DisplayAmount => CombatsSeen % base.DynamicVars[CombatsKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(CombatsKey, 6m)];

    [SavedProperty]
    public int CombatsSeen
    {
        get => _combatsSeen;
        set
        {
            AssertMutable();
            _combatsSeen = value;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (room.ParentEventId != null) return Task.CompletedTask;

        CombatsSeen++;
        return Task.CompletedTask;
    }

    public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != base.Owner) return false;
        if (room == null || !room.RoomType.IsCombatRoom()) return false;
        if (room is CombatRoom { ParentEventId: not null }) return false;
        if (_combatsSeen < base.DynamicVars[CombatsKey].IntValue) return false;

        Flash();

        var options = CardCreationOptions
            .ForRoom(player, room.RoomType)
            .WithFlags(CardCreationFlags.NoRarityModification)
            .WithFilter(c => c.Rarity == CardRarity.Rare);
        rewards.Add(new CardReward(options, CardCount, player));

        _grantedThisGeneration = true;
        return true;
    }

    /// <summary>奖励集生成完才扣次数，避免别的修改钩子蹭到计数。</summary>
    public override Task AfterModifyingRewards()
    {
        if (!_grantedThisGeneration) return Task.CompletedTask;

        _grantedThisGeneration = false;
        CombatsSeen -= base.DynamicVars[CombatsKey].IntValue;
        return Task.CompletedTask;
    }
}
