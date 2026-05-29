using System;
using System.Collections.Generic;
using BaseLib.Config;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.Patches;

namespace TouhouAncients.Scripts;

/// <summary>
/// 强制出现先古之民选项（单选）
/// </summary>
public enum ForcedAncientOption
{
    None,
    Reimu灵梦,
    Sanae早苗,
    Remilia蕾米,
    Satori小五,
    Nina贝子,
    Tenshi天子,
    Tewi帝,
    Seija正邪,
    Medicine梅蒂欣,
    Yuyuko幽幽子,
    Kaguya辉夜,
    Marisa魔理沙
}

/// <summary>
/// TouhouAncients Mod 配置
/// </summary>
public class TouhouAncientsConfig : SimpleModConfig
{

    /// <summary>
    /// 强制出现的先古之民（单选，选中的一定会刷新）
    /// </summary>
    [ConfigSection("ForcedAncient_2")]
    public static ForcedAncientOption ForcedAncient_2 { get; set; } = ForcedAncientOption.None;

    [ConfigSection("ForcedAncient_3")] public static ForcedAncientOption ForcedAncient_3 { get; set; } = ForcedAncientOption.None;

    // Medicine 使用 ForcedAncient_2（Act 2 角色）

    /// <summary>
    /// 检查某个 Ancient 是否被强制出现
    /// </summary>
    public static bool IsAncientForced<T>(int actNumber) where T : AncientEventModel
    {
        var name = typeof(T).Name;
        var option = actNumber switch
        {
            2 => ForcedAncient_2,
            3 => ForcedAncient_3,
            _ => ForcedAncientOption.None
        };

        return option switch
        {
            ForcedAncientOption.Reimu灵梦 when name == nameof(HakureiReimuAncient) => true,
            ForcedAncientOption.Sanae早苗 when name == nameof(KotiyaSanaeAncient) => true,
            ForcedAncientOption.Remilia蕾米 when name == nameof(RemiliaScarletAncient) => true,
            ForcedAncientOption.Satori小五 when name == nameof(KomejiSatoriAncient) => true,
            ForcedAncientOption.Nina贝子 when name == nameof(WatariNinaAncient) => true,
            ForcedAncientOption.Tenshi天子 when name == nameof(HinanawiTenshiAncient) => true,
            ForcedAncientOption.Tewi帝 when name == nameof(InabaTewiAncient) => true,
            ForcedAncientOption.Medicine梅蒂欣 when name == nameof(MedicineMelancholyAncient) => true,
            ForcedAncientOption.Seija正邪 when name == nameof(KijinSeijaAncient) => true,
            ForcedAncientOption.Yuyuko幽幽子 when name == nameof(SaigyoujiYuyukoAncient) => true,
            ForcedAncientOption.Marisa魔理沙 when name == nameof(KirisameMarisaAncient) => true,
            ForcedAncientOption.Kaguya辉夜 when name == nameof(HouraisanKaguyaAncient) => true,
            _ => false
        };
    }

    /// <summary>
    /// 持久化存储被禁用的 Ancient 类型全名列表（逗号分隔），由 SimpleModConfig 自动序列化。
    /// 设置界面中的复选框由 SetupConfigUI 重写动态生成。
    /// </summary>
    [ConfigHideInUI]
    public static string BannedAncientData { get; set; } = "";

    /// <summary>
    /// 运行时被禁用的 Ancient Type.FullName 集合，由 BannedAncientData 同步。
    /// </summary>
    internal static readonly HashSet<string> BannedTypeNames = new();

    internal static void SyncBannedFromData()
    {
        BannedTypeNames.Clear();
        if (string.IsNullOrEmpty(BannedAncientData)) return;
        foreach (var name in BannedAncientData.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            BannedTypeNames.Add(name);
        }
    }

    private static void SyncDataFromBanned()
    {
        BannedAncientData = string.Join(",", BannedTypeNames);
    }

    /// <summary>
    /// 重写设置界面 UI 生成：先让基类自动生成 ForcedAncient 等属性，
    /// 再动态创建"禁用先古之民"分区，为每个扫描到的 Ancient 类型生成复选框。
    /// </summary>
    public override void SetupConfigUI(Control optionContainer)
    {
        GenerateOptionsForAllProperties(optionContainer);

        // 注意：不从 BannedAncientData 同步到 BannedTypeNames，
        // 避免清除已在 Toggled 事件中设置的条目。
        // 复选框使用 BannedTypeNames 的当前状态。

        var entries = BanAncientPatch.GetAllAncientEntries();
        if (entries.Count == 0) return;

        var section = CreateCollapsibleSection("BannedAncients");
        optionContainer.AddChild(section);

        foreach (var (type, title) in entries)
        {
            var fullName = type.FullName!;
            var checkBox = new CheckBox
            {
                Text = "禁用" + title,
                ButtonPressed = BannedTypeNames.Contains(fullName)
            };
            checkBox.Toggled += (pressed) =>
            {
                if (pressed)
                    BannedTypeNames.Add(fullName);
                else
                    BannedTypeNames.Remove(fullName);
                SyncDataFromBanned();
            };
            section.ContentContainer.AddChild(checkBox);
        }

        AddRestoreDefaultsButton(optionContainer);
        SetupFocusNeighbors(optionContainer);
    }
}