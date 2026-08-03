using System;
using BaseLib.Config;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;

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
    Marisa魔理沙,
    Yuuma饕餮,
    Junko纯狐,
    Yorigami依神姐妹,
    Mamizou猯藏
}

/// <summary>
/// TouhouAncients Mod 配置
/// </summary>
public class TouhouAncientsConfig : SimpleModConfig
{

    /// <summary>
    /// 检查某个基础游戏 Ancient（原版先古之民）是否被配置为禁用
    /// </summary>
    public static bool IsBaseGameAncientBanned(AncientEventModel ancient)
    {
        return ancient switch
        {
            Nonupeipe => BanNonupeipe,
            Vakuu => BanVakuu,
            Orobas => BanOrobus,
            Pael => BanPaerl,
            Tezcatara => BanTezcataras,
            Darv => BanDarv,
            Tanx => BanTanx,
            _ => false
        };
    }
    
    /// <summary>
    /// 强制出现的先古之民（单选，选中的一定会刷新）
    /// </summary>
    [ConfigSection("ForcedAncient_2")]
    public static ForcedAncientOption ForcedAncient_2 { get; set; } = ForcedAncientOption.None;

    [ConfigSection("ForcedAncient_3")] public static ForcedAncientOption ForcedAncient_3 { get; set; } = ForcedAncientOption.None;

    /// <summary>
    /// 检查某个 Touhou Ancient 是否被禁止（实例模式匹配版本）
    /// </summary>
    public static bool IsAncientBanned(TouhouAncientBase type)
    {
        return type switch
        {
            HakureiReimuAncient  => BanReimu,
            KotiyaSanaeAncient  => BanSanae,
            RemiliaScarletAncient  => BanRemilia,
            KomejiSatoriAncient  => BanSatori,
            WatariNinaAncient  => BanNina,
            MedicineMelancholyAncient  => BanMedicine,
            HinanawiTenshiAncient  => BanTenshi,
            InabaTewiAncient  => BanTewi,
            KijinSeijaAncient  => BanSeija,
            SaigyoujiYuyukoAncient  => BanYuyuko,
            HouraisanKaguyaAncient  => BanKaguya,
            KirisameMarisaAncient  => BanMarisa,
            JunkoAncient  => BanJunko,
            ToutetsuYuumaAncient  => BanYuuma,
            YorigamiSisterAncient  => BanYorigami,
            //FutatsuiwaMamizouAncient  => !EnableTestContentMamizou || BanMamizou,
            _ => false
        };
    }

    /// <summary>
    /// 检查某个 Ancient 是否被禁止（Type 版本，供 Patch 使用）
    /// </summary>
    public static bool IsAncientBanned(Type type)
    {
        var name = type.Name;
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
            nameof(KirisameMarisaAncient) => BanMarisa,
            nameof(JunkoAncient) => BanJunko,
            nameof(ToutetsuYuumaAncient) => BanYuuma,
            nameof(YorigamiSisterAncient) => BanYorigami,
            //nameof(FutatsuiwaMamizouAncient) => !EnableTestContentMamizou || BanMamizou,
            _ => false
        };
    }

    /// <summary>
    /// 检查某个基础游戏 Ancient 是否被禁止（Type 版本，供 Patch 使用）
    /// </summary>
    public static bool IsBaseGameAncientBanned(Type type)
    {
        var name = type.Name;
        return name switch
        {
            nameof(Nonupeipe) => BanNonupeipe,
            nameof(Vakuu) => BanVakuu,
            nameof(Orobas) => BanOrobus,
            nameof(Pael) => BanPaerl,
            nameof(Tezcatara) => BanTezcataras,
            nameof(Darv) => BanDarv,
            nameof(Tanx) => BanTanx,
            _ => false
        };
    }
    
    /// <summary>
    /// 检查某个 Ancient 是否被强制出现（运行时类型版本）
    /// </summary>
    public static bool IsAncientForced(TouhouAncientBase type, int actNumber)
    {
        var option= actNumber switch
        {
            2 => ForcedAncient_2,
            3 => ForcedAncient_3,
            _ => ForcedAncientOption.None
        };

        return option switch
        {
            ForcedAncientOption.Reimu灵梦 when type is HakureiReimuAncient => true,
            ForcedAncientOption.Sanae早苗 when type is KotiyaSanaeAncient => true,
            ForcedAncientOption.Remilia蕾米 when type is RemiliaScarletAncient => true,
            ForcedAncientOption.Satori小五 when type is KomejiSatoriAncient => true,
            ForcedAncientOption.Nina贝子 when type is WatariNinaAncient => true,
            ForcedAncientOption.Tenshi天子 when type is HinanawiTenshiAncient => true,
            ForcedAncientOption.Tewi帝 when type is InabaTewiAncient => true,
            ForcedAncientOption.Medicine梅蒂欣 when type is MedicineMelancholyAncient => true,
            ForcedAncientOption.Seija正邪 when type is KijinSeijaAncient => true,
            ForcedAncientOption.Yuyuko幽幽子 when type is SaigyoujiYuyukoAncient => true,
            ForcedAncientOption.Marisa魔理沙 when type is KirisameMarisaAncient => true,
            ForcedAncientOption.Kaguya辉夜 when type is HouraisanKaguyaAncient => true,
            ForcedAncientOption.Yuuma饕餮 when type is ToutetsuYuumaAncient => true,
            ForcedAncientOption.Junko纯狐 when type is JunkoAncient => true,
            ForcedAncientOption.Yorigami依神姐妹 when type is YorigamiSisterAncient => true,
            //ForcedAncientOption.Mamizou猯藏 when type is FutatsuiwaMamizouAncient => EnableTestContentMamizou,
            _ => false
        };
    }
    
    
    
    /// <summary>
    /// 配置该列表中先古之民不出现
    /// </summary>
    [ConfigSection("BannedAncients")]
    
    /// <summary>
    /// 一键禁用所有原版先古之民（点击后自动将6个原版Ancient的禁用开关打开）
    /// </summary>
    [ConfigButton("BanAllBasegameAction")]
    public void BanAllBasegame()
    {
        BanNonupeipe = true;
        BanVakuu = true;
        BanOrobus = true;
        BanPaerl = true;
        BanTezcataras = true;
        BanDarv = true;
        BanTanx = true;
    }
    public static bool BanOrobus { get; set; } = false;
    public static bool BanTezcataras { get; set; } = false;
    public static bool BanPaerl { get; set; } = false;
    public static bool BanDarv { get; set; } = false;
    public static bool BanVakuu { get; set; } = false;
    public static bool BanNonupeipe { get; set; } = false;
    public static bool BanTanx { get; set; } = false;
    public static bool BanReimu { get; set; } = false;
    public static bool BanSanae { get; set; } = false;
    public static bool BanMarisa { get; set; } = false;
    public static bool BanSatori { get; set; } = false;
    public static bool BanTewi { get; set; } = false;
    public static bool BanSeija { get; set; } = false;
    public static bool BanMedicine { get; set; } = false;
    public static bool BanNina { get; set; } = false;
    public static bool BanRemilia { get; set; } = false;
    public static bool BanTenshi { get; set; } = false;
    public static bool BanYuyuko { get; set; } = false;
    public static bool BanKaguya { get; set; } = false;
    public static bool BanJunko { get; set; } = false;
    public static bool BanYuuma { get; set; } = false;
    public static bool BanYorigami { get; set; } = false;
    public static bool BanMamizou { get; set; } = false;

    /// <summary>
    /// 启用测试内容·二岩猯藏（勾选后二岩猯藏才会出现在游戏中）
    /// </summary>
    [ConfigSection("TestContent")]
    public static bool EnableTestContentMamizou { get; set; } = false;

}