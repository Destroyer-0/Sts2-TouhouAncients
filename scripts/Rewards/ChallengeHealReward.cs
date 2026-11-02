using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.Rewards;

/// <summary>
/// 挑战胜利奖励：恢复已损失生命值的 35%。固定出现（满血时为 0），可跳过。
/// </summary>
public sealed class ChallengeHealReward : TouhouCustomReward
{
    /// <summary>本奖励专属 RewardType，值由 BaseLib 的 [CustomEnum] 机制自动生成并注册读档工厂。</summary>
    [CustomEnum(null)]
    public static RewardType ChallengeHeal;

    /// <summary>回血比例。本地化文本把 35% 写死，改这里要同步改三语 json。</summary>
    public const decimal HealRatio = 0.35m;

    /// <summary>-1 = 尚未 Populate。显示与实治共用这份，避免二次计算偏差。</summary>
    public int HealAmount { get; private set; } = -1;

    public ChallengeHealReward(Player player)
        : base(player)
    {
    }

    protected override RewardType RewardType => ChallengeHeal;

    /// <summary>本体资源：res://images/ui/reward/reward_heal.png。</summary>
    protected override string? RewardIconPath => ImageHelper.GetImagePath("ui/reward/reward_heal.png");

    /// <inheritdoc />
    public override LocString Description => LocalizedDescription(("heal", HealAmount));

    /// <summary>本奖励由 TryModifyRewards 追加，晚于 Regular 的 Populate，故须自己声明未填充。</summary>
    public override bool IsPopulated => HealAmount >= 0;

    /// <inheritdoc />
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

    /// <summary>无自定义数据要存：读档后按当时生命重算。</summary>
    public override SerializableReward ToSerializable() => new() { RewardType = ChallengeHeal };

    /// <summary>读档重建。</summary>
    public static ChallengeHealReward CreateFromSerializable(SerializableReward save, Player player) => new(player);

    /// <inheritdoc />
    public override void Populate()
    {
        var creature = Player.Creature;
        // 与 Creature.SetCurrentHpInternal 一致用截断取整，显示值 == 实际治疗量。
        HealAmount = (int)((creature.MaxHp - creature.CurrentHp) * HealRatio);
    }

    /// <inheritdoc />
    public override void MarkContentAsSeen()
    {
        // 无图鉴内容需要标记。
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSelect()
    {
        if (HealAmount > 0)
        {
            await CreatureCmd.Heal(Player.Creature, HealAmount);
        }
        // 满血也返回 true，否则按钮留在界面上无法离开。
        return true;
    }
}
