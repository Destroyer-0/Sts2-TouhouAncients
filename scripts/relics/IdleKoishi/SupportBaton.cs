using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.Ui;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 应援棒：每场战斗开始时，选择一种演奏方式（断奏 / 渐强 / 协和）。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SupportBaton : TouhouAncientRelics
{
    /// <summary>本场战斗是否已选过演奏方式。</summary>
    private bool _chosenThisCombat;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => KoishiPerformanceHoverTips;

    public override Task BeforeCombatStart()
    {
        _chosenThisCombat = false;
        EnsureAtmosphereLabel();
        return Task.CompletedTask;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;

        // 战斗房间可能在 BeforeCombatStart 之后才就绪，这里兜底一次（幂等）
        EnsureAtmosphereLabel();

        if (_chosenThisCombat) return;
        if (player.PlayerCombatState == null || player.PlayerCombatState.TurnNumber != 1) return;

        _chosenThisCombat = true;
        Flash();

        List<CardModel> choices =
        [
            combatState.CreateCard(ModelDb.Card<StaccatoChoice>(), player),
            combatState.CreateCard(ModelDb.Card<CrescendoChoice>(), player),
            combatState.CreateCard(ModelDb.Card<HarmonyChoice>(), player),
        ];

        var chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, player);
        if (chosen is IPerformanceChoice performance)
        {
            await performance.OnChosen(choiceContext);
        }
    }

    /// <summary>头顶气氛读数只给本地玩家自己挂，联机时各端互不影响。</summary>
    private void EnsureAtmosphereLabel()
    {
        if (base.Owner is not { } player || !LocalContext.IsMe(player)) return;
        AtmosphereLabel.Create(player);
    }
}
