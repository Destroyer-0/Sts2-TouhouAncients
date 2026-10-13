using System.Threading.Tasks;
using BaseLib.Audio;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TouhouAncients.Scripts;

/// <summary>
/// 「前去面见涅奥」的换场演出：一卷宣纸从右向左摊开（<c>akyuu_transition_mat</c>），
/// 卷轴左侧仍是原先的房间画面、右侧直接露出垫在下面的涅奥房间，交界处画卷筒并压一道阴影。
/// 约 1 秒走完。
///
/// 与「离开梦境」的溶解同构：材质挂在 <see cref="CanvasGroup"/> 上，着色器用
/// <c>hint_screen_texture</c> 从 backbuffer 读回旧房间画面；CanvasGroup 材质里的
/// TEXTURE 是空白默认纹理，不能用。
/// </summary>
public partial class NAkyuuScrollTransition : Control
{
    /// <summary>材质路径。</summary>
    private const string MaterialPath = "res://shaders/akyuu_transition_mat.tres";

    /// <summary>摊开用时（秒）。</summary>
    private const float RollSeconds = 1.0f;

    /// <summary>开演前的等待（秒）：等 CanvasGroup 完成首次合成，也等涅奥房间铺好画面。</summary>
    private const float WarmupSeconds = 0.4f;

    /// <summary>纸卷进度：0 全透明，1 摊满。</summary>
    private static readonly StringName Threshold = new StringName("threshold");

    /// <summary>翻页音效：摊开时播一次。</summary>
    private static readonly ModSound PageTurn = new("res://debug_audio/sfx/page_turn_sfx.wav");

    /// <summary>
    /// 在 <paramref name="newRoom"/> 之上播放卷轴演出；
    /// <paramref name="newRoom"/> 必须已经挂在房间容器里、且位于 <paramref name="oldRoom"/> 之下。
    /// </summary>
    public static async Task Play(NEventRoom oldRoom, NEventRoom newRoom)
    {
        var container = oldRoom.GetParent();
        if (container == null)
        {
            Log.Error("[TouhouAncients] 前去面见涅奥：事件房间不在房间容器里，跳过卷轴演出。");
            return;
        }

        // 屏幕像素尺寸：靠它把覆盖层与离屏容器铺满（不依赖父节点矩形，理由见 FillScreen 注释）。
        var screenSize = container.GetViewport()?.GetVisibleRect().Size ?? new Vector2(1920f, 1080f);

        // 覆盖层铺满整个屏幕，并吃掉鼠标事件（演出期间不应该还能点到任何东西）。
        var overlay = new NAkyuuScrollTransition { MouseFilter = MouseFilterEnum.Stop };
        container.AddChildSafely(overlay);
        FillScreen(overlay, screenSize);

        // 材质加载失败时直接换场（错误日志由 CreateMaterial 打出）。
        var material = CreateMaterial();

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
            PageTurn.Play(volumeMult: 1f);
            await DriveAsync(overlay, material, RollSeconds);
        }
        else
        {
            // 没法演出时直接收起旧房间，容器里就只剩垫在下面的涅奥房间。
            group.Visible = false;
        }

        // 释放覆盖层（连同里面的旧房间）。不能交给 SetCurrentScene：
        // 那会连容器里的涅奥房间一起 QueueFree（详见 LeaveDreamSequence.RegisterCurrentRoom）。
        Release(overlay);
    }

    /// <summary>
    /// 把 <paramref name="control"/> 铺满屏幕。
    ///
    /// 不用 <c>SetAnchorsPreset(FullRect)</c>：它要靠父矩形反推 offsets，而 <c>AddChildSafely</c>
    /// 可能把 AddChild 推迟到帧末，此刻节点还在树外、父矩形为空，尺寸会被永久锁成 0×0。
    /// 改为锚点钉在左上角，矩形完全由 Position / Size 决定。
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
    /// 按帧把 <c>threshold</c> 从 0 推到 1。
    ///
    /// 不用 <c>TweenProperty</c> 补间 shader 参数：对运行时创建的 <c>ShaderMaterial</c>，
    /// 属性路径补间会瞬间跑完，看不到渐变。
    /// </summary>
    private static async Task DriveAsync(Node driver, ShaderMaterial material, float duration)
    {
        var elapsed = 0f;
        while (elapsed < duration)
        {
            if (!GodotObject.IsInstanceValid(material) || !GodotObject.IsInstanceValid(driver))
            {
                return;
            }

            await driver.AwaitProcessFrame();
            elapsed += (float)driver.GetProcessDeltaTime();

            // EaseOutSine：开头快、收尾缓。
            var progress = Mathf.Min(1f, elapsed / duration);
            material.SetShaderParameter(Threshold, Mathf.Sin(progress * Mathf.Pi * 0.5f));
        }

        material.SetShaderParameter(Threshold, 1f);
    }

    /// <summary>释放覆盖层（连同被搬进它的旧房间）。</summary>
    private static void Release(Node overlay)
    {
        if (!GodotObject.IsInstanceValid(overlay))
        {
            return;
        }

        overlay.GetParent()?.RemoveChildSafely(overlay);
        overlay.QueueFreeSafely();
    }

    /// <summary>
    /// 构造纸卷材质。必须 Duplicate：材质资源是 <c>resource_local_to_scene</c> 且被缓存，
    /// 直接用会改到全局那一份的 threshold。
    /// </summary>
    private static ShaderMaterial? CreateMaterial()
    {
        var template = GD.Load<ShaderMaterial>(MaterialPath);
        if (template == null)
        {
            Log.Error($"[TouhouAncients] 前去面见涅奥：加载卷轴材质失败（{MaterialPath}），跳过卷轴演出。");
            return null;
        }

        var material = (ShaderMaterial)template.Duplicate();
        material.SetShaderParameter(Threshold, 0f);
        return material;
    }
}
