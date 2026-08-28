using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 蓬莱山辉夜怪物的视觉辅助脚本，挂在 HouraisanKaguyaMonster.tscn 根节点上。
/// 负责"五道难题"的漂浮谜题演出：
/// 1. 导出 PuzzleRoot（五道谜题图标根节点）及其 5 个 Sprite2D 子节点（Puzzle0~Puzzle4），
///    图标复用同名遗物图标（龙颈之玉/火鼠的皮衣/燕之子安贝/佛御石之钵/蓬莱的玉枝）；
/// 2. 提供 ShowPuzzles()：释放五道难题时显示谜题根节点、5 个图标淡入，
///    并播放 AnimationPlayer 循环动画（五件宝物绕辉夜做带远近变化的旋转）；
/// 3. 提供 UpdatePuzzleTransparency(puzzleType, completedCount, playerCount)：
///    多人模式下某谜题被解开一部分时透明度等比例下降，全部玩家解开后置为不可见；
/// 4. 多人模式下鼠标悬停谜题图标时，以 HoverTip 显示尚未完成该谜题的玩家名。
/// </summary>
public partial class HouraisanKaguyaVisuals : NCreatureVisuals
{
    /// <summary>五道难题谜题数量。</summary>
    private const int PuzzleCount = 5;

    /// <summary>循环旋转动画名（AnimationPlayer 中定义）。</summary>
    private const string OrbitAnimationName = "puzzle_orbit";

    /// <summary>谜题图标淡入时长（秒）。</summary>
    private const float FadeInSeconds = 0.5f;

    /// <summary>谜题图标透明度渐变时长（秒）。</summary>
    private const float FadeOutSeconds = 0.4f;

    /// <summary>本地化表：怪物文本。</summary>
    private const string MonstersLocTable = "monsters";

    /// <summary>本地化键：未完成玩家悬停提示。</summary>
    private const string PuzzleHoverIncompleteKey =
        "TOUHOUANCIENTS-HOURAISAN_KAGUYA_MONSTER.puzzleHover.incomplete";

    /// <summary>谜题图标根节点（PuzzleRoot），控制整体显隐。</summary>
    public Node2D? PuzzleRoot { get; set; }

    /// <summary>循环旋转动画播放器（挂在 PuzzleRoot 下）。</summary>
    public AnimationPlayer? PuzzleAnimPlayer { get; set; }

    /// <summary>五个谜题图标（Puzzle0~Puzzle4）。</summary>
    private Sprite2D?[] _puzzles = new Sprite2D?[PuzzleCount];

    /// <summary>五个谜题图标的悬停命中区域（PuzzleN/Hitbox）。</summary>
    private Control?[] _puzzleHitboxes = new Control?[PuzzleCount];

    /// <summary>是否已连接悬停事件（仅多人模式）。</summary>
    private bool _hoverTipsConnected;

    public override void _Ready()
    {
        base._Ready();

        if (PuzzleRoot == null)
        {
            PuzzleRoot = Body.GetNodeOrNull<Node2D>("PuzzleRoot");
        }
        
        PuzzleRoot.Visible = false;

        if (PuzzleAnimPlayer == null)
        {
            PuzzleAnimPlayer = PuzzleRoot?.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
        }

        for (int i = 0; i < PuzzleCount; i++)
        {
            _puzzles[i] = PuzzleRoot?.GetNodeOrNull<Sprite2D>($"Puzzle{i}");
            _puzzleHitboxes[i] = PuzzleRoot?.GetNodeOrNull<Control>($"Puzzle{i}/Hitbox");
        }

        // 仅多人模式连接悬停提示（单人模式不响应悬停）
        if (GetPuzzlePlayerCount() > 1)
        {
            ConnectPuzzleHoverTips();
        }
    }

    /// <summary>
    /// 释放五道难题：显示谜题根节点、5 个图标透明度从 0 淡入到 1，
    /// 并开始播放五件宝物绕辉夜旋转的循环动画。
    /// </summary>
    public void ShowPuzzles()
    {
        if (PuzzleRoot == null)
        {
            return;
        }

        PuzzleRoot.Visible = true;

        for (int i = 0; i < PuzzleCount; i++)
        {
            Sprite2D? puzzle = _puzzles[i];
            if (puzzle == null)
            {
                continue;
            }

            puzzle.Visible = true;
            puzzle.Modulate = new Color(puzzle.Modulate.R, puzzle.Modulate.G, puzzle.Modulate.B, 0f);
            puzzle.CreateTween().TweenProperty(puzzle, "modulate:a", 1f, FadeInSeconds);
        }

        if (PuzzleAnimPlayer != null && !PuzzleAnimPlayer.IsPlaying())
        {
            PuzzleAnimPlayer.Play(OrbitAnimationName);
        }
    }

    /// <summary>
    /// 更新指定谜题的透明度：多人模式下每被一名玩家解开，透明度等比例下降
    /// （alpha = 1 - completedCount / playerCount）；全部玩家解开后置为不可见。
    /// </summary>
    public void UpdatePuzzleTransparency(int puzzleType, int completedCount, int playerCount)
    {
        if (puzzleType < 0 || puzzleType >= PuzzleCount)
        {
            return;
        }

        Sprite2D? puzzle = _puzzles[puzzleType];
        if (puzzle == null)
        {
            return;
        }

        if (playerCount <= 0 || completedCount >= playerCount)
        {
            // 全部玩家已解开：淡出后隐藏
            puzzle.Visible = true;
            Tween hideTween = puzzle.CreateTween();
            hideTween.TweenProperty(puzzle, "modulate:a", 0f, FadeOutSeconds);
            hideTween.TweenCallback(Callable.From(() => puzzle.Visible = false));
            return;
        }

        // 部分玩家解开：透明度等比例下降
        float alpha = 1f - (float)completedCount / playerCount;
        puzzle.CreateTween().TweenProperty(puzzle, "modulate:a", alpha, FadeOutSeconds);
    }

    /// <summary>当前战斗玩家总数（无法获取时按 1 处理）。</summary>
    private int GetPuzzlePlayerCount()
    {
        return GetKaguyaMonster()?.Creature.CombatState?.Players.Count ?? 1;
    }

    /// <summary>通过场景父节点（NCreature）反查辉夜怪物模型。</summary>
    private HouraisanKaguyaMonster? GetKaguyaMonster()
    {
        return GetParent<NCreature>()?.Entity.Monster as HouraisanKaguyaMonster;
    }

    /// <summary>为每个谜题图标的命中区域连接鼠标悬停事件（幂等，仅多人模式调用）。</summary>
    private void ConnectPuzzleHoverTips()
    {
        if (_hoverTipsConnected)
        {
            return;
        }

        for (int i = 0; i < PuzzleCount; i++)
        {
            Control? hitbox = _puzzleHitboxes[i];
            if (hitbox == null)
            {
                continue;
            }

            int puzzleType = i;
            hitbox.MouseEntered += () => OnPuzzleMouseEntered(puzzleType);
            hitbox.MouseExited += () => OnPuzzleMouseExited(puzzleType);
        }

        _hoverTipsConnected = true;
    }

    /// <summary>
    /// 鼠标进入谜题图标：多人模式下显示 HoverTip，列出尚未完成该谜题的玩家名。
    /// 全部玩家已解开（图标已隐藏）时命中区域随父节点隐藏，不会触发。
    /// </summary>
    private void OnPuzzleMouseEntered(int puzzleType)
    {
        Control? hitbox = _puzzleHitboxes[puzzleType];
        HouraisanKaguyaMonster? kaguya = GetKaguyaMonster();
        if (hitbox == null || kaguya == null)
        {
            return;
        }

        IReadOnlyList<string> incompletePlayers = kaguya.GetIncompletePuzzlePlayerNames(puzzleType);
        if (incompletePlayers.Count == 0)
        {
            return;
        }

        LocString description = new LocString(MonstersLocTable, PuzzleHoverIncompleteKey);
        description.Add("PlayerNames", string.Join(", ", incompletePlayers));
        NHoverTipSet.CreateAndShow(hitbox, new HoverTip(description), HoverTip.GetHoverTipAlignment(hitbox))?.SetFollowOwner();
    }

    /// <summary>鼠标移出谜题图标：移除对应 HoverTip。</summary>
    private void OnPuzzleMouseExited(int puzzleType)
    {
        Control? hitbox = _puzzleHitboxes[puzzleType];
        if (hitbox == null)
        {
            return;
        }

        NHoverTipSet.Remove(hitbox);
    }
}
