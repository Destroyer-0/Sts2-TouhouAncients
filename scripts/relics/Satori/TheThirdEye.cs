using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.CmdUtils;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 第三只眼：战斗开始时，自身获得2层易伤、脆弱。第一次抽牌阶段开始前，预见5。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class TheThirdEye : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromCard<SatoriEye>(),
        HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientSatoriScry)
    ];

    public override async Task BeforeCombatStart()
    {
        var creature = base.Owner.Creature;

        // 自身获得2层虚弱
        await PowerCmd.Apply<WeakPower>(creature, 2m, creature, null);

        // 注：卡牌"觉之眼"暂未实现
        // var eyeCard = base.Owner.RunState.CreateCard(...);
        // await CardPileCmd.Add(eyeCard, PileType.Hand);
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner) return;
        if (Owner.Creature.CombatState == null) return;
        if (combatState.RoundNumber != 1) return;

        Flash();

        // 预见5：查看抽牌堆顶的5张牌，选择任意张置入弃牌堆
        await SatoriScryCmd.SatoriScry(player, 5, choiceContext);
        // 加入觉之眼卡
        await CardPileCmd.AddGeneratedCardsToCombat([base.Owner.Creature.CombatState.CreateCard<SatoriEye>(base.Owner)],
            PileType.Hand, addedByPlayer: true);
    }
}