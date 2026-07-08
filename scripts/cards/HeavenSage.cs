using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(ColorlessCardPool))]
public class HeavenSage : TouhouAncientCards
{
    private const int energyCost = 2;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Uncommon;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;
    
    public HeavenSage() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HeavenSagePower>(choiceContext, base.Owner.Creature, 1m, base.Owner.Creature, this);
    }

}