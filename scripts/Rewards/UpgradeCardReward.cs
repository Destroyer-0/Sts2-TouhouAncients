using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.Rewards;

/// <summary>
/// 自定义奖励：选牌升级，默认 1 张。描述 Key 为基类默认值 TOUHOUANCIENTS-UPGRADE_CARD_REWARD。
/// </summary>
public sealed class UpgradeCardReward : TouhouCustomReward
{
    /// <summary>本奖励专属 RewardType，值由 BaseLib 的 [CustomEnum] 机制自动生成并注册读档工厂。</summary>
    [CustomEnum(null)]
    public static RewardType UpgradeCard;

    /// <summary>最多升级几张牌。</summary>
    public int Amount { get; init; } = 1;

    public UpgradeCardReward(Player player)
        : base(player)
    {
    }

    /// <inheritdoc />
    protected override RewardType RewardType => UpgradeCard;

    /// <summary>复用原版休息处锻造图标：语义一致，且必然是已有资源。</summary>
    protected override string? RewardIconPath => ImageHelper.GetImagePath("ui/reward/reward_upgrade.png");

    /// <inheritdoc />
    public override LocString Description => LocalizedDescription(("cards", Amount));

    /// <inheritdoc />
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

    /// <summary>借 GoldAmount 保存 <see cref="Amount"/>，否则读档后数量会被重置。</summary>
    public override SerializableReward ToSerializable() => new()
    {
        RewardType = UpgradeCard,
        GoldAmount = Amount,
    };

    /// <summary>读档重建。</summary>
    public static UpgradeCardReward CreateFromSerializable(SerializableReward save, Player player) => new(player)
    {
        Amount = save.GoldAmount > 0 ? save.GoldAmount : 1,
    };

    /// <inheritdoc />
    public override void MarkContentAsSeen()
    {
        // 无图鉴内容需要标记。
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSelect()
    {
        // 复用原版升级选牌界面（可取消、需手动确认）。
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1, Amount)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };

        var cards = (await CardSelectCmd.FromDeckForUpgrade(Player, prefs)).ToList();
        if (cards.Count == 0)
        {
            // 取消或没有可升级的牌：保留奖励按钮。
            return false;
        }

        CardCmd.Upgrade(cards, CardPreviewStyle.GridLayout);
        return true;
    }

    /// <summary>加入当前战斗的结算奖励；不在战斗房间返回 false。</summary>
    public static bool AddAsExtraReward(Player player, int amount = 1)
    {
        if (player.RunState.CurrentRoom is not CombatRoom combatRoom)
        {
            return false;
        }
        combatRoom.AddExtraReward(player, new UpgradeCardReward(player) { Amount = amount });
        return true;
    }

    /// <summary>单独弹出只含本奖励的界面（事件/遗物用），仅对本地玩家生效。</summary>
    public static async Task Offer(Player player, int amount = 1)
    {
        await RewardsCmd.OfferCustom(player, new List<Reward> { new UpgradeCardReward(player) { Amount = amount } });
    }
}
