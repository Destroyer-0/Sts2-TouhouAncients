using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 龙颈之玉：在每场战斗开始时，将一张七星+加入你的手牌，其拥有保留。
/// 你每打出5张牌，其在本场战斗中的辉星费用-1。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RyukeiNoTama : TouhouAncientRelics
{
    private int _cardsPlayedThisCombat;
    private int _starCostReduction;

    private SevenStars sevenStar;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Card", 5)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SevenStars>(true).Append(HoverTipFactory.FromKeyword(CardKeyword.Retain));

    public override Task BeforeCombatStart()
    {
        _cardsPlayedThisCombat = 0;
        _starCostReduction = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (player.Creature.CombatState?.RoundNumber != 1) return;

        Flash();

        sevenStar = player.Creature.CombatState.CreateCard<SevenStars>(player);
        Flash();
        CardCmd.Upgrade(sevenStar);
        await CardPileCmd.AddGeneratedCardsToCombat([sevenStar], PileType.Hand, creator: base.Owner);
        
        CardCmd.ApplyKeyword(sevenStar, CardKeyword.Retain);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (cardPlay.Card.Owner != player) return;
        if (player.Creature.CombatState == null) return;

        _cardsPlayedThisCombat++;

        await TryReduceStarCost(player);
    }

    /// <summary>
    /// 回合结束时也触发辉星费用减少。
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;
        if (!participants.Contains(Owner.Creature)) return;
        
        var player = base.Owner;
        if (player?.Creature.CombatState == null) return;

        await TryReduceStarCost(player);
    }

    private async Task TryReduceStarCost(Player player)
    {
        if(sevenStar.HasBeenRemovedFromState)return;
        if (_cardsPlayedThisCombat < 5) return;

        _cardsPlayedThisCombat = 0;
        _starCostReduction++;
        Flash();
        
        sevenStar.SetStarCostThisCombat(Math.Max(0, sevenStar.CurrentStarCost - 1));
    }
}
