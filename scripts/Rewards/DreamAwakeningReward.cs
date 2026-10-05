using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.Rewards;

/// <summary>
/// 「梦醒时」：把一张梦境牌变化成另外两种梦境之一，继承其升级等级与附魔。
/// </summary>
public sealed class DreamAwakeningReward : TouhouCustomReward
{
    /// <summary>本奖励专属 RewardType，值由 BaseLib 的 [CustomEnum] 机制自动生成并注册读档工厂。</summary>
    [CustomEnum(null)]
    public static RewardType DreamAwakening;

    /// <summary>源牌是哪一种梦境（1 幻梦无垠 / 2 深梦无觉 / 3 噩梦无终）。读档后据此在牌组里找回牌。</summary>
    private readonly int _kind;

    /// <summary>内存中的源牌实例；读档后为 null，退回按 <see cref="_kind"/> 查找。</summary>
    private readonly CardModel? _source;

    public DreamAwakeningReward(Player player, CardModel source)
        : base(player)
    {
        _source = source;
        _kind = KindOf(source);
    }

    private DreamAwakeningReward(Player player, int kind)
        : base(player)
    {
        _kind = kind;
    }

    /// <inheritdoc />
    protected override RewardType RewardType => DreamAwakening;

    /// <summary>复用原版「特定卡牌」奖励图标。</summary>
    protected override string? RewardIconPath => ImageHelper.GetImagePath("ui/reward_screen/reward_icon_special_card.png");

    /// <inheritdoc />
    public override LocString Description => LocalizedDescription(("Card", ResolveSource()?.Title));

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips => TargetsOf(_kind).Select(c => HoverTipFactory.FromCard(c));

    /// <inheritdoc />
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

    /// <summary>借 GoldAmount 保存梦境种类，否则读档后不知道要变化哪张牌。</summary>
    public override SerializableReward ToSerializable() => new()
    {
        RewardType = DreamAwakening,
        GoldAmount = _kind,
    };

    /// <summary>读档重建。</summary>
    public static DreamAwakeningReward CreateFromSerializable(SerializableReward save, Player player) =>
        new(player, save.GoldAmount);

    /// <inheritdoc />
    public override void MarkContentAsSeen()
    {
        // 无图鉴内容需要标记。
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSelect()
    {
        var source = ResolveSource();
        if (source == null) return false;

        // 先把升级与附魔继承到候选上，玩家在界面上看到的就是变化后的实际结果
        var candidates = TargetsOf(_kind)
            .Select(target => Inherit(source, Player.RunState.CreateCard(target, Player)))
            .ToList();

        var picked = await CardSelectCmd.FromChooseACardScreen(new BlockingPlayerChoiceContext(), candidates,
            Player, canSkip: true);
        // 跳过即不变化，奖励保留
        if (picked == null) return false;

        await CardCmd.Transform(source, picked);
        return true;
    }

    /// <summary>内存实例已不在牌堆时（读档），按梦境种类在牌组里找回同名牌。</summary>
    private CardModel? ResolveSource()
    {
        if (_source is { Pile: not null }) return _source;

        var id = IdOfKind(_kind);
        return id == ModelId.none ? null : Player.Deck.Cards.FirstOrDefault(c => c.Id == id);
    }

    /// <summary>照搬原版「利爪」变化为「撕咬」的做法：继承升级等级与附魔，附魔不适用时跳过。</summary>
    private static CardModel Inherit(CardModel from, CardModel to)
    {
        if (from.IsUpgraded && to.IsUpgradable) CardCmd.Upgrade(to);

        if (from.Enchantment != null)
        {
            var enchantment = (EnchantmentModel)from.Enchantment.MutableClone();
            if (enchantment.CanEnchant(to)) CardCmd.Enchant(enchantment, to, enchantment.Amount);
        }

        return to;
    }

    // 梦境三牌的互相变化表：1 幻梦无垠、2 深梦无觉、3 噩梦无终
    private static int KindOf(CardModel card) =>
        card.Id == ModelDb.Card<IllusoryDreamWhisper>().Id ? 1 :
        card.Id == ModelDb.Card<DeepDreamSlumber>().Id ? 2 :
        card.Id == ModelDb.Card<EndlessNightmare>().Id ? 3 : 0;

    private static ModelId IdOfKind(int kind) => kind switch
    {
        1 => ModelDb.Card<IllusoryDreamWhisper>().Id,
        2 => ModelDb.Card<DeepDreamSlumber>().Id,
        3 => ModelDb.Card<EndlessNightmare>().Id,
        _ => ModelId.none,
    };

    private static IReadOnlyList<CardModel> TargetsOf(int kind) => kind switch
    {
        1 => [ModelDb.Card<DeepDreamSlumber>(), ModelDb.Card<EndlessNightmare>()],
        2 => [ModelDb.Card<IllusoryDreamWhisper>(), ModelDb.Card<EndlessNightmare>()],
        3 => [ModelDb.Card<IllusoryDreamWhisper>(), ModelDb.Card<DeepDreamSlumber>()],
        _ => [],
    };
}
