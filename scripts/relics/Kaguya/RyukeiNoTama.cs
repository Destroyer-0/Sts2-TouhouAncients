using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 龙颈之玉：
/// 在每场战斗开始时，将一张辉星费用为0的七星+（SevenStar，升级过）加入你的抽牌堆。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class RyukeiNoTama : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<SevenStars>(true);

    // public override async Task BeforeCombatStart()
    // {
    //     var player = base.Owner;
    //     if (player == null) return;
    //
    //     Flash();
    //
    //
    //     // 加入抽牌堆
    //     CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat([card], PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
    //     await Cmd.Wait(0.5f);
    // }
    //
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player == base.Owner && combatState.RoundNumber == 1)
        {
            Flash();
            List<CardModel> list = new List<CardModel>();
            
            // 创建七星牌

            var card = combatState.CreateCard<SevenStars>(base.Owner);

            // 升级（七星升级后费用从2降为1）
            if (!card.IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }

            // 辉星费用设为0
            card.SetStarCostThisCombat(0);
            list.Add(card);
            
            CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(list, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
            await Cmd.Wait(1f);
        }
    }
}
