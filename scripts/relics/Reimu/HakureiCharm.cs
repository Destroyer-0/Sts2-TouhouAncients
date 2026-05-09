using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 博丽御札：战斗开始时，选择一个选项进行祈愿。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HakureiCharm : TouhouAncientRelics
{
    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, CombatState combatState)
    {
        if (player != base.Owner) return;
        if (combatState.RoundNumber != 1) return;

        Flash();

        // 构建选项：不同类型的卡牌代表不同选项
        var choiceCards = new List<CardModel>();

        // 跳过（诅咒牌，暗色边框）
        choiceCards.Add(player.RunState.CreateCard(ModelDb.Card<Injury>(), player));
        // 花费10金币（技能牌）
        if (player.Gold >= 10)
            choiceCards.Add(player.RunState.CreateCard(ModelDb.Card<Neutralize>(), player));
        // 花费30金币（能力牌）
        if (player.Gold >= 30)
            choiceCards.Add(player.RunState.CreateCard(ModelDb.Card<DemonForm>(), player));
        // 花费50金币
        if (player.Gold >= 50)
            choiceCards.Add(player.RunState.CreateCard(ModelDb.Card<Barricade>(), player));

        if (choiceCards.Count <= 0) return;

        var selected = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choiceCards,
            player,
            canSkip: true
        );

        if (selected == null) return;

        // 根据选择的卡牌类型判断选项
        if (selected is Injury) return; // 跳过

        if (selected.Id.Entry == "Neutralize")
        {
            await PlayerCmd.LoseGold(10, player);
            await CardPileCmd.Draw(choiceContext, 1, player, fromHandDraw: true);
        }
        else if (selected.Id.Entry == "DemonForm")
        {
            await PlayerCmd.LoseGold(30, player);
            await CardPileCmd.Draw(choiceContext, 1, player, fromHandDraw: true);
            // 升级所有初始手牌
            var hand = PileType.Hand.GetPile(player);
            foreach (var handCard in hand.Cards)
                CardCmd.Upgrade(handCard);
        }
        else if (selected.Id.Entry == "Barricade")
        {
            await PlayerCmd.LoseGold(50, player);
            await CardPileCmd.Draw(choiceContext, 1, player, fromHandDraw: true);
            // 获得1力量、1敏捷、1集中、3费
            var creature = player.Creature;
            await PowerCmd.Apply<StrengthPower>(creature, 1m, creature, null);
            await PowerCmd.Apply<DexterityPower>(creature, 1m, creature, null);
            await PowerCmd.Apply<FocusPower>(creature, 1m, creature, null);
            await PlayerCmd.GainEnergy(3, player);
        }
    }
}
