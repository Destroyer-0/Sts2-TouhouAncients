using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 神诏之梦：拾起时，失去 12 最大生命。每场战斗的前 3 回合，在你的回合结束时，你可以依次从 3 张随机普通、罕见、稀有牌中
/// 选择 1 张，洗入你的抽牌堆（可跳过）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DivineEdictDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new HpLossVar(12m),
        new CardsVar(3),
        new DynamicVar("Turns", 3)
    ];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        Flash();
        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            player.Creature,
            DynamicVars.HpLoss.BaseValue,
            cardSource: null);
    }

    // 不能放 AfterFlush：那里的 context 是 CombatPlayPhaseOnly，此时队列已暂停，弹选牌会卡死
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        var player = base.Owner;
        if (player?.Creature == null) return;
        if (side != player.Creature.Side || !participants.Contains(player.Creature)) return;

        // TurnNumber 在回合开始时自增，此处仍是本回合编号
        int turn = player.PlayerCombatState?.TurnNumber ?? 0;
        if (turn < 1 || turn > DynamicVars["Turns"].IntValue) return;

        // 第 1/2/3 回合依次给普通 / 罕见 / 稀有
        CardRarity rarity = turn switch
        {
            1 => CardRarity.Common,
            2 => CardRarity.Uncommon,
            _ => CardRarity.Rare
        };

        var pool = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Rarity == rarity);

        // 同原版工具箱：联机各端用同一 RNG 流生成选项，玩家只同步选中的序号
        var choices = CardFactory
            .GetDistinctForCombat(player, pool, DynamicVars.Cards.IntValue, player.RunState.Rng.CombatCardGeneration)
            .ToList();

        if (choices.Count == 0) return;

        Flash();
        var chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, player, canSkip: true);
        if (chosen == null) return;

        await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Draw, player, CardPilePosition.Random);
    }
}
