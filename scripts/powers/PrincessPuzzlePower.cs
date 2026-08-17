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

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 公主的谜题：辉夜的五道难题。
/// 每道未完成的谜题在辉夜回合开始时为她提供 12 点格挡。
/// 图标层数显示未解开的谜题数量（PuzzleNum）；某道谜题需要所有玩家都完成才算解开，
/// 解开后 PuzzleNum 减少并刷新格挡总量（GainBlock）。
/// 多人模式：施加时层数（Amount）自动乘玩家数，格挡总量按缩放后的 Amount 计算。
/// </summary>
public sealed class PrincessPuzzlePower : TouhouAncientPowerModel
{
    /// <summary>每道谜题的格挡量（初始 12）。</summary>
    private const int BaseBlockPerPuzzle = 12;

    /// <summary>初始未解开谜题数（五种谜题各一道）。</summary>
    private const int BasePuzzleNum = 5;

    /// <summary>谜题类型总数（0-4 五种）。</summary>
    private const int PuzzleTypeCount = 5;

    private const string BlockPerPuzzleKey = "Amount";

    private const string PuzzleNumKey = "PuzzleNum";

    private const string GainBlockKey = "GainBlock";

    /// <summary>
    /// 每种谜题类型（0-4）各玩家完成状态。
    /// 战斗开始后（AfterApplied）重置；克隆时重新创建以避免共享 canonical 的引用。
    /// </summary>
    private HashSet<Player>[] _completedPlayers = CreateCompletionTracking();

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    /// 图标显示未解开的谜题数量。
    /// </summary>
    public override int DisplayAmount => base.DynamicVars[PuzzleNumKey].IntValue;

    /// <summary>
    /// 多人模式：施加时层数自动乘玩家数。
    /// </summary>
    public override bool ShouldScaleInMultiplayer => true;

    public override decimal GetScaledAmountForMultiplayer(ICombatState combatState, Creature? applier, decimal amount,
        Creature target, CardModel? cardSource)
    {
        return amount * (decimal)combatState.Players.Count;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(BlockPerPuzzleKey, BaseBlockPerPuzzle),
        new DynamicVar(PuzzleNumKey, BasePuzzleNum),
        new DynamicVar(GainBlockKey, BaseBlockPerPuzzle * BasePuzzleNum)
    ];

    /// <summary>
    /// 首次施加时重置完成追踪并刷新格挡总量
    /// （此时层数 Amount 已应用多人缩放，格挡总量按缩放后的 Amount 计算）。
    /// </summary>
    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        foreach (HashSet<Player> completed in _completedPlayers)
        {
            completed.Clear();
        }

        RefreshGainBlock();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 辉夜回合开始时，为辉夜提供当前格挡总量（未解开谜题数 × 每道谜题格挡量 × 玩家数）。
    /// </summary>
    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner.IsDead)
        {
            return;
        }

        if (side != CombatSide.Player || CombatManager.Instance.PlayersTakingExtraTurn.Count > 0)
        {
            return;
        }

        await GainBlockOnTurnStart();
    }

    private async Task GainBlockOnTurnStart()
    {
        await CreatureCmd.GainBlock(base.Owner, base.DynamicVars[GainBlockKey].IntValue, ValueProp.Move, null);
    }

    /// <summary>
    /// 标记指定玩家完成指定类型的谜题。
    /// 当该类型的谜题首次达成"所有玩家都完成"时，未解开谜题数减一并刷新格挡总量。
    /// </summary>
    public void CompletePuzzle(int puzzleType, Player player)
    {
        if (puzzleType < 0 || puzzleType >= _completedPlayers.Length) return;
        if (!_completedPlayers[puzzleType].Add(player)) return;

        int playerCount = base.Owner.CombatState?.Players.Count ?? 1;
        if (_completedPlayers[puzzleType].Count >= playerCount)
        {
            base.DynamicVars[PuzzleNumKey].BaseValue = Math.Max(0, base.DynamicVars[PuzzleNumKey].IntValue - 1);
            RefreshGainBlock();
            InvokeDisplayAmountChanged();
        }
    }

    /// <summary>
    /// 刷新格挡总量：未解开谜题数 × 每道谜题格挡量 × 玩家数（缩放后的层数）。
    /// </summary>
    private void RefreshGainBlock()
    {
        int puzzleNum = base.DynamicVars[PuzzleNumKey].IntValue;
        int blockPerPuzzle = base.DynamicVars[BlockPerPuzzleKey].IntValue;
        int playerCount = Math.Max(1, base.Amount);
        base.DynamicVars[GainBlockKey].BaseValue = puzzleNum * blockPerPuzzle * playerCount;
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