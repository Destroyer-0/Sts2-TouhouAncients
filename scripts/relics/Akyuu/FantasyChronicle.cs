using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 幻想万象录：拾起时把任务牌「幻想万象录」放进牌组，进第二幕时可变形为
/// 尘封魔典 / 古老牙齿给出的先古牌，或幻想乡缘起 / 高塔英雄传（有 AKYUUMOD 时再加其卡）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class FantasyChronicle : TouhouAncientRelics
{
    private static readonly MethodInfo? GetStarterCardMethod = typeof(ArchaicTooth).GetMethod(
        "GetTranscendenceStarterCard", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(Player)], null);

    private static readonly MethodInfo? GetTransformedCardMethod = typeof(ArchaicTooth).GetMethod(
        "GetTranscendenceTransformedCard", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(CardModel)], null);

    private List<IHoverTip> _extraHoverTips = [];
    private ModelId? _dustyTomeCard;
    private SerializableCard? _toothCard;

    /// <summary>「尘封魔典」对应的先古牌；<see cref="SavedProperty"/> 让读档时 setter 重跑、重建提示。</summary>
    [SavedProperty]
    public ModelId? DustyTomeCard
    {
        get => _dustyTomeCard;
        set
        {
            AssertMutable();
            _dustyTomeCard = value;
            UpdateHoverTips();
        }
    }

    /// <summary>「古老牙齿」对应的升华牌；牌组里没有可升华的起手牌时为 null。</summary>
    [SavedProperty]
    public SerializableCard? ToothCard
    {
        get => _toothCard;
        set
        {
            AssertMutable();
            _toothCard = value;
            UpdateHoverTips();
        }
    }

    public override bool HasUponPickupEffect => true;

    /// <summary>任务牌卡面 + 全部候选牌；选项阶段由 <see cref="SetupForPlayer"/> 备好，读档后缺数据时惰性重建。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (_extraHoverTips.Count == 0) UpdateHoverTips();
            return _extraHoverTips;
        }
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _extraHoverTips = [];
    }

    /// <summary>
    /// 在生成遗物选项时预先掷定两张候选牌，否则选项面板里的预览拿不到数据。
    /// 此刻遗物自身 Owner 还没设置，玩家由调用方（阿求事件）传入。
    /// </summary>
    public void SetupForPlayer(Player player)
    {
        AssertMutable();
        DustyTomeCard = PickDustyTomeCard(player);
        ToothCard = ResolveToothCard(player);
    }

    public override async Task AfterObtained()
    {
        Player? player = base.Owner;
        if (player == null) return;

        Flash();
        FantasyChronicleCard card = player.RunState.CreateCard<FantasyChronicleCard>(player);
        // 选项阶段已掷定；经由其它途径获得时才现场补算
        card.DustyTomeCard = _dustyTomeCard ?? PickDustyTomeCard(player);
        card.ToothCard = _toothCard ?? ResolveToothCard(player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }

    private void UpdateHoverTips()
    {
        _extraHoverTips =
        [
            BuildQuestCardTip(),
            .. HoverTipFactory.FromCardWithCardHoverTips<GensokyoChronicle>(),
            .. HoverTipFactory.FromCardWithCardHoverTips<SpireHeroesLegend>()
        ];

        if (FantasyChronicleCard.FindAkyuuCard() is { } modCard) _extraHoverTips.Add(HoverTipFactory.FromCard(modCard));
    }

    /// <summary>canonical 的任务牌 CardList 是空的，必须用填好数据的克隆才能显示完整描述。</summary>
    private IHoverTip BuildQuestCardTip()
    {
        var card = (FantasyChronicleCard)ModelDb.Card<FantasyChronicleCard>().MutableClone();
        card.DustyTomeCard = _dustyTomeCard;
        card.ToothCard = _toothCard;
        return HoverTipFactory.FromCard(card);
    }

    /// <summary>照原版尘封魔典：当前角色已解锁的 Ancient 稀有度牌，排除升华用的那几张。</summary>
    private static ModelId? PickDustyTomeCard(Player player)
    {
        List<CardModel> pool = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == CardRarity.Ancient && !ArchaicTooth.TranscendenceCards.Contains(c))
            .ToList();

        return pool.Count == 0 ? null : pool[player.PlayerRng.Rewards.NextInt(0, pool.Count)].Id;
    }

    /// <summary>
    /// 照「虹光之梦」反射调原版私有方法：牌组里可升华的起手牌 → 对应的升华牌。
    /// BaseLib / RitsuLib 都改的是这两个方法读的表，所以一次覆盖全部来源。
    /// </summary>
    private static SerializableCard? ResolveToothCard(Player player)
    {
        ArchaicTooth canonical = ModelDb.Relic<ArchaicTooth>();
        if (GetStarterCardMethod?.Invoke(canonical, [player]) is not CardModel starter) return null;
        if (GetTransformedCardMethod?.Invoke(canonical, [starter]) is not CardModel transformed) return null;

        return transformed.ToSerializable();
    }
}
