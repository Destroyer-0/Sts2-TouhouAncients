using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 虹光之梦：拾起时，将你的初始遗物替换为先古版本，并将「古老牙齿」所指代的初始卡牌从牌组中移除。
///
/// 与效果相关的三个目标必须在「事件选项生成」阶段就确定下来（见 <see cref="SetupForPlayer"/>），
/// 而不能等到 <see cref="AfterObtained"/>：
/// 事件界面在玩家拾起之前就会渲染遗物描述，而描述中的
/// <c>{StarterRelic.StringValue:cond:...|...}</c> 需要在那之前已经填好 <c>StringValue</c>，
/// 否则只会一直显示兜底的泛化文本。原版 <see cref="TouchOfOrobas"/> 与 <see cref="ArchaicTooth"/>
/// 也是由 Orobas 事件在生成选项时调用各自的 SetupForPlayer，走的正是同一条路。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class IridescentDream : TouhouAncientRelics
{
    private const string StarterRelicKey = "StarterRelic";
    private const string UpgradedRelicKey = "UpgradedRelic";
    private const string StarterCardKey = "StarterCard";

    /// <summary>
    /// 「古老牙齿」所对应的初始卡牌。与原版 <see cref="ArchaicTooth"/> 的 TranscendenceUpgrades 键集合一致
    /// （该字典是私有的，故在此显式列出，以便用同一条规则选取要移除的牌）。
    /// </summary>
    private static ModelId[] StarterCardIds =>
    [
        ModelDb.Card<Bash>().Id,
        ModelDb.Card<Neutralize>().Id,
        ModelDb.Card<Unleash>().Id,
        ModelDb.Card<FallingStar>().Id,
        ModelDb.Card<Dualcast>().Id
    ];

    private List<IHoverTip> _extraHoverTips = new();

    private ModelId? _starterRelic;
    private ModelId? _upgradedRelic;
    private SerializableCard? _starterCard;

    /// <summary>
    /// 要被替换掉的初始遗物。标记为 <c>[SavedProperty]</c>，
    /// 这样读档时 setter 会重新执行，文本与悬停提示也随之重建。
    /// </summary>
    [SavedProperty]
    public ModelId? StarterRelic
    {
        get => _starterRelic;
        set
        {
            _starterRelic = value;
            UpdateText();
        }
    }

    /// <summary>替换成的先古遗物，同样需要保存以便读档后重建文本。</summary>
    [SavedProperty]
    public ModelId? UpgradedRelic
    {
        get => _upgradedRelic;
        set
        {
            _upgradedRelic = value;
            UpdateText();
        }
    }

    /// <summary>要从牌组中移除的初始卡牌。</summary>
    [SavedProperty]
    public SerializableCard? StarterCard
    {
        get => _starterCard;
        set
        {
            _starterCard = value;
            UpdateText();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar(StarterRelicKey),
        new StringVar(UpgradedRelicKey),
        new StringVar(StarterCardKey)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => _extraHoverTips;

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _extraHoverTips = new List<IHoverTip>();
    }

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 在「事件选项生成」阶段预准备数据。
    /// 由 <c>DoremySweetAncient</c> 的选项候选（<c>TARelicOption&lt;IridescentDream&gt;().Prep(...)</c>）在构造时调用，
    /// 对应的就是原版 Orobas 事件里调用 <c>TouchOfOrobas.SetupForPlayer</c> 的时机。
    ///
    /// 注意：此阶段遗物自身的 Owner 尚未设置，
    /// 所以必须由调用方把玩家传进来。
    /// </summary>
    /// <param name="player">正在生成遗物选项的玩家。</param>
    public void SetupForPlayer(Player player)
    {
        // 1) 初始遗物 → 先古版本（复用原版「欧洛巴斯之触」的升级对照表）
        var starterRelic = player.Relics.FirstOrDefault(r => r.Rarity == RelicRarity.Starter);
        if (starterRelic != null)
        {
            var upgraded = ModelDb.Relic<TouchOfOrobas>().GetUpgradedStarterRelic(starterRelic);
            StarterRelic = starterRelic.Id;
            UpgradedRelic = upgraded.Id;
        }

        // 2) 「古老牙齿」所指代的初始卡牌
        StarterCard = player.Deck.Cards
            .FirstOrDefault(c => StarterCardIds.Contains(c.Id))
            ?.ToSerializable();
    }

    /// <summary>
    /// 依据当前已保存的目标重建描述文本与悬停提示。
    /// 三个属性任意一个变化都会走到这里，因此读档后与克隆后都能恢复正确显示。
    /// </summary>
    private void UpdateText()
    {
        _extraHoverTips.Clear();

        if (_starterRelic != null)
        {
            var starterRelic = SaveUtil.RelicOrDeprecated(_starterRelic);
            ((StringVar)base.DynamicVars[StarterRelicKey]).StringValue = starterRelic.Title.GetFormattedText();
            _extraHoverTips.AddRange(starterRelic.HoverTips);
        }

        if (_upgradedRelic != null)
        {
            var upgradedRelic = SaveUtil.RelicOrDeprecated(_upgradedRelic);
            ((StringVar)base.DynamicVars[UpgradedRelicKey]).StringValue = upgradedRelic.Title.GetFormattedText();
            _extraHoverTips.AddRange(upgradedRelic.HoverTips);
        }

        if (_starterCard != null)
        {
            var starterCard = CardModel.FromSerializable(_starterCard);
            ((StringVar)base.DynamicVars[StarterCardKey]).StringValue = starterCard.Title;
            _extraHoverTips.Add(HoverTipFactory.FromCard(starterCard));
        }
    }

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        Flash();

        // 1) 初始遗物 → 先古版本
        // 正常情况下目标已由 SetupForPlayer 在选项生成阶段确定；
        // 若未预准备（例如通过其它途径获得此遗物），则在此现场查找作为兜底。
        var starterRelic = _starterRelic != null
            ? player.GetRelicById(_starterRelic)
            : player.Relics.FirstOrDefault(r => r.Rarity == RelicRarity.Starter);

        if (starterRelic != null)
        {
            var upgradedId = _upgradedRelic
                ?? ModelDb.Relic<TouchOfOrobas>().GetUpgradedStarterRelic(starterRelic).Id;
            await RelicCmd.Replace(starterRelic, ModelDb.GetById<RelicModel>(upgradedId).ToMutable());
        }

        // 2) 「古老牙齿」所指代的初始卡牌 → 从牌组中移除
        var preparedCardId = _starterCard?.Id;
        var starterCard = preparedCardId != null
            ? player.Deck.Cards.FirstOrDefault(c => c.Id == preparedCardId)
            : player.Deck.Cards.FirstOrDefault(c => StarterCardIds.Contains(c.Id));

        if (starterCard != null)
        {
            await CardPileCmd.RemoveFromDeck(starterCard);
        }
    }
}
