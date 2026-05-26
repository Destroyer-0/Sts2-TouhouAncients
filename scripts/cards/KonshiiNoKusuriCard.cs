using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 绀珠之药 — 诅咒牌
/// 不可被打出，保留。此牌在手中时，生命值降至0时弃置此牌，回复一定比例生命值并失去最大生命，
/// 每有一层污秽额外失去最大生命，然后获得一层污秽计数。
/// </summary>
[Pool(typeof(CurseCardPool))]
public class KonshiiNoKusuriCard : TouhouAncientCards
{
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    private const int energyCost = -1;
    private const CardType type = CardType.Curse;
    private const CardRarity rarity = CardRarity.Curse;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HealPercent", 30),
        new DynamicVar("BaseMaxHpLoss", 6),
        new DynamicVar("ShouldLose", 0)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Unplayable,
        CardKeyword.Retain
    ];

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientFilth)];

    public KonshiiNoKusuriCard() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    private KonshiiNoKusuri? GetRelic() => Owner.GetRelic<KonshiiNoKusuri>();

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card == this)
        {
            UpdateShouldLoseDisplay();
        }

        return Task.CompletedTask;
    }

    private void UpdateShouldLoseDisplay()
    {
        var filthBonus = (GetRelic()?.TouhouAncients_FilthCount + 1) ?? 0;
        DynamicVars["ShouldLose"].BaseValue = filthBonus * DynamicVars["BaseMaxHpLoss"].IntValue;
    }

    /// <summary>
    /// 阻止死亡：此牌在手中时阻止死亡。
    /// </summary>
    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner?.Creature) return true;
        return Pile?.Type != PileType.Hand; // 不在手中则正常死亡
    }

    /// <summary>
    /// 死亡被阻止后：弃置此牌，回复生命，失去最大生命，增加污秽。
    /// </summary>
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != base.Owner?.Creature) return;
        if (Pile?.Type != PileType.Hand) return;


        // 回复生命值
        var healPercent = base.DynamicVars["HealPercent"].BaseValue / 100m;
        var healAmount = Math.Max(1m, (decimal)creature.MaxHp * healPercent);
        await CreatureCmd.Heal(creature, healAmount);

        var relic = GetRelic();
        if (relic == null) return;

        var a = new ThrowingPlayerChoiceContext();
        // 从手牌中移除这张牌
        await CardCmd.Discard(a, this);
        // 失去最大生命：基础 + 每层污秽额外
        await CreatureCmd.LoseMaxHp(a, creature, DynamicVars["ShouldLose"].IntValue, isFromCard: true);

        // 增加一层污秽
        relic.IncrementFilth();
        UpdateShouldLoseDisplay();
    }
}