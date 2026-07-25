using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 未名妖魔：1c（升级后0c）技能 保留。
/// 将一张此牌的复制品随机化耗能后加入抽牌堆。变化为上一张打出的牌并返回手牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class NamelessYoukai : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public NamelessYoukai() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
        // 升级后 0c
    }
}
