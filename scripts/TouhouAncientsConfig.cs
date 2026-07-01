using System;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Settings;

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
    Junko纯狐
}

/// <summary>
/// TouhouAncients Mod 配置（RitsuLib 反射式设置页）
/// </summary>
[ModSettingsPage(Entry.ModId)]
[ModSettingsSection("BannedAncients_BaseGame")]
[ModSettingsSection("BannedAncients_Mod")]
[ModSettingsSection("ForcedAncient")]
public static class TouhouAncientsConfig
{
    // ========== 原版先古之民禁用 ==========
    [ModSettingsToggle("banOrobus", "BannedAncients_BaseGame")]
    public static bool BanOrobus { get; set; } = false;

    [ModSettingsToggle("banTezcataras", "BannedAncients_BaseGame")]
    public static bool BanTezcataras { get; set; } = false;

    [ModSettingsToggle("banPaerl", "BannedAncients_BaseGame")]
    public static bool BanPaerl { get; set; } = false;

    [ModSettingsToggle("banDarv", "BannedAncients_BaseGame")]
    public static bool BanDarv { get; set; } = false;

    [ModSettingsToggle("banVakuu", "BannedAncients_BaseGame")]
    public static bool BanVakuu { get; set; } = false;

    [ModSettingsToggle("banNonupeipe", "BannedAncients_BaseGame")]
    public static bool BanNonupeipe { get; set; } = false;

    [ModSettingsToggle("banTanx", "BannedAncients_BaseGame")]
    public static bool BanTanx { get; set; } = false;

    [ModSettingsButton("banAllBasegame", "BannedAncients_BaseGame")]
    public static void BanAllBasegame()
    {
        BanNonupeipe = true;
        BanVakuu = true;
        BanOrobus = true;
        BanPaerl = true;
        BanTezcataras = true;
        BanDarv = true;
        BanTanx = true;
    }

    // ========== Mod 先古之民禁用 ==========
    [ModSettingsToggle("banReimu", "BannedAncients_Mod")]
    public static bool BanReimu { get; set; } = false;

    [ModSettingsToggle("banSanae", "BannedAncients_Mod")]
    public static bool BanSanae { get; set; } = false;

    [ModSettingsToggle("banMarisa", "BannedAncients_Mod")]
    public static bool BanMarisa { get; set; } = false;

    [ModSettingsToggle("banSatori", "BannedAncients_Mod")]
    public static bool BanSatori { get; set; } = false;

    [ModSettingsToggle("banTewi", "BannedAncients_Mod")]
    public static bool BanTewi { get; set; } = false;

    [ModSettingsToggle("banSeija", "BannedAncients_Mod")]
    public static bool BanSeija { get; set; } = false;

    [ModSettingsToggle("banMedicine", "BannedAncients_Mod")]
    public static bool BanMedicine { get; set; } = false;

    [ModSettingsToggle("banNina", "BannedAncients_Mod")]
    public static bool BanNina { get; set; } = false;

    [ModSettingsToggle("banRemilia", "BannedAncients_Mod")]
    public static bool BanRemilia { get; set; } = false;

    [ModSettingsToggle("banTenshi", "BannedAncients_Mod")]
    public static bool BanTenshi { get; set; } = false;

    [ModSettingsToggle("banYuyuko", "BannedAncients_Mod")]
    public static bool BanYuyuko { get; set; } = false;

    [ModSettingsToggle("banKaguya", "BannedAncients_Mod")]
    public static bool BanKaguya { get; set; } = false;

    [ModSettingsToggle("banJunko", "BannedAncients_Mod")]
    public static bool BanJunko { get; set; } = false;

    [ModSettingsToggle("banYuuma", "BannedAncients_Mod")]
    public static bool BanYuuma { get; set; } = false;

    // ========== 强制出现先古之民 ==========
    [ModSettingsChoice("forcedAncient_2", "ForcedAncient")]
    public static ForcedAncientOption ForcedAncient_2 { get; set; } = ForcedAncientOption.None;

    [ModSettingsChoice("forcedAncient_3", "ForcedAncient")]
    public static ForcedAncientOption ForcedAncient_3 { get; set; } = ForcedAncientOption.None;

    // ========== 静态工具方法 ==========

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
            _ => false
        };
    }

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
            _ => false
        };
    }

    public static bool IsAncientForced(TouhouAncientBase type, int actNumber)
    {
        var option = actNumber switch
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
            _ => false
        };
    }
}