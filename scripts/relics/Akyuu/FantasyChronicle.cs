using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;
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
    private const string CardListKey = "CardList";

    private static readonly MethodInfo? GetStarterCardMethod = typeof(ArchaicTooth).GetMethod(
        "GetTranscendenceStarterCard", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(Player)], null);

    private static readonly MethodInfo? GetTransformedCardMethod = typeof(ArchaicTooth).GetMethod(
        "GetTranscendenceTransformedCard", BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(CardModel)], null);

    private List<IHoverTip> _extraHoverTips = [];
    private ModelId? _dustyTomeCard;
    private SerializableCard? _toothCard;
    private bool _prepared;

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
            RefreshCardList();
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
            RefreshCardList();
        }
    }

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 选项过滤阶段一定会带着事件主人调到这里，比 <c>Prep</c> 更可靠地拿到玩家来掷定候选牌。
    /// </summary>
    public override bool CanAppear(Player? player)
    {
        if (!_prepared && IsMutable && player != null) SetupForPlayer(player);
        return true;
    }

    /// <summary>任务牌卡面 + 全部候选牌；选项阶段由 <see cref="SetupForPlayer"/> 备好，缺失时借 Owner 补算。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            // 选项阶段没备上（或读档后字段为空）时，持有者已知就现算一次
            if (!_prepared && IsMutable && base.Owner is { } owner) SetupForPlayer(owner);
            if (_extraHoverTips.Count == 0) UpdateHoverTips();

            return _extraHoverTips;
        }
    }

    // 图鉴/选项预览会拿 canonical 实例，先垫上固定候选，否则 {CardList} 为空
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            StringListVar list = new(CardListKey);
            list.Set(BuildCardNames(null, null));
            return [list];
        }
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _extraHoverTips = [];
        RefreshCardList();
    }

    /// <summary>
    /// 在生成遗物选项时预先掷定两张候选牌，否则选项面板里的预览拿不到数据。
    /// 此刻遗物自身 Owner 还没设置，玩家由调用方（阿求事件）传入。
    /// </summary>
    public void SetupForPlayer(Player player)
    {
        AssertMutable();
        _prepared = true;
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
        _extraHoverTips = [BuildQuestCardTip()];

        if (_dustyTomeCard != null)
            _extraHoverTips.Add(HoverTipFactory.FromCard(SaveUtil.CardOrDeprecated(_dustyTomeCard), upgrade: true));

        if (_toothCard?.Id != null)
            _extraHoverTips.Add(HoverTipFactory.FromCard(
                SaveUtil.CardOrDeprecated(_toothCard.Id), _toothCard.CurrentUpgradeLevel > 0));

        _extraHoverTips.AddRange(HoverTipFactory.FromCardWithCardHoverTips<GensokyoChronicle>());
        _extraHoverTips.AddRange(HoverTipFactory.FromCardWithCardHoverTips<SpireHeroesLegend>());

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

    /// <summary>遗物描述里的候选牌名；AKYUUMOD 是否在场运行时判定。</summary>
    private void RefreshCardList()
        => ((StringListVar)base.DynamicVars[CardListKey]).Set(BuildCardNames(_dustyTomeCard, _toothCard));

    /// <summary>未掷定时两张随机牌为 null，只列固定选项。</summary>
    private static List<string> BuildCardNames(ModelId? dustyTome, SerializableCard? tooth)
    {
        List<string> names = [];

        if (dustyTome != null) names.Add($"[gold]{SaveUtil.CardOrDeprecated(dustyTome).Title}[/gold]");
        if (tooth?.Id != null) names.Add($"[gold]{SaveUtil.CardOrDeprecated(tooth.Id).Title}[/gold]");
        names.Add($"[gold]{ModelDb.Card<GensokyoChronicle>().Title}[/gold]");
        names.Add($"[gold]{ModelDb.Card<SpireHeroesLegend>().Title}[/gold]");
        // 金色会盖掉内层颜色，阿求 mod 的卡只能整项换粉色
        if (FantasyChronicleCard.FindAkyuuCard() is { } modCard) names.Add($"[pink]{modCard.Title}[/pink]");

        return names;
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
