using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.Rewards;

/// <summary>
/// 誊写奖励：查看某个角色卡牌池的卡牌奖励，选择一张加入「百鬼夜行」的誊写记录。
/// 选中的牌只写进牌组中每张百鬼夜行的誊写记录，不会加入牌组。
/// 牌组中没有百鬼夜行时不触发（按钮保留，可稍后再点）。
/// </summary>
public sealed class TranscribeReward : TouhouCustomReward
{
    protected override string? RewardIconPath => "res://images/icon/Character/FutatsuiwaMamizou.png";
    /// <summary>本奖励专属 RewardType，值由 BaseLib 的 [CustomEnum] 机制自动生成并注册读档工厂。</summary>
    [CustomEnum(null)] public static RewardType Transcribe;

    /// <summary>一次展示几张该角色的牌供选择，与普通卡牌奖励一致。</summary>
    public const int OptionCount = 3;

    /// <summary>本次誊写的来源角色卡牌池：拾起时是固定五人的池，斩杀奖励是玩家已解锁的其他角色池。</summary>
    public CardPoolModel? Pool { get; init; }

    public TranscribeReward(Player player)
        : base(player)
    {
    }

    /// <inheritdoc />
    protected override RewardType RewardType => Transcribe;

    /// <inheritdoc />
    public override CreateRewardFromSave<CustomReward> DeserializeMethod => CreateFromSerializable;

    /// <summary>
    /// 借 <see cref="SerializableReward.CardPoolIds"/> 保存来源卡池（存档与联机包都始终读写这个字段），
    /// 否则读档后角色会被重置。该方法会被 BaseLib 拿未初始化实例调用，所以不能碰 Player 与实例状态。
    /// </summary>
    public override SerializableReward ToSerializable() => new()
    {
        RewardType = Transcribe,
        CardPoolIds = Pool == null ? [] : [Pool.Id],
    };

    /// <summary>读档重建；卡池已不存在时留空，选择时保留按钮。</summary>
    public static TranscribeReward CreateFromSerializable(SerializableReward save, Player player)
    {
        ModelId? poolId = save.CardPoolIds.Count > 0 ? save.CardPoolIds[0] : null;
        return new TranscribeReward(player)
        {
            Pool = poolId == null ? null : ModelDb.GetByIdOrNull<CardPoolModel>(poolId),
        };
    }

    /// <inheritdoc />
    public override LocString Description => LocalizedDescription(("Character", CharacterTitle));

    /// <summary>奖励按钮的悬浮说明：复用遗物上的「誊写」定义。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var final = new[] { ModelDb.Relic<HyakkiYagyoScroll>().TranscribeTip };
            if (Pool == null)
            {
                return final;
            }

            var cards = DeckHyakkiYagyo
                .SelectMany(x => x.Records)
                .Where(x => HyakkiYagyo.PoolIdOf(x) == Pool.Id).Select(x =>
                {
                    CardModel? model = ModelDb.GetByIdOrNull<CardModel>(x.Id!);
                    return (x, model);
                }).ToList();

            string entry = ModelDb.Relic<HyakkiYagyoScroll>().Id.Entry;
            if (cards.Count == 0)
            {
                return final.Append(new HoverTip(new LocString("relics", entry + ".transcribedTitle"), new LocString("relics", entry + ".transcribedDescriptionEmpty")));
            }

            LocString description = new("relics", entry + ".transcribedDescription");
            description.Add("Cards", cards.Select(card => card.model.Title).ToList());

            return final
                .Append(new HoverTip(new LocString("relics", entry + ".transcribedTitle"), description))
                .Concat(cards.Select(pair => HoverTipFactory.FromCard(pair.model!, pair.x.CurrentUpgradeLevel > 0)));
        }
    }

    /// <summary>来源角色的本地化名；按卡池反查角色，反查不到时退回卡池名。</summary>
    private string CharacterTitle
    {
        get
        {
            if (Pool == null) return string.Empty;

            CharacterModel? owner = ModelDb.AllCharacters.FirstOrDefault(character => character.CardPool.Id == Pool.Id);
            return owner != null ? owner.Title.GetFormattedText() : Pool.Title;
        }
    }

    /// <inheritdoc />
    public override void MarkContentAsSeen()
    {
        // 无图鉴内容需要标记。
    }

    /// <inheritdoc />
    protected override async Task<bool> OnSelect()
    {
        CardPoolModel? pool = Pool;
        // 卡池取不到（存档里的角色已不存在）：不触发誊写，保留奖励按钮。
        if (pool == null) return false;

        List<HyakkiYagyo> targets = DeckHyakkiYagyo.ToList();
        // 牌组中没有百鬼夜行：不触发誊写，保留奖励按钮。
        if (targets.Count == 0) return false;

        List<CardCreationResult> options =
            CardFactory.CreateForReward(Player, OptionCount, BuildCreationOptions(pool)).ToList();
        if (options.Count == 0) return false;

        List<CardModel> candidates = options.Select(option => option.Card).ToList();
        CardModel? picked = await CardSelectCmd.FromChooseACardScreen(
            new BlockingPlayerChoiceContext(), candidates, Player, canSkip: true);

        if (picked == null) return false;

        foreach (HyakkiYagyo target in targets)
        {
            target.Record(pool, picked);
        }

        CardCmd.Preview(picked);
        return true;
    }

    /// <summary>牌组中的百鬼夜行（誊写记录的持有者）。</summary>
    private IEnumerable<HyakkiYagyo> DeckHyakkiYagyo =>
        PileType.Deck.GetPile(Player).Cards.OfType<HyakkiYagyo>();

    /// <summary>
    /// 来源角色卡池的卡牌奖励选项。
    /// 禁止其他遗物扩大卡池，保证奖励确实来自该角色。
    /// </summary>
    private CardCreationOptions BuildCreationOptions(CardPoolModel pool)
    {
        var options = new CardCreationOptions(
            new[] { pool },
            CardCreationSource.Other,
            CardRarityOddsType.RegularEncounter);
        return options.WithFlags(CardCreationFlags.NoCardPoolModifications);
    }
}