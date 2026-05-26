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
    Tenshi天子,
    Tewi帝,
    Seija正邪,
    Medicine梅蒂欣,
    Yuyuko幽幽子,
    Kaguya辉夜
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
            nameof(MedicineMelancholyAncient) => BanMedicine,
            nameof(HinanawiTenshiAncient) => BanTenshi,
            nameof(InabaTewiAncient) => BanTewi,
            nameof(KijinSeijaAncient) => BanSeija,
            nameof(SaigyoujiYuyukoAncient) => BanYuyuko,
            nameof(HouraisanKaguyaAncient) => BanKaguya,
            _ => false
        };
    }

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
            ForcedAncientOption.Kaguya辉夜 when name == nameof(HouraisanKaguyaAncient) => true,
            _ => false
        };
    }
    
    
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
    public static bool BanTewi { get; set; } = false;
    public static bool BanSeija { get; set; } = false;
    public static bool BanMedicine { get; set; } = false;
    public static bool BanYuyuko { get; set; } = false;
    public static bool BanKaguya { get; set; } = false;
}