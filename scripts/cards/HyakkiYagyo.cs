using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 百鬼夜行：1c（升级后0c）技能 保留。
/// 打出随机2张记录的牌，然后将它们在本场战斗中从记录中移除。
/// 记录全部移除后，将此牌在本场战斗中移出游戏。
/// </summary>
[Pool(typeof(EventCardPool))]
public class HyakkiYagyo : TouhouAncientCards
{
    public override string? Author => "抽风男";
    
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public HyakkiYagyo() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
        // 升级后 0c
    }
}
