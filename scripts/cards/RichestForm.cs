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
[Pool(typeof(EventCardPool))]
public class RichestForm : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = false;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Eternal
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new EnergyVar("Energy2", 5),
        new EnergyVar("Energy3", 5),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        base.EnergyHoverTip
    ];

    public RichestForm() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;
        Owner.PlayerCombatState.GainEnergy(base.DynamicVars["Energy2"].IntValue);
        var power = await PowerCmd.Apply<RichestFormPower>(choiceContext, creature, DynamicVars.Energy.IntValue, creature, this);
        if (power != null)
        {
            power.ExtraEnergy = base.DynamicVars["Energy3"].IntValue;
        }
    }
    protected override void OnUpgrade()
    {
        base.DynamicVars["Energy2"].UpgradeValueBy(1m);
        base.DynamicVars["Energy3"].UpgradeValueBy(1m);
    }
}