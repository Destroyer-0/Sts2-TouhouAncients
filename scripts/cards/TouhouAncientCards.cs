using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace TouhouAncients.Scripts.cards;

/// <summary>远古框卡面的两层立绘：BasePath 恒铺底，OverlayPath 叠在上面按 OverlayAlpha 淡入。</summary>
public readonly record struct PortraitFade(string BasePath, string OverlayPath, float OverlayAlpha);

public abstract class TouhouAncientCards : CustomCardModel
{
    public override string PortraitPath => $"res://images/cards/{GetType().Name}.png";

    public virtual bool UseAncientFrame => false;

    /// <summary>
    /// 绘师（作者）名称。为 null 或空字符串时不显示绘师 HoverTip。
    /// </summary>
    public virtual string? Author => null;

    /// <summary>
    /// 只在卡节点上生效的淡入叠加层；其他视图（卡牌控制台、悬停预览等）走的仍是 <see cref="PortraitPath"/> 的换图逻辑。
    /// </summary>
    public virtual PortraitFade? PortraitFade => null;

    /// <summary>卡节点该用的底图：有叠加层时用恒定的那张（上层由节点自己叠）。</summary>
    internal Texture2D NodePortrait => PortraitFade is { } fade
        ? ResourceLoader.Load<Texture2D>(fade.BasePath)
        : Portrait;
    
    // /// <summary>
    // /// ModelDb 反射实例化所需的无参构造函数。
    // /// 实际使用时应调用带参构造函数。
    // /// </summary>
    // protected TouhouAncientCards() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.None, true)
    // {
    // }

    public TouhouAncientCards(int energyCost, CardType type, CardRarity rarity, TargetType targetType,
        bool shouldShowInCardLibrary) : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>提前停止 ModSound 音效并归还 BaseLib 播放器池；Stop() 不触发 Finished，必须手动脱树。</summary>
    protected void StopSfx(AudioStreamPlayer? player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            return;
        }

        player.Stop();
        player.GetParent()?.RemoveChild(player);
    }
}


[HarmonyPatch(typeof(NCard), "Reload")]
public static class AncientCardFramePatch
{
	private static readonly FieldInfo _portraitBorder = AccessTools.Field(typeof(NCard), "_portraitBorder");

	private static readonly FieldInfo _portrait = AccessTools.Field(typeof(NCard), "_portrait");

	private static readonly FieldInfo _frame = AccessTools.Field(typeof(NCard), "_frame");

	private static readonly FieldInfo _banner = AccessTools.Field(typeof(NCard), "_banner");

	private static readonly FieldInfo _ancientPortrait = AccessTools.Field(typeof(NCard), "_ancientPortrait");

	private static readonly FieldInfo _ancientBorderGlassOverlay = AccessTools.Field(typeof(NCard), "_ancientBorderGlassOverlay");

	private static readonly FieldInfo _ancientBorder = AccessTools.Field(typeof(NCard), "_ancientBorder");

	private static readonly FieldInfo _ancientTextBg = AccessTools.Field(typeof(NCard), "_ancientTextBg");

	private static readonly FieldInfo _ancientBanner = AccessTools.Field(typeof(NCard), "_ancientBanner");

	private static readonly FieldInfo _portraitCanvasGroup = AccessTools.Field(typeof(NCard), "_portraitCanvasGroup");

	private const string CanvasGroupMaskMaterialPath = "res://scenes/cards/card_canvas_group_mask_material.tres";

	private static bool Resolve(bool? nullable, bool master)
	{
		return nullable ?? master;
	}

	// 原版映射：None/Status/Curse 无专属底图退回 Skill，其余按自身类型取图
	private static string GetAncientTextBgPath(CardModel model)
	{
		CardType bgType = model.Type is CardType.Attack or CardType.Skill or CardType.Power or CardType.Quest
			? model.Type
			: CardType.Skill;
		return ImageHelper.GetImagePath($"atlases/compressed_atlas.sprites/ancient_text_bg_{bgType.ToString().ToLowerInvariant()}.png.tres");
	}

	private static string GetAncientBorderPath()
	{
		return ImageHelper.GetImagePath("atlases/compressed_atlas.sprites/ancient_card_border.png.tres");
	}

	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		if (!(__instance.Model is TouhouAncientCards { UseAncientFrame: var useAncientFrame } ancientCards))
		{
			return;
		}
		if (!useAncientFrame)
		{
			return;
		}
		TextureRect val = (TextureRect)_portraitBorder.GetValue(__instance);
		TextureRect val2 = (TextureRect)_portrait.GetValue(__instance);
		TextureRect val3 = (TextureRect)_frame.GetValue(__instance);
		TextureRect val4 = (TextureRect)_banner.GetValue(__instance);
		TextureRect val5 = (TextureRect)_ancientPortrait.GetValue(__instance);
		TextureRect val6 = (TextureRect)_ancientBorderGlassOverlay.GetValue(__instance);
		TextureRect val7 = (TextureRect)_ancientBorder.GetValue(__instance);
		TextureRect val8 = (TextureRect)_ancientTextBg.GetValue(__instance);
		Control val9 = (Control)_ancientBanner.GetValue(__instance);
		CanvasGroup val10 = (CanvasGroup)_portraitCanvasGroup.GetValue(__instance);
		if (val == null || val2 == null || val3 == null || val4 == null || val5 == null || val6 == null || val7 == null || val8 == null || val9 == null)
		{
			return;
		}
		if (useAncientFrame)
		{
			val.Visible = false;
			val3.Visible = false;
			val7.Visible = true;
			val6.Visible = true;
			val7.Texture = ResourceLoader.Load<Texture2D>(GetAncientBorderPath());
			if (val10 != null)
			{
				val10.Material = ResourceLoader.Load<Material>(CanvasGroupMaskMaterialPath);
			}
			val2.Visible = false;
			val5.Visible = true;
			val5.Texture = ancientCards.NodePortrait;
			val2.Material = null;
			val5.Material = null;
			val8.Visible = true;
			val8.Texture = ResourceLoader.Load<Texture2D>(GetAncientTextBgPath(ancientCards));
		
			val4.Visible = false;
			val9.Visible = true;
			val4.Material = null;
		}
	}
}

/// <summary>
/// 远古框卡的「两层立绘」：直接在 <c>%AncientPortrait</c> 下放个子 <c>TextureRect</c> 当叠加层，
/// 它天然与父节点同矩形、同变换，画在父节点之上，并被同一个 CanvasGroup 遮罩 / 模糊。
/// 另外远古框卡的 <c>Rarity</c> 不是 Ancient，原版 <c>UpdatePortrait</c> 不会写 <c>%AncientPortrait</c>，
/// 所以贴图目前在 <c>Reload</c>（见 <see cref="AncientCardFramePatch"/>）里赋一次；本条 postfix 提供战斗中刷新时机。
/// </summary>
[HarmonyPatch]
public static class AncientCardPortraitRefreshPatch
{
	private static readonly FieldInfo _ancientPortrait = AccessTools.Field(typeof(NCard), "_ancientPortrait");

	/// <summary>叠加层节点名，同时用于识别节点池复用后残留的层。</summary>
	private const string FadeLayerName = "PortraitFadeLayer";

	// 淡入补间时长（秒）。改成 0 即立即生效。
	private const float FadeTweenSeconds = 0.5f;

	private sealed class FadeState
	{
		/// <summary>当前补间的目标值；NaN = 还没给过。</summary>
		public float Target = float.NaN;

		public Tween? Tween;
	}

	/// <summary>按卡存状态：池化复用的节点不能接着上一张牌的目标值/补间。</summary>
	private static readonly ConditionalWeakTable<NCard, FadeState> FadeStates = new();

	/// <summary>换模型时清状态，让新牌直接拿终值而不是从上一张牌的透明度化过来。</summary>
	internal static void ResetFadeState(NCard card)
	{
		FadeStates.Remove(card);
	}

	/// <summary>必须在 <c>Reload</c> 正文（内含 <c>UpdatePortrait</c>）之前清，否则会清掉本次刚建好的状态。</summary>
	[HarmonyPatch(typeof(NCard), "Reload")]
	[HarmonyPrefix]
	private static void ReloadPrefix(NCard __instance)
	{
		ResetFadeState(__instance);
	}

	[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
	[HarmonyPostfix]
	private static void Postfix(NCard __instance)
	{
		if (_ancientPortrait.GetValue(__instance) is not TextureRect portrait) return;

		SyncFadeLayer(__instance, portrait, (__instance.Model as TouhouAncientCards)?.PortraitFade);

		// 立绘「换图」已停用：底图恒定，改由 Reload 赋值即可。若以后有卡的 PortraitPath 要在战斗中变化，恢复下面几行：
		// if (__instance.Model is not TouhouAncientCards { UseAncientFrame: true } cards) return;
		// Texture2D portraitTexture = cards.NodePortrait; // Registry 缓存：同一路径每次返回同一实例，引用相等就是没换图
		// if (ReferenceEquals(portrait.Texture, portraitTexture)) return;
		// portrait.Texture = portraitTexture;
		// ForceRedraw(portrait);
	}

	/// <summary>按模型的数据维护叠加层；<paramref name="fade"/> 为 null 时收起。</summary>
	private static void SyncFadeLayer(NCard card, TextureRect portrait, PortraitFade? fade)
	{
		TextureRect? layer = portrait.GetNodeOrNull<TextureRect>(FadeLayerName);
		bool hasState = FadeStates.TryGetValue(card, out FadeState? state);

		if (fade is not { } value)
		{
			state?.Tween?.Kill();
			FadeStates.Remove(card);
			if (layer != null) layer.Visible = false;
			return;
		}

		// 刚建层 / 刚从池里出来 / 换过模型：直接给终值；只有战斗中计数变化才补间
		bool fresh = layer == null || !layer.Visible || !hasState;
		if (!hasState)
		{
			state = new FadeState();
			FadeStates.Add(card, state);
		}

		if (layer == null)
		{
			// 不走 SetAnchorsPreset：它按父矩形反推 offsets，节点还没进树时父矩形为空
			layer = new TextureRect
			{
				Name = FadeLayerName,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				MouseFilter = Control.MouseFilterEnum.Ignore,
				AnchorRight = 1f,
				AnchorBottom = 1f,
			};
			portrait.AddChildSafely(layer);
		}

		Texture2D overlayTexture = ResourceLoader.Load<Texture2D>(value.OverlayPath);
		float alpha = Mathf.Clamp(value.OverlayAlpha, 0f, 1f);
		// 材质也要比：原版会在「未见过」时给立绘挂单向模糊材质，层不跟着换会一半清一半糊
		bool textureChanged = !ReferenceEquals(layer.Texture, overlayTexture)
			|| !ReferenceEquals(layer.Material, portrait.Material);
		if (textureChanged)
		{
			layer.Texture = overlayTexture;
			layer.Material = portrait.Material;
		}

		// 比目标值而不是当前值：补间途中每帧都在变，比当前值会把补间反复掐断重启
		if (!fresh && !textureChanged && Mathf.IsEqualApprox(state!.Target, alpha)) return;

		layer.Visible = true;
		state!.Target = alpha;
		state.Tween?.Kill();
		state.Tween = null;

		if (fresh || textureChanged || FadeTweenSeconds <= 0f)
		{
			layer.Modulate = new Color(1f, 1f, 1f, alpha);
			ForceRedraw(layer);
			return;
		}

		state.Tween = layer.CreateTween();
		state.Tween.TweenProperty(layer, "modulate:a", alpha, FadeTweenSeconds)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
	}

	/// <summary>抖一次可见性强制 <c>PortraitCanvasGroup</c> 重建：只改贴图值 / 材质参数不会重绘。</summary>
	private static void ForceRedraw(CanvasItem item)
	{
		item.Visible = false;
		item.Visible = true;
	}
}