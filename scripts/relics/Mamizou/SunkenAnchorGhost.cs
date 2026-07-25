using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 沉锚幽灵：回合开始时获得1能量。战斗开始时，为你的能力牌添加沉底关键词。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SunkenAnchorGhost : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + 1m;
    }

    /// <summary>
    /// 战斗开始后，为牌组中所有能力牌添加沉底关键词。
    /// </summary>
    public override Task BeforeCombatStartLate()
    {
        if (base.Owner?.Creature?.CombatState == null) return Task.CompletedTask;

        var player = base.Owner;
        var drawPile = player.PlayerCombatState.DrawPile;

        var powerCards = drawPile.Cards
            .Where(c => c.Type == CardType.Power && !c.Keywords.Contains(TouhouAncientKeywords.TouhouAncientSinkToBottom))
            .ToList();

        if (powerCards.Count == 0) return Task.CompletedTask;

        Flash();
        foreach (var card in powerCards)
        {
            CardCmd.ApplyKeyword(card, TouhouAncientKeywords.TouhouAncientSinkToBottom);
        }

        return Task.CompletedTask;
    }
}
