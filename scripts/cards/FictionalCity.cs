using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>虚构都市：贝子塞入抽牌堆的状态牌，抽到时叠一层计数，满 6 层失去力量与敏捷。</summary>
[Pool(typeof(StatusCardPool))]
public sealed class FictionalCity : TouhouAncientCards
{
    private const int energyCost = -1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    public override bool UseAncientFrame => true;

    public override string? Author => "ブドウあめ";
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Unplayable, CardKeyword.Ethereal];

    public FictionalCity() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);
        // 计数与扣减都在 Power 里结算，卡牌本体只管叠层
        await PowerCmd.Apply<FictionalCityCountPower>(choiceContext, base.Owner.Creature,
            1m, base.Owner.Creature, this);
    }
}
