using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TouhouAncients.Scripts;

/// <summary>
/// 「离开梦境」的场景溶解演出：把哆来咪房间整棵子树搬进 <see cref="CanvasGroup"/>，
/// 再按帧把溶解着色器的 <c>threshold</c> 从 1 推到 0，画面像一滴墨从中心洇开那样化掉，
/// 逐渐露出垫在下面、已经就位的涅奥房间。
///
/// 用 CanvasGroup 而不是 SubViewport 离屏渲染：SubViewport 默认与主视口共享 World2D
/// （2D 的 canvas 就挂在 World2D 上），旧房间不会真正离开主画面，还会盖住覆盖层。
/// 着色器见 <c>res://shaders/ink_dissolve.gdshader</c>。
/// </summary>
public partial class NLeaveDreamDissolve : Control
{
    /// <summary>溶解时长（秒）。</summary>
    private const float FadeDuration = 1.0f;

    /// <summary>开演前的等待（秒）：等 CanvasGroup 完成首次合成，也等涅奥房间铺好画面。</summary>
    private const float WarmupSeconds = 0.4f;

    /// <summary>溶解着色器路径。</summary>
    private const string ShaderPath = "res://shaders/ink_dissolve.gdshader";

    /// <summary>溶解进度：1 表示完全可见，0 表示完全化开。</summary>
    private static readonly StringName Threshold = new StringName("threshold");

    /// <summary>溶解噪声贴图。</summary>
    private static readonly StringName InkNoise = new StringName("ink_noise");

    /// <summary>
    /// 在 <paramref name="newRoom"/> 之上播放溶解演出。
    /// <paramref name="newRoom"/> 必须已经挂在房间容器里（位于覆盖层之下）。
    /// </summary>
    public static async Task Play(NEventRoom oldRoom, NEventRoom newRoom)
    {
        var container = oldRoom.GetParent();
        if (container == null)
        {
            Log.Error("[TouhouAncients] 离开梦境：事件房间不在房间容器里，跳过溶解演出。");
            return;
        }

        // 屏幕像素尺寸：靠它把覆盖层与离屏容器铺满（不依赖父节点矩形，理由见 FillScreen 注释）。
        var screenSize = container.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);

        // 覆盖层铺满整个屏幕，并吃掉鼠标事件（演出期间不应该还能点到任何东西）。
        var overlay = new NLeaveDreamDissolve { MouseFilter = MouseFilterEnum.Stop };
        container.AddChildSafely(overlay);
        FillScreen(overlay, screenSize);

        // 材质加载失败时直接切场景（错误日志由 CreateInkDissolveMaterial 打出）。
        var material = CreateInkDissolveMaterial();

        var group = new CanvasGroup();
        if (material != null)
        {
            group.Material = material;
        }
        overlay.AddChildSafely(group);

        // 关键：搬进 CanvasGroup（Node2D，不是 Control）后要显式把尺寸恢复成屏幕大小，
        // 否则会被算成 0×0（父不是 Control，搬移那一刻又可能还没进场景树）。
        oldRoom.Reparent(group, keepGlobalTransform: false);
        FillScreen(oldRoom, screenSize);
        oldRoom.Visible = true;

        newRoom.Visible = true;

        // 等画面就绪再开演（理由见 WarmupSeconds）。
        await WaitSecondsAsync(overlay, WarmupSeconds);

        if (material != null)
        {
            await DriveDissolveAsync(overlay, material);
        }
        else
        {
            // 没法溶解时直接收起旧房间，容器里就只剩垫在下面的涅奥房间。
            group.Visible = false;
        }

        // 释放覆盖层（连同里面的旧房间）。不能交给 SetCurrentScene：
        // 那会连容器里的涅奥房间一起 QueueFree（详见 LeaveDreamSequence.RegisterCurrentRoom）。
        if (GodotObject.IsInstanceValid(overlay))
        {
            overlay.GetParent()?.RemoveChildSafely(overlay);
            overlay.QueueFreeSafely();
        }
    }

    /// <summary>
    /// 把 <paramref name="control"/> 铺满屏幕。
    ///
    /// 不用 <c>SetAnchorsPreset(FullRect)</c>：它要靠父矩形反推 offsets，而 <c>AddChildSafely</c>
    /// 可能把 AddChild 推迟到帧末，此刻节点还在树外、父矩形为空，尺寸会被永久锁成 0×0。
    /// 改为锤点钉在左上角，矩形完全由 Position / Size 决定。
    /// </summary>
    private static void FillScreen(Control control, Vector2 screenSize)
    {
        control.SetAnchorsPreset(LayoutPreset.TopLeft);
        control.Position = Vector2.Zero;
        control.Size = screenSize;
    }

    /// <summary>等待指定秒数；节点已释放时直接返回。</summary>
    private static async Task WaitSecondsAsync(Node node, float seconds)
    {
        if (!GodotObject.IsInstanceValid(node))
        {
            return;
        }

        var timer = node.GetTree()?.CreateTimer(seconds);
        if (timer == null)
        {
            return;
        }

        await node.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
    }

    /// <summary>
    /// 按帧把 <c>threshold</c> 从 1 推到 0。
    ///
    /// 不用 <c>TweenProperty</c> 补间 shader 参数：对运行时创建的 <c>ShaderMaterial</c>，
    /// 属性路径补间会瞬间跑完（threshold 直接跳到 0），看不到渐变。
    /// </summary>
    private static async Task DriveDissolveAsync(Node driver, ShaderMaterial material)
    {
        var elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            if (!GodotObject.IsInstanceValid(material) || !GodotObject.IsInstanceValid(driver))
            {
                return;
            }

            await driver.AwaitProcessFrame();
            elapsed += (float)driver.GetProcessDeltaTime();

            var progress = Mathf.Min(1f, elapsed / FadeDuration);
            // EaseOutSine：开头快、结尾缓，观感更接近原版的怪物风化。
            var eased = Mathf.Sin(progress * Mathf.Pi * 0.5f);
            material.SetShaderParameter(Threshold, 1f - eased);
        }

        material.SetShaderParameter(Threshold, 0f);
    }

    /// <summary>构造溶解材质（从 <see cref="ShaderPath"/> 加载着色器）；加载失败时返回 null。</summary>
    private static ShaderMaterial? CreateInkDissolveMaterial()
    {
        var shader = GD.Load<Shader>(ShaderPath);
        if (shader == null)
        {
            Log.Error($"[TouhouAncients] 离开梦境：加载溶解着色器失败（{ShaderPath}），跳过溶解演出。");
            return null;
        }

        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter(InkNoise, CreateInkNoiseTexture());
        material.SetShaderParameter(Threshold, 1.0f);
        return material;
    }

    /// <summary>生成无缝噪声贴图（无缝是不让 repeat_enable 采样出现明显接缝）。</summary>
    private static NoiseTexture2D CreateInkNoiseTexture()
    {
        return new NoiseTexture2D
        {
            Noise = new FastNoiseLite
            {
                Frequency = 0.012f,
                FractalType = FastNoiseLite.FractalTypeEnum.Fbm,
                FractalOctaves = 3,
            },
            Seamless = true,
        };
    }
}
