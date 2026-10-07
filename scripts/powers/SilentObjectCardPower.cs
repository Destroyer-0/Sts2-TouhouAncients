using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 不语之物的本体：持有该不语之物所代表的卡牌，悬停随从时预览该牌
/// （机制参考原版偷窃草蜢的 SwipePower：Creature.HoverTips 会聚合各 Power 的 ExtraHoverTips）。
/// </summary>
public class SilentObjectCardPower : TouhouAncientPowerModel
{
    /// <summary>文案键前缀（power 的键由类名 slug 得到，数字不会补分隔符，所以第二套键只能自己写）。</summary>
    private const string RemovedFromDeckPrefix = "TOUHOUANCIENTS-SILENT_OBJECT_CARD_POWER";
    private const string AgitatedInCombatPrefix = "TOUHOUANCIENTS-SILENT_OBJECT_CARD_2_POWER";

    /// <summary>图标复用原版 SwipePower（卡牌被敌方持有）。</summary>
    public override string? CustomPackedIconPath => TouhouAncientCmd.CheckPathExists("res://images/atlases/power_atlas.sprites/swipe_power.tres");

    public override string? CustomBigIconPath => TouhouAncientCmd.CheckPathExists("res://images/powers/swipe_power.png");

    private CardModel? _embodiedCard;
    private bool _fromDeck = true;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldPlayVfx => false;

    /// <summary>所代表的牌来自牌组（本局被移除的牌，被抛弃），而非战斗中因魔力躁动被鼓动的牌。</summary>
    public bool FromDeck
    {
        get => _fromDeck;
        set
        {
            AssertMutable();
            _fromDeck = value;
        }
    }

    private string LocPrefix => _fromDeck ? RemovedFromDeckPrefix : AgitatedInCombatPrefix;

    public override LocString Title => new("powers", LocPrefix + ".title");

    public override LocString Description => new("powers", LocPrefix + ".description");

    protected override string SmartDescriptionLocKey => LocPrefix + ".smartDescription";

    /// <summary>所代表的卡牌。</summary>
    public CardModel? EmbodiedCard
    {
        get => _embodiedCard;
        set
        {
            AssertMutable();
            _embodiedCard = value;
        }
    }

    /// <summary>悬停预览：被代表的牌 + 这张牌自己的全部提示（附魔 / 苦恼 / 关键词 / 重放等，等同手牌里悬停它）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        EmbodiedCard == null ? [] : EmbodiedCard.HoverTips.Prepend(HoverTipFactory.FromCard(EmbodiedCard));
}
