using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.CmdUtils;

/// <summary>
/// 预见命令：检视抽牌堆顶部的 X 张牌，选择任意张置入弃牌堆。
/// </summary>
public static class SatoriScryCmd
{
    /// <summary>
    /// 执行预见操作。
    /// </summary>
    /// <param name="player">执行预见的玩家</param>
    /// <param name="amount">查看的牌数</param>
    /// <param name="choiceContext">选择上下文（战斗中传递 choiceContext，非战斗用 new BlockingPlayerChoiceContext()）</param>
    public static async Task SatoriScry(Player player, int amount, PlayerChoiceContext? choiceContext = null)
    {
        if (amount <= 0) return;

        var drawPile = PileType.Draw.GetPile(player);
        var topCards = drawPile.Cards.Take(amount).ToList();
        if (topCards.Count <= 0)
        {
            await Cmd.Wait(0.25f);
            return;
        }

        choiceContext ??= new BlockingPlayerChoiceContext();
        var prefs = new CardSelectorPrefs(
            new LocString("card_keywords","TOUHOUANCIENTS-SATORISCRY.selectionScreenPrompt"),
            0,
            topCards.Count
        );
        var toDiscard = (await CardSelectCmd.FromSimpleGrid(
            context: choiceContext,
            cardsIn: topCards,
            player: player,
            prefs
        )).ToList();

        foreach (var card in toDiscard)
        {
            await CardCmd.Discard(choiceContext, card);
        }
    }
}
