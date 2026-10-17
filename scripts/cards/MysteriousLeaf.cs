using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BaseLib.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 迷之树叶：消耗。费用等同于其伪装牌。打出时，打出其伪装牌的复制。升级后获得保留。
/// 每当你将一张牌加入你的牌组时，此牌伪装为该牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class MysteriousLeaf : TouhouAncientCards
{
    /// <summary>星费缓存标志与变更事件（星费无公开写入口，只能反射）。</summary>
    private static readonly FieldInfo? StarCostSetField = AccessTools.Field(typeof(CardModel), "_starCostSet");

    private static readonly FieldInfo? StarCostChangedField = AccessTools.Field(typeof(CardModel), "StarCostChanged");

    public override string? Author => "てらじん";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    /// <summary>伪装记录（0 或 1 条）。只有牌组里那一张是持有者，战斗副本读牌组原件那一份。</summary>
    private List<SerializableCard> _disguise = [];

    [SavedProperty]
    private List<SerializableCard> MysteriousLeaf_Disguise
    {
        get => _disguise;
        set
        {
            AssertMutable();
            _disguise.Clear();
            _disguise.AddRange(value);
            InvalidateDisguise();
        }
    }

    /// <summary>牌组里的原件；战斗副本的 DeckVersion 指向它。伪装记录只认它。</summary>
    private MysteriousLeaf? DeckOrigin => DeckVersion as MysteriousLeaf;

    private MysteriousLeaf RecordOwner => DeckOrigin ?? this;

    /// <summary>按伪装记录重建的牌（带升级等级与附魔）；无记录或模型已缺失时为 null。</summary>
    private CardModel? _disguiseCard;

    /// <summary>
    /// 伪装目标。必须按记录重建（<c>FromSerializable</c> 会补上升级等级与附魔），
    /// 直接用 <c>ModelDb.GetByIdOrNull</c> 拿到的是 canonical 模板，永远是未升级的牌。
    /// 缓存一份：描述/悬停是渲染期高频调用。
    /// </summary>
    private CardModel? DisguiseCard
    {
        get
        {
            var owner = RecordOwner;
            var record = owner._disguise.FirstOrDefault();
            if (record?.Id == null) return null;
            if (ModelDb.GetByIdOrNull<CardModel>(record.Id) == null) return null;

            return owner._disguiseCard ??= CardModel.FromSerializable(record);
        }
    }

    /// <summary>耗能与伪装目标一致（含升级带来的变化）。</summary>
    protected override int CanonicalEnergyCost =>
        DisguiseCard is { } d ? Math.Max(0, d.EnergyCost.GetWithModifiers(CostModifiers.None)) : 0;

    /// <summary>X 耗能也跟随伪装目标。</summary>
    protected override bool HasEnergyCostX => DisguiseCard?.EnergyCost.CostsX ?? false;

    /// <summary>辉星消耗与伪装目标一致（-1 = 无辉星消耗）。</summary>
    public override int CanonicalStarCost => DisguiseCard?.BaseStarCost ?? -1;

    /// <summary>X 辉星消耗也跟随伪装目标。</summary>
    public override bool HasStarCostX => DisguiseCard?.HasStarCostX ?? false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new StringVar("LeafText")];

    // 伪装说明直接内联在卡牌本地化里，不走 CardKeyword
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>
            {
                new HoverTip(
                    new LocString("cards", Id.Entry + ".disguiseTitle"),
                    new LocString("cards", Id.Entry + ".disguiseDescription"))
            };

            // 已伪装时额外显示伪装目标的完整卡面提示（实例已带升级态，无需 upgrade 参数）
            var disguise = DisguiseCard;
            if (disguise != null) tips.Add(HoverTipFactory.FromCard(disguise));
            return tips;
        }
    }

    public MysteriousLeaf() : base(0, CardType.Skill, CardRarity.Event, TargetType.None, true)
    {
    }

    /// <summary>每当你把一张牌加入牌组，就把那一张记成伪装目标（自己进牌组不算）。</summary>
    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (IsCanonical) return Task.CompletedTask;
        if (card == this) return Task.CompletedTask;
        if (card.Pile?.Type != PileType.Deck) return Task.CompletedTask;
        if (card.Owner != null && Owner != null && card.Owner != Owner) return Task.CompletedTask;

        var owner = RecordOwner;
        owner.AssertMutable();
        owner._disguise.Clear();
        owner._disguise.Add(card.ToSerializable());
        owner.InvalidateDisguise();
        return Task.CompletedTask;
    }

    /// <summary>打出伪装目标牌的复制（复制品打完进 Limbo，不会留在弃牌堆）。</summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var record = RecordOwner._disguise.FirstOrDefault();
        if (record == null) return;

        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        // 记录只是牌样：临场造一张战斗副本，再打出它的复制（照原版历史课）
        var card = CardModel.FromSerializable(record);
        combatState.AddCard(card, Owner);

        var dupe = card.CreateDupe();
        // 树叶自己的 X 已经花掉了：AutoPlay 在抽干之后才捕获，只会拿到 0，得先把本次 X 传过去
        var skipXCapture = false;
        if (dupe.EnergyCost.CostsX && EnergyCost.CostsX)
        {
            dupe.EnergyCost.CapturedXValue = EnergyCost.CapturedXValue;
            skipXCapture = true;
        }

        if (dupe.HasStarCostX)
        {
            dupe.LastStarsSpent = LastStarsSpent;
            skipXCapture = true;
        }

        await CardCmd.AutoPlay(choiceContext, dupe, null, AutoPlayType.Default, skipXCapture);
    }

    /// <summary>描述两态：未伪装只有通用规则；已伪装时开头多一行「打出{CardStr}。」。升级过会带 +。</summary>
    protected override void AddExtraArgsToDescription(LocString description)
    {
        var disguise = DisguiseCard;
        string text;
        if (disguise != null)
        {
            var disguised = new LocString("cards", Id.Entry + ".descriptionDisguised");
            disguised.Add("CardStr", disguise.Title);
            text = disguised.GetFormattedText();
        }
        else
        {
            text = new LocString("cards", Id.Entry + ".descriptionIdle").GetFormattedText();
        }

        ((StringVar)DynamicVars["LeafText"]).StringValue = text;
        description.Add("LeafText", text);
    }

    /// <summary>伪装记录变了：丢掉重建缓存，再把耗能/辉星同步到新目标（牌组界面里没有 CombatState）。</summary>
    private void InvalidateDisguise()
    {
        _disguiseCard = null;
        ApplyDisguiseCost();
    }

    /// <summary>EnergyCost 的基址是首次访问时快照的，伪装变更后要同步一次。</summary>
    private void ApplyDisguiseCost()
    {
        if (!IsMutable) return;
        if (EnergyCost.CostsX != HasEnergyCostX)
        {
            // CostsX 构造后不可改：对不上只能整块换掉（局部费用修正一并丢失）
            MockSetEnergyCost(new CardEnergyCost(this, CanonicalEnergyCost, HasEnergyCostX));
            InvokeEnergyCostChanged();
        }
        else
        {
            EnergyCost.SetCustomBaseCost(CanonicalEnergyCost);
        }

        ApplyDisguiseStarCost();
    }

    /// <summary>星费没有公开写入口：清掉懒加载标记让 BaseStarCost 重取 CanonicalStarCost，再通知一次 UI。</summary>
    private void ApplyDisguiseStarCost()
    {
        if (StarCostSetField == null) return;

        StarCostSetField.SetValue(this, false);
        _ = BaseStarCost;
        if (StarCostChangedField?.GetValue(this) is Action notify) notify();
    }
}
