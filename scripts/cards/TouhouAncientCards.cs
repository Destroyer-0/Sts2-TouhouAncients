using System.Reflection;
using BaseLib.Abstracts;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace TouhouAncients.Scripts.cards;

public abstract class TouhouAncientCards : CustomCardModel
{
    public override string PortraitPath => $"res://images/cards/{GetType().Name}.png";

    public virtual bool UseAncientFrame => false;
    
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

	private unsafe static string GetAncientTextBgPath(CardModel model)
	{
		CardType type = model.Type;
		if (1 == 0)
		{
		}
		CardType val = ((int)type != 0 && (int)type - 4 > 1) ? model.Type : ((CardType)2);
		if (1 == 0)
		{
		}
		CardType val2 = val;
		return ImageHelper.GetImagePath("atlases/compressed_atlas.sprites/ancient_text_bg_" + ((object)(*(CardType*)(&val2))/*cast due to .constrained prefix*/).ToString().ToLowerInvariant() + ".png.tres");
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
			val5.Texture = ancientCards.Portrait;
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