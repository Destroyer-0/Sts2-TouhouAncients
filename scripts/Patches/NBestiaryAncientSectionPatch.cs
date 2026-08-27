using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.Bestiary;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.Patches;

/// <summary>
/// 在怪物图鉴（Bestiary）最顶部追加"先古之民"分区，列出所有 <see cref="TouhouAncientMonsterBase"/>。
/// 原版图鉴只遍历各幕（Act）的遭遇，本 Mod 的挑战怪物不属于任何幕，因此不会出现；
/// 本补丁在 <see cref="NBestiary.CreateEntries"/> 完成后，把分区标题与怪物条目插入侧边栏列表最上方，
/// 点击条目时通过反射调用原版私有 OnMonsterClicked，完全复用原版的选择/预览/技能列表逻辑。
/// </summary>
[HarmonyPatch(typeof(NBestiary), "CreateEntries")]
internal static class NBestiaryAncientSectionPatch
{
    /// <summary>分区标题本地化键（monsters.json）。</summary>
    private static readonly LocString _sectionTitle =
        new LocString("monsters", "TOUHOUANCIENTS-ANCIENT_SECTION.title");

    /// <summary>
    /// 先古之民分区内怪物的展示顺序（顶部 → 底部）。
    /// 新增 <see cref="TouhouAncientMonsterBase"/> 怪物时，在此列表末尾追加其 (怪物类型, 遭遇类型) 即可。
    /// </summary>
    private static readonly (Type Monster, Type Encounter)[] AncientMonsters =
    [
        (typeof(HakureiReimuMonster), typeof(HakureiReimuEncounter)),
        (typeof(KirisameMarisaMonster), typeof(KirisameMarisaEncounter)),
        (typeof(FantasyMushroomMonster), typeof(KirisameMarisaEncounter)),
        (typeof(YorigamiJoonMonster), typeof(YorigamiSistersEncounter)),
        (typeof(YorigamiShionMonster), typeof(YorigamiSistersEncounter)),
        (typeof(HouraisanKaguyaMonster), typeof(HouraisanKaguyaEncounter)),
        (typeof(ToutetsuYuumaMonster), typeof(ToutetsuYuumaEncounter)),
    ];

    [HarmonyPostfix]
    private static void AfterCreateEntries(NBestiary __instance)
    {
        VBoxContainer? bestiaryList = Traverse.Create(__instance).Field("_bestiaryList").GetValue<VBoxContainer>();
        if (bestiaryList == null)
        {
            return;
        }

        // 已发现怪物集合（TotalWins > 0），与原版"未见过"灰色显示逻辑保持一致
        HashSet<ModelId>? discoveredMonsterIds =
            Traverse.Create(__instance).Field("_discoveredMonsterIds").GetValue<HashSet<ModelId>>();

        // 先收集本分区所有节点，再统一移动到列表最顶部，避免逐个插入导致顺序颠倒
        List<Node> sectionNodes = new List<Node> { CreateDivider() };
        // 每个条目是否应用金色标题（与 sectionNodes 索引一一对应；奇幻蘑菇保持原版奶油色）
        List<bool> goldTitles = new List<bool> { false };

        foreach ((Type monsterType, Type encounterType) in AncientMonsters)
        {
            MonsterModel? monster = ModelDb.GetByIdOrNull<MonsterModel>(ModelDb.GetId(monsterType));
            EncounterModel? encounter = ModelDb.GetByIdOrNull<EncounterModel>(ModelDb.GetId(encounterType));
            if (monster == null || encounter == null || !monster.ShouldShowInCompendium)
            {
                continue;
            }

            BestiaryEntry entry = BestiaryEntry.FromMonster(monster, encounter, RoomType.Monster);
            bool isDiscovered = discoveredMonsterIds?.Contains(monster.Id) ?? false;
            NBestiaryEntry node = NBestiaryEntry.Create(entry, isDiscovered);
            node.Connect(NClickableControl.SignalName.Released,
                Callable.From<NBestiaryEntry>(clicked => OnMonsterClicked(__instance, clicked)));
            sectionNodes.Add(node);
            // 挑战本体使用金色标题（区别于原版精英紫、Boss 红），非主怪（如奇幻蘑菇）保持默认奶油色
            goldTitles.Add(isDiscovered && monster is TouhouAncientMonsterBase ancientMonster && ancientMonster.IsPrimaryMonster);
        }

        foreach (Node node in sectionNodes)
        {
            bestiaryList.AddChildSafely(node);
        }

        // 节点已进入树（_Ready 已按 roomType 设置默认色），对挑战本体覆盖为金色标题
        for (int i = 0; i < sectionNodes.Count; i++)
        {
            if (goldTitles[i] && sectionNodes[i] is NBestiaryEntry entryNode)
            {
                entryNode.GetNode<MegaRichTextLabel>("%Label").SelfModulate = StsColors.gold;
            }
        }

        // 逆序逐个移动到索引 0，使整个分区位于列表最顶部且保持正确顺序
        for (int i = sectionNodes.Count - 1; i >= 0; i--)
        {
            bestiaryList.MoveChildSafely(sectionNodes[i], 0);
        }
    }

    /// <summary>
    /// 创建"先古之民"分区标题节点，样式复刻原版 bestiary_act_divider 场景
    /// （kreon 粗体 32 号、蓝色、带阴影，280x64 垂直居中）。
    /// </summary>
    private static Control CreateDivider()
    {
        Control divider = new Control
        {
            CustomMinimumSize = new Vector2(280, 64),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        MegaRichTextLabel label = new MegaRichTextLabel
        {
            ClipContents = false,
            CustomMinimumSize = new Vector2(0, 36),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            VerticalAlignment = VerticalAlignment.Center,
            AutoSizeEnabled = false,
            MinFontSize = 28,
            MaxFontSize = 32,
            Modulate = StsColors.blue
        };
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.501961f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 3);
        label.AddThemeFontOverride("normal_font", GD.Load<Font>("res://themes/kreon_bold_glyph_space_two.tres"));
        label.AddThemeFontSizeOverride("normal_font_size", 32);
        label.AddThemeFontSizeOverride("bold_font_size", 32);
        label.AddThemeFontSizeOverride("bold_italics_font_size", 32);
        label.AddThemeFontSizeOverride("italics_font_size", 32);
        label.AddThemeFontSizeOverride("mono_font_size", 32);
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        label.Text = _sectionTitle.GetFormattedText();

        divider.AddChild(label);
        return divider;
    }

    /// <summary>点击分区内条目时，调用原版私有 OnMonsterClicked 复用完整的选择/预览逻辑。</summary>
    private static void OnMonsterClicked(NBestiary bestiary, NBestiaryEntry entry)
    {
        AccessTools.Method(typeof(NBestiary), "OnMonsterClicked").Invoke(bestiary, new object[] { entry });
    }
}
