using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 石油期货：每回合获得1费，每3回合，从3张随机诅咒中选择一张加入手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class OilFutures : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new DynamicVar("Turns", 2),
        new CardsVar(3)
    ];

    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_TurnCounter;

    private int TurnInterval => base.DynamicVars["Turns"].IntValue;

    [SavedProperty] public int TouhouAncients_TurnCounter { get; set; }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }
    
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.PlayerCombatState == null) return;
        
        // 每3回合：从3张随机诅咒中选择一张加入手牌
        TouhouAncients_TurnCounter++;
        if (TouhouAncients_TurnCounter < TurnInterval)
        {
            base.Status = RelicStatus.Normal;
            InvokeDisplayAmountChanged();
            return;
        }
        TouhouAncients_TurnCounter -= TurnInterval;
        
        Flash();
        base.Status = RelicStatus.Active;
        InvokeDisplayAmountChanged();
        List<CardModel> cards = CardFactory.GetDistinctForCombat(base.Owner, ModelDb.CardPool<CurseCardPool>().GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint), base.DynamicVars.Cards.IntValue, base.Owner.RunState.Rng.CombatCardGeneration).ToList();
        CardModel cardModel = await CardSelectCmd.FromChooseACardScreen(choiceContext, cards, base.Owner);
        if (cardModel != null)
        {
            await CardPileCmd.AddGeneratedCardToCombat(cardModel, PileType.Hand, player);
        }
        base.Status = RelicStatus.Normal;
        InvokeDisplayAmountChanged();
    }
}
