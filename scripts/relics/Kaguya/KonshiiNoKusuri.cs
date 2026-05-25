using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 绀珠之药：每场战斗开始时，将一张绀珠之药置入手牌。
/// 绀珠之药效果：生命值降至0时弃置此牌，回复30%生命值并失去4最大生命，
/// 每有一层污秽额外失去4最大生命，然后获得一层污秽计数。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KonshiiNoKusuri : TouhouAncientRelics
{
    /// <summary>
    /// 全局污秽计数，持久化保存。每次触发复活永久增加1层。
    /// </summary>
    [SavedProperty]
    private int TouhouAncients_FilthCount { get; set; }

    private const decimal HealPercent = 0.3m;
    private const decimal BaseMaxHpLoss = 4m;
    private const decimal ExtraMaxHpLossPerFilth = 4m;

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Unplayable),
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
    ];

    /// <summary>
    /// 战斗开始时，将绀珠之药置入手牌。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        var player = base.Owner;
        if (player == null) return;

        var card = player.RunState.CreateCard(ModelDb.Card<KonshiiNoKusuriCard>(), player);
        await CardPileCmd.AddToHand(card, player);
    }

    /// <summary>
    /// 阻止死亡：检查手中是否有绀珠之药。
    /// </summary>
    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != base.Owner?.Creature) return true;
        if (TouhouAncients_FilthCount < 0) TouhouAncients_FilthCount = 0;

        // 检查手中是否有绀珠之药诅咒牌
        var handPile = PileType.Hand.GetPile(base.Owner);
        var hasKusuri = handPile?.Any(c => c is KonshiiNoKusuriCard
                                            || c.Id.Entry == nameof(KonshiiNoKusuriCard)) == true;
        return !hasKusuri; // 没有药则正常死亡
    }

    /// <summary>
    /// 死亡被阻止后：弃置绀珠之药，回复生命，失去最大生命，增加污秽。
    /// </summary>
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != base.Owner?.Creature) return;

        Flash();
        var player = base.Owner;

        // 从手牌中移除绀珠之药
        var handPile = PileType.Hand.GetPile(player);
        var kusuriCard = handPile?.Cards.FirstOrDefault(c => c is KonshiiNoKusuriCard
                                                       || c.Id.Entry == nameof(KonshiiNoKusuriCard));
        if (kusuriCard != null)
        {
            await CardPileCmd.RemoveFromCombat(kusuriCard);
        }

        // 回复30%生命值
        var healAmount = Math.Max(1m, (decimal)creature.MaxHp * HealPercent);
        await CreatureCmd.Heal(creature, healAmount);

        // 失去最大生命：基础4 + 每层污秽额外4
        var totalMaxHpLoss = BaseMaxHpLoss + ExtraMaxHpLossPerFilth * TouhouAncients_FilthCount;
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), creature, totalMaxHpLoss, isFromCard: false);

        // 增加一层污秽
        TouhouAncients_FilthCount++;
        InvokeDisplayAmountChanged();
    }
}
