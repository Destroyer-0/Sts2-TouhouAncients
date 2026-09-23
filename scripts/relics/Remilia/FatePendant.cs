using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.Patches;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 命运吊坠：
/// 在你的回合开始时，蕾米莉亚会将随机 {Cards} 张手牌标记为「命运」。
/// 如果本回合你打出的首张牌为「命运」，获得 {Energy} 点能量。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class FatePendant : TouhouAncientRelics
{
    private HashSet<CardModel> _marked = [];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3),
        new EnergyVar(1)
    ];

    private int MarkCount => base.DynamicVars.Cards.IntValue;

    /// <summary>该牌是否被本遗物标记为「命运」。</summary>
    public bool IsMarked(CardModel? card) => card != null && _marked.Contains(card);

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _marked = new ();
    }

    public override Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return Task.CompletedTask;

        ClearMarks();

        IReadOnlyList<CardModel> hand = PileType.Hand.GetPile(player).Cards;
        if (hand.Count <= 0) return Task.CompletedTask;

        Flash();

        _marked = PickCardsToMark(player, hand);
        RefreshCards(_marked);

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return Task.CompletedTask;

        // 「本回合首张牌」只统计玩家手动打出的牌，排除自动打出。
        // CardPlayFinished 已在 Hook.AfterCardPlayed 之前写入历史，故此处计数为 1 即表示刚打出的是首张手动牌。
        bool isFirstManualCardThisTurn = CombatManager.Instance.History.CardPlaysFinished
            .Count(entry => entry.CardPlay.Player == base.Owner &&
                            !entry.CardPlay.IsAutoPlay &&
                            entry.HappenedThisTurn(base.Owner.Creature.CombatState)) == 1;

        if (!isFirstManualCardThisTurn) return Task.CompletedTask;

        // 本回合第一张手动牌打出后，无论它是不是「命运」牌，都清除所有命运标记。
        // 触发与否要在清除前判定（ClearMarks 会移除标记）。
        bool triggered = IsMarked(cardPlay.Card);
        ClearMarks();

        if (!triggered) return Task.CompletedTask;
        if (RunManager.Instance.IsGameOver) return Task.CompletedTask;

        Flash();
        TalkCmd.Play(RelicModel.L10NLookup(base.Id.Entry + ".approval"), base.Owner.Creature, VfxColor.Red);
        return GainEnergy();
    }

    /// <summary>战斗结束时清空标记（读档、异常中断等情况的安全网）。</summary>
    public override Task AfterCombatEnd(CombatRoom room)
    {
        ClearMarks();
        return Task.CompletedTask;
    }

    private async Task GainEnergy()
    {
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.IntValue, base.Owner);
    }

    private HashSet<CardModel> PickCardsToMark(Player player, IReadOnlyList<CardModel> hand)
    {
        var rng = player.RunState.Rng.CombatCardGeneration;
        int count = System.Math.Min(MarkCount, hand.Count);
        var result = new HashSet<CardModel>();

        List<CardModel> playable = hand.Where(card => card.CanPlay()).ToList();
        if (playable.Count > 0)
        {
            result.Add(playable[rng.NextInt(playable.Count)]);
        }

        List<CardModel> remaining = hand.Where(card => !result.Contains(card)).ToList();
        while (result.Count < count && remaining.Count > 0)
        {
            CardModel picked = remaining[rng.NextInt(remaining.Count)];
            remaining.Remove(picked);
            result.Add(picked);
        }

        return result;
    }

    /// <summary>
    /// 清除玩家所有区域内卡牌的「命运」标记（手牌 / 抽牌堆 / 弃牌堆 / 消耗堆 / 打出堆 / 牌组），
    /// 并刷新仍可见的卡面 —— 否则被标记的牌流转到其它牌堆后，
    /// 仍会带着流光与「命运的指引」额外文本。
    /// </summary>
    private void ClearMarks()
    {
        var cleared = new List<CardModel>();

        // 按牌堆扫描而不是只清 _marked：标记的牌可能已流转到其它区域
        foreach (CardPile pile in base.Owner.Piles)
        {
            foreach (CardModel card in pile.Cards)
            {
                if (_marked.Remove(card)) cleared.Add(card);
            }
        }

        // 兜底：已不在任何牌堆中的残留引用（如已被移除的牌）
        if (_marked.Count > 0)
        {
            cleared.AddRange(_marked);
            _marked.Clear();
        }

        RefreshCards(cleared);
    }

    private void RefreshCards(IEnumerable<CardModel> cards)
    {
        foreach (CardModel card in cards)
        {
            CardGlowPatch.RefreshHandCard(card);
        }
    }
}
