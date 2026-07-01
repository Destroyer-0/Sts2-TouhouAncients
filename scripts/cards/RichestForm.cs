using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 极奢形态 (Richest Form)
/// 3费能力牌。
/// 获得 120（升级后 150）启动资金。
/// 打出非X费牌不再消耗能量，改为 1:10 消耗启动资金。
/// 启动资金不足时改为消耗金币。
/// </summary>
public class RichestForm : TouhouAncientCards
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
        new DynamicVar("StartingCapital", 120m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<CapitalPower>(),
        base.EnergyHoverTip
    ];

    public RichestForm() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;

        // 如果已有极奢形态，不再叠加
        var existingForm = creature.GetPower<RichestFormPower>();
        if (existingForm == null)
        {
            await PowerCmd.Apply<RichestFormPower>(choiceContext, creature, 1m, creature, this);
        }

        // 设置启动资金（覆盖已有资金）
        var startingCapital = base.DynamicVars["StartingCapital"].BaseValue;
        var existingCapital = creature.GetPower<CapitalPower>();
        if (existingCapital != null)
        {
            await PowerCmd.ModifyAmount(choiceContext, existingCapital, startingCapital - existingCapital.Amount, null, null);
        }
        else
        {
            await PowerCmd.Apply<CapitalPower>(choiceContext, creature, startingCapital, creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars["StartingCapital"].UpgradeValueBy(30m);
    }
}
