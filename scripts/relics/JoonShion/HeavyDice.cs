using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 灌铅骰子：回合结束时，你可以选择一张手牌并记录。
/// 你的抽牌阶段结束后，如果你有记录的牌，则可以弃置一张牌，将记录卡牌的复制品加入手牌，其拥有消耗、虚无。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HeavyDice : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromKeyword(CardKeyword.Ethereal)
    ];

    // ---------- 工具：取/建记录 Power ----------

    private HeavyDiceRecordPower? GetRecordPower(Player player)
    {
        return player.Creature.GetPower<HeavyDiceRecordPower>();
    }

    private async Task<HeavyDiceRecordPower> GetOrApplyRecordPower(Player player)
    {
        HeavyDiceRecordPower? power = GetRecordPower(player);
        if (power == null)
        {
            power = await PowerCmd.Apply<HeavyDiceRecordPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, 1m, player.Creature, null);
        }
        return power!;
    }

    // ---------- 记录：回合结束时，选择至多一张手牌记录 ----------

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;
        if (!participants.Contains(base.Owner.Creature)) return;
        if (base.Owner.PlayerCombatState == null) return;

        // 用 SelectionScreenPrompt 的“记录”语义做选择；不选=保持旧记录
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 0, 1),
            context: choiceContext,
            player: base.Owner,
            filter: null,
            source: this)).ToList();
        if (selected.Count == 0) return;

        Flash();

        // 覆盖式记录：新的记录替换旧记录
        HeavyDiceRecordPower power = await GetOrApplyRecordPower(base.Owner);
        power.SetRecordedCard(selected[0]);
    }

    // ---------- 兑现：抽牌阶段结束后，弃置一张牌则拿回记录的复制品 ----------

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (base.Owner.PlayerCombatState == null) return;

        HeavyDiceRecordPower? power = GetRecordPower(player);
        if (power == null) return; // 无记录则无事发生

        var hand = PileType.Hand.GetPile(player);
        if (hand.IsEmpty) return;

        // 从手牌中选择 0~1 张弃置；不弃则保留记录
        var toDiscard = (await CardSelectCmd.FromHandForDiscard(
            context: choiceContext,
            player: player,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 0, 1),
            filter: null,
            source: this)).ToList();
        if (toDiscard.Count == 0) return;

        CardModel? recordedClone = power.TakeRecordedCard();
        if (recordedClone == null)
        {
            // 理论上不会发生：有 Power 必有记录；万一为空则移除 Power 兜底
            await PowerCmd.Remove(power);
            return;
        }

        Flash();
        await CardCmd.Discard(choiceContext, toDiscard);

        // 复制品获得消耗、虚无后加入手牌
        recordedClone.AddKeyword(CardKeyword.Exhaust);
        recordedClone.AddKeyword(CardKeyword.Ethereal);
        await CardPileCmd.Add(recordedClone, PileType.Hand);

        // 记录已兑现，移除标记 Power（下回合可重新记录）
        await PowerCmd.Remove(power);
    }

    // ---------- 战斗生命周期清理 ----------

    public override async Task BeforeCombatStart()
    {
        // 清掉上一场残留的记录标记
        HeavyDiceRecordPower? power = GetRecordPower(base.Owner);
        if (power != null)
        {
            await PowerCmd.Remove(power);
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        // 战斗结束清掉记录标记，防止跨战斗残留
        HeavyDiceRecordPower? power = GetRecordPower(base.Owner);
        if (power != null)
        {
            await PowerCmd.Remove(power);
        }
    }
}
