using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.CmdUtils;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 幻想万象录：任务牌，进第二幕时二选一变形为「尘封魔典 / 古老牙齿」给出的那张先古牌，
/// 或本 mod / AKYUUMOD 的额外选项。
/// </summary>
[Pool(typeof(EventCardPool))]
public class FantasyChronicleCard : TouhouAncientCards
{
    private const string AkyuuModId = "AKYUUMOD";
    private const string CardListKey = "CardList";

    private static readonly ModelId AkyuuChronicleId = new(AkyuuModId, "GENSOKYO_CHRONICLE");
    private static readonly ModelId AkyuuMissionId = new(AkyuuModId, "MISSION");

    private ModelId? _dustyTomeCard;
    private SerializableCard? _toothCard;
    private bool _offered;

    /// <summary>「尘封魔典」对应的先古牌（拾起时已按角色池掷定）。</summary>
    [SavedProperty]
    public ModelId? DustyTomeCard
    {
        get => _dustyTomeCard;
        set
        {
            AssertMutable();
            _dustyTomeCard = value;
            RefreshCardList();
        }
    }

    /// <summary>「古老牙齿」对应的升华牌；牌组里没有可升华的起手牌时为 null，该选项不出现。</summary>
    [SavedProperty]
    public SerializableCard? ToothCard
    {
        get => _toothCard;
        set
        {
            AssertMutable();
            _toothCard = value;
            RefreshCardList();
        }
    }

    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringListVar(CardListKey)];

    /// <summary>候选牌的卡面提示；AKYUUMOD 是否在场在悬停时才判定，读档换 mod 列表也能跟上。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            List<IHoverTip> tips = [];

            if (_dustyTomeCard != null)
                tips.Add(HoverTipFactory.FromCard(SaveUtil.CardOrDeprecated(_dustyTomeCard), upgrade: true));

            if (_toothCard?.Id != null)
                tips.Add(HoverTipFactory.FromCard(
                    SaveUtil.CardOrDeprecated(_toothCard.Id), _toothCard.CurrentUpgradeLevel > 0));

            tips.AddRange(HoverTipFactory.FromCardWithCardHoverTips<GensokyoChronicle>());
            tips.AddRange(HoverTipFactory.FromCardWithCardHoverTips<SpireHeroesLegend>());

            if (FindAkyuuCard() is { } modCard) tips.Add(HoverTipFactory.FromCard(modCard));

            return tips;
        }
    }

    public FantasyChronicleCard() : base(-1, CardType.Quest, CardRarity.Quest, TargetType.None, shouldShowInCardLibrary: false)
    {
    }

    protected override void AfterCloned()
    {
        base.AfterCloned();
        // 读档后 AKYUUMOD 是否在场要重新判定，候选名称跟着重算
        RefreshCardList();
    }

    public override async Task AfterActEntered()
    {
        if (_offered) return;
        Player? player = base.Owner;
        if (player?.Creature == null) return;
        if (!PileType.Deck.GetPile(player).Cards.Contains(this)) return;

        List<CardModel> candidates = BuildCandidates(player);
        if (candidates.Count == 0) return;

        // 同一次进幕可能有多个面板（多张本牌、或别的遗物），交给闸门排队
        await ActEntryPanelQueue.Run(player, async () =>
        {
            _offered = true;
            // 候选 4~5 张，FromChooseACardScreen 超过 3 张会抛异常，只能用网格
            CardSelectorPrefs prefs = new(base.SelectionScreenPrompt, 1) { Cancelable = false };
            CardModel? picked = (await CardSelectCmd.FromSimpleGrid(
                new BlockingPlayerChoiceContext(), candidates, player, prefs)).FirstOrDefault();
            await CardCmd.Transform(this, picked);
            if (picked.Id == AkyuuChronicleId) await GrantAkyuuMission(player);
        });
    }

    private List<CardModel> BuildCandidates(Player player)
    {
        List<CardModel> list = [];

        if (_dustyTomeCard != null)
        {
            CardModel dusty = player.RunState.CreateCard(SaveUtil.CardOrDeprecated(_dustyTomeCard), player);
            CardCmd.Upgrade(dusty, CardPreviewStyle.None);
            list.Add(dusty);
        }

        if (BuildToothCard(player) is { } tooth) list.Add(tooth);

        list.Add(player.RunState.CreateCard<GensokyoChronicle>(player));
        list.Add(player.RunState.CreateCard<SpireHeroesLegend>(player));

        if (FindAkyuuCard() is { } modCard) list.Add(player.RunState.CreateCard(modCard, player));

        return list;
    }

    private CardModel? BuildToothCard(Player player)
    {
        if (_toothCard?.Id == null) return null;

        CardModel card = player.RunState.CreateCard(SaveUtil.CardOrDeprecated(_toothCard.Id), player);
        for (int i = 0; i < _toothCard.CurrentUpgradeLevel; i++) CardCmd.Upgrade(card, CardPreviewStyle.None);
        return card;
    }

    /// <summary>AKYUUMOD 在场时才取它的卡；每次判定都重查 mod 列表，读档换 mod 也能跟上。</summary>
    internal static CardModel? FindAkyuuCard() =>
        IsAkyuuModLoaded() ? ModelDb.GetByIdOrNull<CardModel>(AkyuuChronicleId) : null;

    private static bool IsAkyuuModLoaded() =>
        ModManager.Mods.Any(mod => string.Equals(mod.manifest?.id, AkyuuModId) && mod.state == ModLoadState.Loaded);

    private static async Task GrantAkyuuMission(Player player)
    {
        if (ModelDb.GetByIdOrNull<RelicModel>(AkyuuMissionId) is not { } relic) return;
        if (player.Relics.Any(owned => owned.Id == AkyuuMissionId)) return;

        await RelicCmd.Obtain(relic.ToMutable(), player);
    }

    private void RefreshCardList()
    {
        List<string> names = [];

        if (_dustyTomeCard != null) names.Add(SaveUtil.CardOrDeprecated(_dustyTomeCard).Title);
        if (_toothCard?.Id != null) names.Add(SaveUtil.CardOrDeprecated(_toothCard.Id).Title);
        names.Add(ModelDb.Card<GensokyoChronicle>().Title);
        names.Add(ModelDb.Card<SpireHeroesLegend>().Title);
        if (FindAkyuuCard() is { } modCard) names.Add(modCard.Title);

        ((StringListVar)base.DynamicVars[CardListKey]).Set(names);
    }
}
