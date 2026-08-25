using System;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace TouhouAncients.Scripts.Vfx;

/// <summary>
/// 博丽灵梦怪物的视觉辅助脚本，挂在 HakureiReimuMonster.tscn 根节点上。
/// 负责：
/// 1. 导出灵符（Amulet）及其 7 个子节点引用，供怪物 C# 控制甩出 / 激活 / 收回；
/// 2. 提供运行时构建"符纸燃烧"逐帧动画（amulet_orange2.png，6 帧）的方法。
/// </summary>
public partial class HakureiReimuVisuals : NCreatureVisuals
{
    private const int AmuletCount = 7;

    private const float BurnFps = 15f;

    private const string AmuletFramesPath = "res://images/sprite/reimu/AmuletAnim.tres";

    private const string BurnTexturePath = "res://images/sprite/reimu/amulet_orange2.png";

    /// <summary>灵符根节点（Amulet），控制整体显隐。</summary>
    public Node2D? AmuletRoot { get; set; }

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

        for (int i = 0; i < AmuletCount; i++)
        {
            var node = AmuletRoot?.GetNodeOrNull<AnimatedSprite2D>($"Amulet{i + 1}");
            _amulets[i] = new AmuletData(node, node.GlobalPosition - VfxSpawnPosition.GlobalPosition);
        }
    }

    /// <summary>
    /// 构建符纸燃烧逐帧动画（amulet_orange2.png，192×16 = 6 帧）。
    /// 若已存在同名动画则直接返回，避免重复创建。
    /// </summary>
    public SpriteFrames? GetOrCreateBurnAnimation()
    {
        Texture2D? texture = GD.Load<Texture2D>(BurnTexturePath);
        if (texture == null)
        {
            return null;
        }

        SpriteFrames frames = new SpriteFrames();
        int frameWidth = 32;
        int frameHeight = 16;
        int frameCount = texture.GetWidth() / frameWidth;

        for (int i = 0; i < frameCount; i++)
        {
            AtlasTexture atlas = new AtlasTexture
            {
                Atlas = texture,
                Region = new Rect2(i * frameWidth, 0, frameWidth, frameHeight)
            };
            frames.AddFrame("default", atlas);
        }
        frames.SetAnimationSpeed("default", BurnFps);
        return frames;
    }

    /// <summary>
    /// 在指定位置播放一次符纸燃烧特效（amulet_orange2.png 逐帧动画）。
    /// </summary>
    public void PlayBurnVfx(Vector2 globalPosition)
    {
        SpriteFrames? frames = GetOrCreateBurnAnimation();
        if (frames == null)
        {
            return;
        }

        AnimatedSprite2D burn = new AnimatedSprite2D
        {
            SpriteFrames = frames,
            Animation = "default",
            Position = ToLocal(globalPosition),
            ZIndex = 10
        };
        burn.SpriteFrames.SetAnimationLoop("default", false);
        AddChild(burn);
        burn.AnimationFinished += () =>
        {
            if (GodotObject.IsInstanceValid(burn))
            {
                burn.QueueFree();
            }
        };
        burn.Play();
    }
}
