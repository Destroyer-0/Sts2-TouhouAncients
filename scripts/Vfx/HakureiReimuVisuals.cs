using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 博丽灵梦怪物的视觉辅助脚本，挂在 HakureiReimuMonster.tscn 根节点上。
/// 负责：
/// 1. 导出灵符（Amulet）及其 7 个子节点引用，供怪物 C# 控制甩出 / 激活 / 收回；
/// 2. 提供"符纸燃烧"特效（amuletTrigger.tscn 粒子序列帧，对象池复用，随本节点清除）的方法；
/// 3. 提供八方鬼缚阵的脚下魔法阵（MagicCircle，含 Spin 子节点）的召唤 / 扩散 / 渐隐控制。
/// </summary>
public partial class HakureiReimuVisuals : NCreatureVisuals
{
    private const int AmuletCount = 7;

    /// <summary>灵符根节点（Amulet），控制整体显隐。</summary>
    public Node2D? AmuletRoot { get; set; }

    /// <summary>脚下魔法阵根节点（MagicCircle），负责整体缩放与渐隐。</summary>
    public Node2D? MagicCircle { get; set; }

    /// <summary>魔法阵的旋转子节点（MagicCircle/Spin），单独旋转以免连带压扁轴一起转。</summary>
    private Sprite2D? _magicCircleSpin;

    /// <summary>魔法阵持续旋转的 Tween（重复调用需先 Kill）。</summary>
    private Tween? _magicCircleSpinTween;

    /// <summary>魔法阵展开后的目标缩放（y = 0.6，模拟沿 x 轴旋转后贴地）。</summary>
    private static readonly Vector2 MagicCircleScale = new(1.5f, 0.75f);

    public struct AmuletData
    {
        public AnimatedSprite2D? Sprite;
        public Vector2 Offset;

        public AmuletData(AnimatedSprite2D? sprite, Vector2 offset)
        {
            Sprite = sprite;
            Offset = offset;
        }
    }
    
    
    private AmuletData[] _amulets = new AmuletData[AmuletCount];

    /// <summary>符纸燃烧特效对象池：空闲待复用的实例。</summary>
    private readonly List<NAmuletTriggerVfx> _amuletTriggerPool = new();

    /// <summary>所有已创建的符纸燃烧特效实例（含播放中），退出场景树时统一销毁。</summary>
    private readonly List<NAmuletTriggerVfx> _amuletTriggerAll = new();

    /// <summary>已激活的灵符数量（用于幂等激活）。</summary>
    public int ActivatedCount { get; set; }

    /// <summary>重置激活计数（全部收回后调用）。</summary>
    public void ResetActivatedCount()
    {
        ActivatedCount = 0;
    }

    /// <summary>当前是否处于"梦想天生演出中"（攻击期间跳过收回/甩出等重复演出）。</summary>
    public bool IsPerformingFantasyNature { get; set; }

    /// <summary>第 index（0 基）张灵符节点，可能为 null（场景未配置）。</summary>
    public AmuletData GetAmulet(int index)
    {
        if (index < 0 || index >= AmuletCount)
        {
            throw new Exception("Index out of range");
        }
        return _amulets[index];
    }

    public override void _Ready()
    {
        base._Ready();

        if (AmuletRoot == null)
        {
            AmuletRoot = GetNodeOrNull<Node2D>("Amulet");
        }

        MagicCircle ??= GetNodeOrNull<Node2D>("MagicCircle");
        _magicCircleSpin = MagicCircle?.GetNodeOrNull<Sprite2D>("Spin");
        // 战斗开始时不显示，等八方鬼缚阵自己召唤
        if (MagicCircle != null)
        {
            MagicCircle.Visible = false;
        }

        for (int i = 0; i < AmuletCount; i++)
        {
            var node = AmuletRoot?.GetNodeOrNull<AnimatedSprite2D>($"Amulet{i + 1}");
            _amulets[i] = new AmuletData(node, node.GlobalPosition - VfxSpawnPosition.GlobalPosition);
        }
    }

    /// <summary>
    /// 死亡时把灵符根节点（Amulet）整体挂到指定身体节点下，
    /// 使其随身体一起被 NMonsterDeathVfx 风化溶解（否则灵符会随节点被瞬间删除）。
    /// </summary>
    public void ReparentAmuletTo(Node2D targetBody)
    {
        if (AmuletRoot == null || targetBody == null)
        {
            return;
        }
        Node? oldParent = AmuletRoot.GetParent();
        if (oldParent == targetBody)
        {
            return;
        }
        oldParent?.RemoveChild(AmuletRoot);
        targetBody.AddChild(AmuletRoot);
    }

    /// <summary>退出场景树时销毁所有已创建的符纸燃烧特效实例（含播放中），并清空对象池。</summary>
    public override void _ExitTree()
    {
        base._ExitTree();
        foreach (NAmuletTriggerVfx vfx in _amuletTriggerAll)
        {
            if (GodotObject.IsInstanceValid(vfx))
            {
                vfx.QueueFree();
            }
        }
        _amuletTriggerAll.Clear();
        _amuletTriggerPool.Clear();
    }

    /// <summary>
    /// 在指定位置播放一次符纸燃烧特效（amuletTrigger.tscn 粒子序列帧，6 帧）。
    /// 从对象池取用或创建实例，挂到战斗 VFX 容器（NCombatRoom.Instance.CombatVfxContainer）播放，
    /// 播完自动归还对象池复用；池随本节点（HakureiReimuVisuals）退出场景树时一并销毁。
    /// </summary>
    public void PlayBurnVfx(Vector2 globalPosition)
    {
        if (NCombatRoom.Instance?.CombatVfxContainer == null)
        {
            return;
        }

        NAmuletTriggerVfx vfx = GetOrCreateTrigger();
        vfx.PlayAt(globalPosition);
    }

    /// <summary>从对象池取一个闲置实例；没有则创建并订阅播放结束回调。</summary>
    private NAmuletTriggerVfx GetOrCreateTrigger()
    {
        if (_amuletTriggerPool.Count > 0)
        {
            NAmuletTriggerVfx vfx = _amuletTriggerPool[^1];
            _amuletTriggerPool.RemoveAt(_amuletTriggerPool.Count - 1);
            return vfx;
        }

        NAmuletTriggerVfx newVfx = NAmuletTriggerVfx.Create();
        newVfx.PlaybackFinished += OnTriggerPlaybackFinished;
        _amuletTriggerAll.Add(newVfx);
        return newVfx;
    }

    /// <summary>播放完毕：从战斗 VFX 容器移除，放回对象池供下次复用。</summary>
    private void OnTriggerPlaybackFinished(NAmuletTriggerVfx vfx)
    {
        if (!GodotObject.IsInstanceValid(vfx))
        {
            return;
        }

        vfx.GetParent()?.RemoveChildSafely(vfx);
        _amuletTriggerPool.Add(vfx);
    }

    /// <summary>
    /// 召唤脚下魔法阵：从极小在 <paramref name="growSeconds"/> 内展开，并持续在自身平面内旋转。
    /// </summary>
    public void ShowMagicCircle(float growSeconds)
    {
        if (MagicCircle == null || _magicCircleSpin == null)
        {
            return;
        }

        MagicCircle.Visible = true;
        MagicCircle.Modulate = new Color(1f, 1f, 1f, 1f);
        MagicCircle.Scale = MagicCircleScale * 0.05f;
        _magicCircleSpin.Scale = Vector2.One;

        MagicCircle.CreateTween().TweenProperty(MagicCircle, "scale", MagicCircleScale, growSeconds)
            .SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Quad);

        _magicCircleSpinTween?.Kill();
        _magicCircleSpinTween = _magicCircleSpin.CreateTween().SetLoops();
        // AsRelative：循环 tween 的起点会重设为当前 rotation，用绝对值只能在第一圈动
        _magicCircleSpinTween.TweenProperty(_magicCircleSpin, "rotation", Mathf.Tau, 2.5f).AsRelative();
    }
    
    /// <summary>魔法阵渐隐消失（<paramref name="seconds"/> 秒），结束后隐藏并停止旋转。</summary>
    public async Task HideMagicCircle(float seconds)
    {
        if (MagicCircle == null || !GodotObject.IsInstanceValid(MagicCircle))
        {
            return;
        }

        Tween fade = MagicCircle.CreateTween();
        fade.TweenProperty(_magicCircleSpin, "scale", Vector2.One * 2f, seconds).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Linear);
        fade.TweenProperty(MagicCircle, "modulate:a", 0f, seconds).SetTrans(Tween.TransitionType.Linear);
        await fade.AwaitFinished(MagicCircle);

        if (!GodotObject.IsInstanceValid(MagicCircle))
        {
            return;
        }

        MagicCircle.Visible = false;
        _magicCircleSpinTween?.Kill();
        _magicCircleSpinTween = null;
        if (_magicCircleSpin != null && GodotObject.IsInstanceValid(_magicCircleSpin))
        {
            _magicCircleSpin.Scale = Vector2.One;
            _magicCircleSpin.Rotation = 0f;
        }
    }
}
