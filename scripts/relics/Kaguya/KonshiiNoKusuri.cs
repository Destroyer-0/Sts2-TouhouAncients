using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 绀珠之药：每场战斗开始时，将一张绀珠之药置入手牌。
/// 死亡拦截逻辑在 KonshiiNoKusuriCard 中处理。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KonshiiNoKusuri : TouhouAncientRelics
{
    /// <summary>
    /// 全局污秽计数，持久化保存。每次触发复活永久增加1层。
    /// 由 KonshiiNoKusuriCard 读取并递增。
    /// </summary>
    [SavedProperty]
    public int TouhouAncients_FilthCount
    {
        get => filthCount;
        set
        {
            AssertMutable();
            filthCount = value;
            InvokeDisplayAmountChanged();
        }
    }

    private int filthCount;

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<KonshiiNoKusuriCard>();

    public override bool ShowCounter => DisplayAmount > -1;

    public override int DisplayAmount => TouhouAncients_FilthCount;
    // {
    //     get
    //     {
    //         if (base.IsCanonical)
    //         {
    //             return -1;
    //         }
    //
    //         return TouhouAncients_FilthCount;
    //     }
    // }

    /// <summary>
    /// 增加一层污秽计数并刷新显示。由 KonshiiNoKusuriCard 调用。
    /// </summary>
    public void IncrementFilth()
    {
        TouhouAncients_FilthCount++;
        Flash();
        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 战斗开始时，将绀珠之药置入手牌。
    /// </summary>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner) return;
        if (Owner.Creature.CombatState == null) return;
        if (combatState.RoundNumber != 1) return;

        // 重置运行时标记

        Flash();

        await CardPileCmd.AddGeneratedCardsToCombat(
            [base.Owner.Creature.CombatState.CreateCard<KonshiiNoKusuriCard>(base.Owner)],
            PileType.Hand, addedByPlayer: true);
    }
}