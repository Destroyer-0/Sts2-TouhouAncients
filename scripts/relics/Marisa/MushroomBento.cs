using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 蘑菇便当盒：在每个回合开始时获得1能量。回合结束时，如果你的手牌数大于等于3，
/// 将一张孢子心灵（SporeMind）加入抽牌堆。
/// 彩蛋：替换你所有的遗物：面包
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class MushroomBento : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1), new CardsVar(3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SporeMind>().Append(HoverTipFactory.ForEnergy(this));

    public static HashSet<Player> hasReplacedStart = new();

    public override Task AfterObtained()
    {
        if (hasReplacedStart.Add(base.Owner))
        {
            var breads = new List<RelicModel>(Owner.Relics.Where(x => x is Bread));
            foreach (var bread in breads)
            {
                RelicCmd.Replace(bread, ModelDb.Relic<MushroomBento>().ToMutable());
            }
        }
        hasReplacedStart.Remove(base.Owner);
        return base.AfterObtained();
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner)
            return amount;
        return amount + base.DynamicVars.Energy.IntValue;
    }

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != base.Owner.Creature.Side) return;
        if (base.Owner.PlayerCombatState == null) return;

        var hand = PileType.Hand.GetPile(base.Owner);
        if (hand.Cards.Count < 3) return;

        Flash();
        await CardPileCmd.AddToCombatAndPreview<SporeMind>(base.Owner.Creature, PileType.Draw, 1, true);
    }
}