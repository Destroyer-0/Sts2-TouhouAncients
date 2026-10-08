using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Common.Rewards.LinkedRewardSet;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.Rewards;

/// <summary>
/// 「梦醒时」三选一里的单个梦：选中后按固定方向对一张幻梦无垠执行一次「幻梦变化」。
/// 三个梦包在 BaseLib 的 <see cref="CustomLinkedRewardSet"/>（Exclusive）里，选一个后其余消失。
/// </summary>
public sealed class DreamChoiceReward : TouhouCustomReward
{
    /// <summary>本奖励专属 RewardType，值由 BaseLib 的 [CustomEnum] 机制自动生成并注册读档工厂。</summary>
    [CustomEnum(null)] public static RewardType DreamChoice;

    /// <summary>绀色狂梦：本次变化中造成伤害必定增加。</summary>
    public const int Indigo = 1;

    /// <summary>蔚蓝愁梦：本次变化中获得格挡必定增加。</summary>
    public const int Azure = 2;

    /// <summary>刈安迷梦：本次变化中生成卡牌数必定增加。</summary>
    public const int Kariyasu = 3;

    /// <summary>被变化的幻梦无垠实例，由 <see cref="CreateSet"/> 创建时传入；读档后为 null。</summary>
    private readonly IllusoryDreamWhisper? _source;

    private readonly int _kind;

    private bool _populated;
    private int _damageDelta;
    private int _blockDelta;
    private int _cardsDelta;

    public DreamChoiceReward(Player player, IllusoryDreamWhisper? source, int kind) : base(player)
    {
        _source = source;
        _kind = kind;
    }

    /// <summary>把三个梦包成一个「只能选一个」的奖励组，整组作用于同一张牌。</summary>
    public static CustomLinkedRewardSet CreateSet(IllusoryDreamWhisper card)
    {
        List<Reward> rewards =
        [
            new DreamChoiceReward(card.Owner, card, Indigo),
            new DreamChoiceReward(card.Owner, card, Azure),
            new DreamChoiceReward(card.Owner, card, Kariyasu),
        ];
        return new CustomLinkedRewardSet(rewards, card.Owner);
    }

    /// <inheritdoc />
    protected override RewardType RewardType => DreamChoice;

    /// <inheritdoc />
    protected override string? RewardIconPath => _kind switch
    {
        Indigo => "res://images/icon/rewards/DreamChoiceIndigo.png",
        Azure => "res://images/icon/rewards/DreamChoiceAzure.png",
        Kariyasu => "res://images/icon/rewards/DreamChoiceKariyasu.png",
        _ => null,
    };

    /// <inheritdoc />
    protected override string DescriptionLocKey => _kind switch
    {
        Indigo => "TOUHOUANCIENTS-DREAM_CHOICE_INDIGO",
        Azure => "TOUHOUANCIENTS-DREAM_CHOICE_AZURE",
        Kariyasu => "TOUHOUANCIENTS-DREAM_CHOICE_KARIYASU",
        _ => DefaultDescriptionLocKey,
    };

    /// <inheritdoc />
    public override LocString Description => LocalizedDescription();

    /// <inheritdoc />
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            List<IHoverTip> tips = [IllusoryDreamWhisper.BubbleIllusionTip];
            // 与实际变化的牌同源，保证悬停看到的牌就是要改的牌
            if (ResolveSource() is { } card) tips.Insert(0, HoverTipFactory.FromCard(card));
            return tips;
        }
    }

    /// <inheritdoc />
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

    /// <summary>借 GoldAmount 保存是哪一个梦，否则读档后不知道要往哪个方向变化。</summary>
    public override SerializableReward ToSerializable() => new()
    {
        RewardType = DreamChoice,
        GoldAmount = _kind,
    };

    /// <summary>读档重建。</summary>
    public static DreamChoiceReward CreateFromSerializable(SerializableReward save, Player player) =>
        new(player, null, save.GoldAmount);

    /// <inheritdoc />
    public override void MarkContentAsSeen()
    {
        // 无图鉴内容需要标记。
    }

    /// <summary>
    /// 奖励组生成时掷出增量（两端同一时点、同一 RNG，消耗一致），<see cref="OnSelect"/> 只读缓存，
    /// 避免「跳过再点」把增量重掷一遍。
    /// </summary>
    public override void Populate()
    {
        if (_populated) return;

        var rng = Player.PlayerRng.Rewards;
        _damageDelta = _kind == Indigo ? rng.NextInt(1, 4) : rng.NextInt(-1, 4);
        _blockDelta = _kind == Azure ? rng.NextInt(1, 4) : rng.NextInt(-1, 4);
        _cardsDelta = _kind == Kariyasu ? 1 : rng.NextInt(-1, 2);
        _populated = true;
    }

    /// <inheritdoc />
    public override bool IsPopulated => _populated;

    /// <inheritdoc />
    protected override Task<bool> OnSelect()
    {
        var card = ResolveSource();
        // 找不到源牌：不变化，保留奖励按钮。
        if (card == null) return Task.FromResult(false);

        card.ApplyDreamChange(_damageDelta, _blockDelta, _cardsDelta);
        CardCmd.Preview(card, 3);
        return Task.FromResult(true);
    }

    /// <summary>优先用创建时传入的实例；读档后实例失效，退回牌组里第一张幻梦无垠（多张时只能认第一张）。</summary>
    private IllusoryDreamWhisper? ResolveSource()
    {
        if (_source is { Pile: not null }) return _source;
        return PileType.Deck.GetPile(Player).Cards.OfType<IllusoryDreamWhisper>().FirstOrDefault();
    }
}