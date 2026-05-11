using BaseLib.Config;
using MegaCrit.Sts2.Core.Models;

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
    Tenshi天子
}

/// <summary>
/// TouhouAncients Mod 配置
/// </summary>
public class TouhouAncientsConfig : SimpleModConfig
{
    /// <summary>
    /// 配置该列表中先古之民不出现
    /// </summary>
    [ConfigSection("BannedAncients")]
    public static bool BanReimu { get; set; } = false;
    public static bool BanSanae { get; set; } = false;
    public static bool BanRemilia { get; set; } = false;
    public static bool BanSatori { get; set; } = false;
    public static bool BanNina { get; set; } = false;
    public static bool BanTenshi { get; set; } = false;

    /// <summary>
    /// 强制出现的先古之民（单选，选中的一定会刷新）
    /// </summary>
    [ConfigSection("ForcedAncient")]
    public static ForcedAncientOption ForcedAncient { get; set; } = ForcedAncientOption.None;

    /// <summary>
    /// 检查某个 Ancient 是否被禁止
    /// </summary>
    public static bool IsAncientBanned<T>() where T : AncientEventModel
    {
        var name = typeof(T).Name;
        return name switch
        {
            nameof(HakureiReimuAncient) => BanReimu,
            nameof(KotiyaSanaeAncient) => BanSanae,
            nameof(RemiliaScarletAncient) => BanRemilia,
            nameof(KomejiSatoriAncient) => BanSatori,
            nameof(WatariNinaAncient) => BanNina,
            nameof(HinanawiTenshiAncient) => BanTenshi,
            _ => false
        };
    }

    /// <summary>
    /// 检查某个 Ancient 是否被强制出现
    /// </summary>
    public static bool IsAncientForced<T>() where T : AncientEventModel
    {
        var name = typeof(T).Name;
        return ForcedAncient switch
        {
            ForcedAncientOption.Reimu灵梦 when name == nameof(HakureiReimuAncient) => true,
            ForcedAncientOption.Sanae早苗 when name == nameof(KotiyaSanaeAncient) => true,
            ForcedAncientOption.Remilia蕾米 when name == nameof(RemiliaScarletAncient) => true,
            ForcedAncientOption.Satori小五 when name == nameof(KomejiSatoriAncient) => true,
            ForcedAncientOption.Nina贝子 when name == nameof(WatariNinaAncient) => true,
            ForcedAncientOption.Tenshi天子 when name == nameof(HinanawiTenshiAncient) => true,
            _ => false
        };
    }
}
