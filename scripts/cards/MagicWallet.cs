using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class MagicWallet : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    private decimal _extraPowerNum;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("CurrentFreeCount", 1),
        new EnergyVar(1),
    ];

    private decimal ExtraPowerNum
    {
        get
        {
            return _extraPowerNum;
        }
        set
        {
            AssertMutable();
            _extraPowerNum = value;
        }
    }
    public MagicWallet() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = Owner;
        if (player.Creature.CombatState == null) return;

        // 将免费次数+1（累加机制记录到 MagicWalletPower）
        // 该 Power 负责拦截下一张牌的消耗
        await PowerCmd.Apply<MagicWalletPower>(player.Creature, DynamicVars["CurrentFreeCount"].BaseValue, player.Creature, this);
        EnergyCost.AddThisCombat(DynamicVars.Energy.IntValue);
        base.DynamicVars["CurrentFreeCount"].BaseValue += 1;
        ExtraPowerNum += 1;
    }
}
