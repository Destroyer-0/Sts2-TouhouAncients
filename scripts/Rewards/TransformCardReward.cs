using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.Rewards;

/// <summary>
/// 自定义奖励：变化牌组中的牌，默认最多 1 张。描述 Key 为 TOUHOUANCIENTS-TRANSFORM_CARD_REWARD。
/// </summary>
public sealed class TransformCardReward : TouhouCustomReward
{
    /// <summary>本奖励专属 RewardType，值由 BaseLib 的 [CustomEnum] 机制自动生成并注册读档工厂。</summary>
    [CustomEnum(null)]
    public static RewardType TransformCard;

    /// <summary>最多变化几张牌。</summary>
    public int Amount { get; init; } = 1;

    public TransformCardReward(Player player)
        : base(player)
    {
    }

    /// <inheritdoc />
    protected override RewardType RewardType => TransformCard;

    /// <summary>复用原版卡牌奖励图标。</summary>
    protected override string? RewardIconPath => ImageHelper.GetImagePath("ui/reward_screen/reward_icon_card.png");

    /// <inheritdoc />
    public override LocString Description => LocalizedDescription(("cards", Amount));

    /// <inheritdoc />
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

    /// <summary>借 GoldAmount 保存张数，否则读档后数量会被重置。</summary>
    public override SerializableReward ToSerializable() => new()
    {
        RewardType = TransformCard,
        GoldAmount = Amount,
    };

    /// <summary>读档重建。</summary>
    public static TransformCardReward CreateFromSerializable(SerializableReward save, Player player) => new(player)
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
        // 可取消：0~Amount，取消即不变化，奖励按钮保留。
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 0, Amount)
        {
            Cancelable = true,
        };

        var selected = (await CardSelectCmd.FromDeckForTransformation(Player, prefs)).ToList();
        if (selected.Count == 0)
        {
            return false;
        }

        foreach (var card in selected)
        {
            var newCard = CardFactory.CreateRandomCardForTransform(card, isInCombat: false, Player.PlayerRng.Transformations);
            await CardCmd.Transform(card, newCard);
        }
        return true;
    }
}
