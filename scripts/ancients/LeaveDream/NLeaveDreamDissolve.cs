using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace TouhouAncients.Scripts;

/// <summary>
/// 「离开梦境」的场景溶解演出。
///
/// 做法：先把当前画面（哆来咪房间 + 那句告别台词）截成一张贴图，铺满屏幕盖在涅奥房间之上，
/// 再用原版溶解着色器 <c>res://shaders/dissolve.gdshader</c>（怪物死亡风化同一套）把
/// <c>threshold</c> 从 1 补间到 0，画面就会逐渐溶解消失，露出下面已经就位的涅奥房间。
///
/// 为什么不用 SubViewport 装实时画面：把整棵 <c>NEventRoom</c>（Control 树）塞进 SubViewport
/// 再用 ViewportTexture 显示，实测拿不到可见输出（玩家会直接看到涅奥房、毫无过渡），
/// 截图方案没有嵌套视口的布局/渲染时序问题，也更稳。
///
/// 生命周期：本节点把自己的覆盖层挂在房间容器里，**不自行释放** ——
/// 演出结束后由 <c>NRun.SetCurrentRoom</c> → <c>NSceneContainer.SetCurrentScene</c> 统一释放
/// （连同覆盖层与旧房间节点一起销毁）。
/// </summary>
public partial class NLeaveDreamDissolve : Control
{
    /// <summary>原版溶解着色器（与怪物死亡风化共用）。</summary>
    private const string DissolveShaderPath = "res://shaders/dissolve.gdshader";

    /// <summary>两层溶解噪声的频率，取自 <c>vfx_monster_death.tscn</c> 的曲线参数。</summary>
    private const float NoiseFrequency1 = 0.0234f;
    private const float NoiseFrequency2 = 0.0084f;

    /// <summary>溶解时长（秒）。</summary>
    private const float FadeDuration = 1.0f;

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

        // 覆盖层铺满整个房间容器，并吃掉鼠标事件（演出期间不应该还能点到任何东西）。
        var overlay = new NLeaveDreamDissolve { MouseFilter = MouseFilterEnum.Stop };
        container.AddChildSafely(overlay);
        overlay.SetAnchorsPreset(LayoutPreset.FullRect);

        var snapshot = await CaptureAsync(oldRoom);
        if (snapshot == null)
        {
            // 截图失败：退化为“旧房间逐渐淡出”的交叉淡化，至少保证有过渡。
            Log.Error("[TouhouAncients] 离开梦境：画面截图失败，改用淡出过渡。");
            overlay.QueueFreeSafely();
            await CrossFadeAsync(oldRoom, newRoom);
            return;
        }

        var material = CreateDissolveMaterial();
        var screen = new TextureRect
        {
            Texture = snapshot,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        if (material != null)
        {
            screen.Material = material;
        }
        else
        {
            // 着色器不可用：退化为整张贴图淡出（同样是“逐渐溶解露出涅奥房”的观感）。
            Log.Error($"[TouhouAncients] 离开梦境：加载溶解着色器失败（{DissolveShaderPath}），改用淡出过渡。");
        }

        screen.SetAnchorsPreset(LayoutPreset.FullRect);
        overlay.AddChildSafely(screen);

        // 旧房间的完整画面已经在截图里了，直接隐藏，避免它跟贴图叠出重影。
        oldRoom.Visible = false;
        newRoom.Visible = true;

        var tween = overlay.CreateTween();
        if (material != null)
        {
            // 用 TweenMethod 直接改着色器参数，避开属性路径补间对不上导致动画瞬间结束的坑。
            tween.TweenMethod(
                Callable.From<float>(value => material.SetShaderParameter("threshold", value)),
                1.0f,
                0.0f,
                FadeDuration);
        }
        else
        {
            tween.TweenProperty(screen, "modulate:a", 0.0f, FadeDuration);
        }
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        await tween.AwaitFinished(overlay);
    }

    /// <summary>截图失败时的兜底过渡：旧房间逐渐淡出，直接露出后面的涅奥房间。</summary>
    private static async Task CrossFadeAsync(NEventRoom oldRoom, NEventRoom newRoom)
    {
        newRoom.Visible = true;
        var tween = oldRoom.CreateTween();
        tween.TweenProperty(oldRoom, "modulate:a", 0.0f, FadeDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
        await tween.AwaitFinished(oldRoom);
    }

    /// <summary>截取当前画面（等帧绘制完成后再取，保证包含最新渲染内容）。</summary>
    private static async Task<ImageTexture?> CaptureAsync(Node node)
    {
        var viewport = node.GetViewport();
        if (viewport == null)
        {
            return null;
        }

        await viewport.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = viewport.GetTexture()?.GetImage();
        if (image == null)
        {
            return null;
        }

        return ImageTexture.CreateFromImage(image);
    }

    /// <summary>构造原版溶解材质（threshold = 1 表示完全可见，0 表示完全溶解）；着色器加载失败时返回 null。</summary>
    private static ShaderMaterial? CreateDissolveMaterial()
    {
        var shader = GD.Load<Shader>(DissolveShaderPath);
        if (shader == null)
        {
            return null;
        }

        var material = new ShaderMaterial { Shader = shader };
        material.SetShaderParameter("dissolveGradient1", CreateNoiseTexture(NoiseFrequency1));
        material.SetShaderParameter("dissolveGradient2", CreateNoiseTexture(NoiseFrequency2));
        material.SetShaderParameter("threshold", 1.0f);
        return material;
    }

    /// <summary>按指定频率生成一张溶解噪声贴图。</summary>
    private static NoiseTexture2D CreateNoiseTexture(float frequency)
    {
        return new NoiseTexture2D
        {
            Noise = new FastNoiseLite { Frequency = frequency },
        };
    }
}
