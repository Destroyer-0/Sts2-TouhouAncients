using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 绀珠之药 — 诅咒牌
/// 不可被打出，保留。此牌在手中时，生命值降至0时弃置此牌，回复30%生命值并失去4最大生命，
/// 每有一层污秽额外失去4最大生命，然后获得一层污秽计数。
/// </summary>
public class KonshiiNoKusuriCard : TouhouAncientCards
{
    private const int energyCost = -1;
    private const CardType type = CardType.Curse;
    private const CardRarity rarity = CardRarity.Curse;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable,
        CardKeyword.Retain
    ];

    public override int MaxUpgradeLevel => 0;

    public KonshiiNoKusuriCard() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }
}
