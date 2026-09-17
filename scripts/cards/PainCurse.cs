using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 疼痛 — 诅咒牌
/// 不可被打出。每当你打出牌时，如果这张牌在你的手牌中，你失去1点生命。
/// </summary>
[Pool(typeof(CurseCardPool))]
public class PainCurse : TouhouAncientCards
{
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;

    private const int energyCost = -1;
    private const CardType type = CardType.Curse;
    private const CardRarity rarity = CardRarity.Curse;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    /// <summary>每次触发时失去的生命值。</summary>
    private const decimal HpLossPerPlay = 1m;

    /// <summary>
    /// 立绘：本 Mod 尚无疼痛的专属立绘，回落到同为诅咒的「遗恨」立绘作为占位。
    /// 将来补上 <c>images/cards/PainCurse.png</c> 后会自动优先使用该图。
    /// </summary>
    public override string PortraitPath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/cards/{GetType().Name}.png",
        "res://images/cards/YuanHen.png");

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable];

    /// <summary>诅咒不可升级。</summary>
    public override int MaxUpgradeLevel => 0;

    public PainCurse() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>
    /// 每当有牌被打出时检查自身是否仍在手牌中；在则失去生命。
    /// 用 Unblockable | Unpowered 复刻本 Mod 其他「失去生命」效果的写法
    /// （见 DraculaLegacy.LoseHp / SoulSakeCup.AfterObtained）。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        // 本牌不可被打出，此处仅为防御性判断
        if (cardPlay.Card == this) return;
        // 必须仍在手牌中才触发
        if (base.Pile?.Type != PileType.Hand) return;

        // 参数顺序：(choiceContext, target, amount, props, cardSource, cardPlay)
        await CreatureCmd.Damage(
            choiceContext,
            base.Owner.Creature,
            HpLossPerPlay,
            ValueProp.Unblockable | ValueProp.Unpowered,
            this,
            null);
    }
}
