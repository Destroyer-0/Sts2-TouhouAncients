using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 公主的谜题：辉夜的五道难题。
/// 每道未完成的谜题让辉夜失去生命时减少 Amount 点生命损失。
/// 图标层数显示当前总减伤值（HpLossReduction = 未解谜题数 × Amount）；某道谜题需要所有玩家都完成才算解开，
/// 解开后 PuzzleNum 减少并刷新总减伤值。
/// </summary>
public sealed class PrincessPuzzlePower : TouhouAncientPowerModel
{

    /// <summary>初始未解开谜题数（五种谜题各一道）。</summary>
    private const int BasePuzzleNum = 5;

    /// <summary>谜题类型总数（0-4 五种）。</summary>
    private const int PuzzleTypeCount = 5;


    private const string PuzzleNumKey = "PuzzleNum";

    private const string HpLossReductionKey = "HpLossReduction";

    /// <summary>
    /// 每种谜题类型（0-4）各玩家完成状态。
    /// 战斗开始后（AfterApplied）重置；克隆时重新创建以避免共享 canonical 的引用。
    /// </summary>
    private HashSet<Player>[] _completedPlayers = CreateCompletionTracking();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 图标显示当前总减伤值。
    /// </summary>
    public override int DisplayAmount => base.DynamicVars[HpLossReductionKey].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(PuzzleNumKey, BasePuzzleNum),
        new DynamicVar(HpLossReductionKey, Amount * BasePuzzleNum)
    ];

    /// <summary>
    /// 首次施加时重置完成追踪并刷新总减伤值（此时 Amount 为每道谜题的减伤量）。
    /// </summary>
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        foreach (HashSet<Player> completed in _completedPlayers)
        {
            completed.Clear();
        }

        RefreshHpLossReduction();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 辉夜失去生命时按未解谜题数减免（与钨合金棍同一节点）。
    /// </summary>
    public override decimal ModifyHpLostAfterOsty(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != base.Owner)
        {
            return amount;
        }

        return Math.Max(0m, amount - base.DynamicVars[HpLossReductionKey].BaseValue);
    }

    /// <summary>
    /// 标记指定玩家完成指定类型的谜题。
    /// 当该类型的谜题首次达成"所有玩家都完成"时，未解开谜题数减一并刷新总减伤值。
    /// 每次有新玩家完成时，通知辉夜怪物同步谜题图标的漂浮演出透明度。
    /// </summary>
    public void CompletePuzzle(int puzzleType, Player player)
    {
        if (puzzleType < 0 || puzzleType >= _completedPlayers.Length) return;
        if (!_completedPlayers[puzzleType].Add(player)) return;

        if (base.Owner.Monster is HouraisanKaguyaMonster kaguya)
        {
            kaguya.NotifyPuzzleProgress(puzzleType);
        }

        int playerCount = base.Owner.CombatState?.Players.Count ?? 1;
        if (_completedPlayers[puzzleType].Count >= playerCount)
        {
            base.DynamicVars[PuzzleNumKey].BaseValue = Math.Max(0, base.DynamicVars[PuzzleNumKey].IntValue - 1);
            RefreshHpLossReduction();
        }
    }

    /// <summary>
    /// 指定谜题类型当前已完成的玩家数量（供漂浮演出按玩家比例计算透明度）。
    /// </summary>
    public int GetCompletedPlayerCount(int puzzleType)
    {
        if (puzzleType < 0 || puzzleType >= _completedPlayers.Length) return 0;
        return _completedPlayers[puzzleType].Count;
    }

    /// <summary>
    /// 指定谜题类型当前尚未完成的玩家显示名列表（多人模式悬停提示用）。
    /// 单人模式始终返回空列表（不显示悬停提示）。
    /// </summary>
    public IReadOnlyList<string> GetIncompletePlayerNames(int puzzleType)
    {
        if (puzzleType < 0 || puzzleType >= _completedPlayers.Length) return Array.Empty<string>();

        ICombatState? combatState = base.Owner.CombatState;
        if (combatState == null || combatState.Players.Count <= 1)
        {
            return Array.Empty<string>();
        }

        HashSet<Player> completed = _completedPlayers[puzzleType];
        return combatState.Players
            .Where(p => !completed.Contains(p))
            .Select(p => p.Creature.Name)
            .ToList();
    }

    /// <summary>
    /// 刷新总减伤值：未解开谜题数 × 每道谜题的减伤量（Amount）。
    /// </summary>
    private void RefreshHpLossReduction()
    {
        int puzzleNum = base.DynamicVars[PuzzleNumKey].IntValue;
        base.DynamicVars[HpLossReductionKey].BaseValue = puzzleNum * Amount;
        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 创建独立的完成追踪数组（每个克隆实例一份，避免共享 canonical 的引用）。
    /// </summary>
    protected override void DeepCloneFields()
    {
        base.DeepCloneFields();
        _completedPlayers = CreateCompletionTracking();
    }

    private static HashSet<Player>[] CreateCompletionTracking()
    {
        HashSet<Player>[] tracking = new HashSet<Player>[PuzzleTypeCount];
        for (int i = 0; i < tracking.Length; i++)
        {
            tracking[i] = new HashSet<Player>();
        }

        return tracking;
    }
}