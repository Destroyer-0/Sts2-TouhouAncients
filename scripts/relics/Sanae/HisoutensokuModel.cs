using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class HisoutensokuModel : TouhouAncientRelics
{
    private const int OfferCount = 10;
    private const int SelectCount = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(OfferCount),
        new DynamicVar("SelectCount", SelectCount)
    ];

    /// <summary>
    /// 战斗开始时，从10张升级过的故障机器人卡牌中选择5张加入抽牌堆，这些牌免费。
    /// </summary>
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner) return;
        if (combatState != player.Creature.CombatState) return;
        if (player.Creature.CombatState?.RoundNumber != 1) return;

        Flash();

        // 获取故障机器人的卡牌池
        var defectPool = ModelDb.Character<Defect>().CardPool;
        var unlockedCards = defectPool.GetUnlockedCards(
            player.UnlockState, player.RunState.CardMultiplayerConstraint);

        // 随机选 OfferCount 张不同卡牌并升级
        var offered = CardFactory.GetDistinctForCombat(player, unlockedCards, OfferCount,
                player.RunState.Rng.CombatCardGeneration)
            .Select(c =>
            {
                if (c.IsUpgradable) CardCmd.Upgrade(c);
                return c;
            })
            .ToList();

        if (offered.Count == 0) return;

        // 实际可选数量不能超过 OfferCount
        var actualSelectCount = SelectCount > offered.Count ? offered.Count : SelectCount;

        // 网格选择
        var selected = (await CardSelectCmd.FromSimpleGrid(
            context: choiceContext,
            cardsIn: offered,
            player: player,
            prefs: new CardSelectorPrefs(
                base.SelectionScreenPrompt,
                actualSelectCount,
                actualSelectCount)
        )).ToList();

        if (selected.Count == 0) return;

        // 加入抽牌堆并设为本场战斗免费
        foreach (var card in selected)
        {
            card.SetToFreeThisCombat();
        }
        
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardsToCombat(selected, PileType.Draw, addedByPlayer: true, CardPilePosition.Random));
    }
}
