using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 至贫形态 (Poorest Form)
/// 3费能力牌，虚无（升级后去除）。
/// 打出耗能为 0 的牌或以 0 能量结束回合时，
/// 每 1 层至贫形态给予随机一个敌人以下一个 Debuff：
///   1 虚弱 / 1 易伤 / 3 中毒 / 8 灾厄
/// </summary>
[Pool(typeof(EventCardPool))]
public class PoorestForm : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = false;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Eternal
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Amount", 5m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DoomPower>()
    ];

    public PoorestForm() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;
        // 至贫形态可叠加
        await PowerCmd.Apply<PoorestFormPower>(choiceContext, creature, base.DynamicVars["Amount"].BaseValue, creature,
            this);
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["Amount"].BaseValue += 1;
    }
}
