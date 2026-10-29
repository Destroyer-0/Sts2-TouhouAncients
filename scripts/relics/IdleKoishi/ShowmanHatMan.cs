using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 演出用帽子先生：每获得 4 气氛获得 1 活力；嗨到爆下每次触发还额外把一张带虚无的随机牌加入手牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class ShowmanHatMan : TouhouAncientRelics, IAtmosphereGainedListener
{
    private const int AtmospherePerVigor = 4;
    private const int StatAmount = 1;

    private int _progress;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("AtmospherePerVigor", AtmospherePerVigor),
        new DynamicVar("StatAmount", StatAmount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VigorPower>(),
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal),
        .. KoishiPerformanceHoverTips,
    ];

    public override Task BeforeCombatStart()
    {
        _progress = 0;
        return Task.CompletedTask;
    }

    public async Task AfterAtmosphereGained(PlayerChoiceContext choiceContext, int amount)
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        _progress += amount;
        while (_progress >= AtmospherePerVigor)
        {
            _progress -= AtmospherePerVigor;
            Flash();
            await PowerCmd.Apply<VigorPower>(choiceContext, player.Creature, StatAmount, player.Creature, null);

            if (KoishiPerformanceCmd.IsHyped(player))
            {
                await AddEtherealCardToHand(choiceContext, player);
            }
        }
    }

    private static async Task AddEtherealCardToHand(PlayerChoiceContext choiceContext, Player player)
    {
        var pool = player.Character.CardPool.GetUnlockedCards(player.UnlockState,
            player.RunState.CardMultiplayerConstraint);
        var card = CardFactory.GetForCombat(player, pool, 1, player.RunState.Rng.CombatCardGeneration)
            .FirstOrDefault();
        if (card == null) return;

        card.AddKeyword(CardKeyword.Ethereal);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, creator: player);
    }
}
