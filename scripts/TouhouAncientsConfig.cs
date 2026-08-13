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
    //Mamizou猯藏
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

    /// <summary>
    /// 本次运行实际生效的强制 Ancient（运行期字段，非设置项，不写入配置文件）。
    /// BaseLib 配置是各端本地文件、不会网络同步，直接读 ForcedAncient_2/3 会导致联机不同步；
    /// 因此开局时由 ForcedAncientSyncPatch 将主机的配置写入这两个字段，
    /// 并随 LobbyBeginRunMessage 广播到所有客户端，ShouldForceSpawn 只读取这两个字段，
    /// 保证各端一致。非主机端的本地设置文件不被修改。
    /// [ConfigIgnore]：不显示在设置 UI，也不参与配置文件的读写。
    /// </summary>
    [ConfigIgnore]
    public static ForcedAncientOption ForcedAncient_2_Run { get; set; } = ForcedAncientOption.None;

    [ConfigIgnore]
    public static ForcedAncientOption ForcedAncient_3_Run { get; set; } = ForcedAncientOption.None;

    /// <summary>
    /// 本次运行实际生效的禁用掩码（运行期字段，非设置项，不写入配置文件）。
    /// 位 i 对应 <see cref="GetBanBit"/> 定义的 Ancient；null 表示未同步（回退本地配置）。
    /// 开局时由 ForcedAncientSyncPatch 将主机的禁用配置写入该字段并随 LobbyBeginRunMessage 广播，
    /// 保证各端候选池一致（BanAncientPatch 的 Transpiler 过滤依赖此字段）。
    /// [ConfigIgnore]：不显示在设置 UI，也不参与配置文件的读写。
    /// </summary>
    [ConfigIgnore]
    public static ulong? BannedMask_Run { get; set; }

    /// <summary>
    /// 由本地禁用配置构造运行期掩码（开局时由 ForcedAncientSyncPatch 调用）。
    /// 位序必须与 <see cref="GetBanBit"/> 保持一致。
    /// </summary>
    public static ulong BuildBannedMask()
    {
        ulong mask = 0;
        if (BanNonupeipe) mask |= 1UL << 0;
        if (BanVakuu) mask |= 1UL << 1;
        if (BanTanx) mask |= 1UL << 2;
        if (BanOrobus) mask |= 1UL << 3;
        if (BanPaerl) mask |= 1UL << 4;
        if (BanTezcataras) mask |= 1UL << 5;
        if (BanDarv) mask |= 1UL << 6;
        if (BanReimu) mask |= 1UL << 7;
        if (BanSanae) mask |= 1UL << 8;
        if (BanMarisa) mask |= 1UL << 9;
        if (BanSatori) mask |= 1UL << 10;
        if (BanTewi) mask |= 1UL << 11;
        if (BanSeija) mask |= 1UL << 12;
        if (BanMedicine) mask |= 1UL << 13;
        if (BanNina) mask |= 1UL << 14;
        if (BanRemilia) mask |= 1UL << 15;
        if (BanTenshi) mask |= 1UL << 16;
        if (BanYuyuko) mask |= 1UL << 17;
        if (BanKaguya) mask |= 1UL << 18;
        if (BanJunko) mask |= 1UL << 19;
        if (BanYuuma) mask |= 1UL << 20;
        if (BanYorigami) mask |= 1UL << 21;
        //if (BanMamizou) mask |= 1UL << 22;
        return mask;
    }

    /// <summary>
    /// 返回 Type 对应的禁用位（0~22）；未识别的 Ancient 返回 -1。
    /// 位序必须与 <see cref="BuildBannedMask"/> 保持一致。
    /// </summary>
    private static int GetBanBit(Type type)
    {
        return type.Name switch
        {
            nameof(Nonupeipe) => 0,
            nameof(Vakuu) => 1,
            nameof(Tanx) => 2,
            nameof(Orobas) => 3,
            nameof(Pael) => 4,
            nameof(Tezcatara) => 5,
            nameof(Darv) => 6,
            nameof(HakureiReimuAncient) => 7,
            nameof(KotiyaSanaeAncient) => 8,
            nameof(KirisameMarisaAncient) => 9,
            nameof(KomejiSatoriAncient) => 10,
            nameof(InabaTewiAncient) => 11,
            nameof(KijinSeijaAncient) => 12,
            nameof(MedicineMelancholyAncient) => 13,
            nameof(WatariNinaAncient) => 14,
            nameof(RemiliaScarletAncient) => 15,
            nameof(HinanawiTenshiAncient) => 16,
            nameof(SaigyoujiYuyukoAncient) => 17,
            nameof(HouraisanKaguyaAncient) => 18,
            nameof(JunkoAncient) => 19,
            nameof(ToutetsuYuumaAncient) => 20,
            nameof(YorigamiSisterAncient) => 21,
            //nameof(FutatsuiwaMamizouAncient) => 22, // 猯藏 Ancient 类暂被注释，恢复时取消注释（BuildBannedMask 已预留第 22 位）
            _ => -1
        };
    }

    /// <summary>
    /// 检查某个 Ancient 是否被禁止（实例版本，委托 Type 版本统一走运行期掩码）
    /// </summary>
    public static bool IsAncientBanned(TouhouAncientBase type)
    {
        return IsAncientBanned(type.GetType());
    }

    /// <summary>
    /// 检查某个 Ancient 是否被禁止（Type 版本，供 Patch 使用）
    /// 运行期掩码优先（各端一致的主机配置）；未同步时回退本地配置。
    /// </summary>
    public static bool IsAncientBanned(Type type)
    {
        if (BannedMask_Run is ulong mask)
        {
            int bit = GetBanBit(type);
            return bit >= 0 && ((mask >> bit) & 1UL) != 0;
        }
        return type.Name switch
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
    /// 检查某个基础游戏 Ancient 是否被禁止（实例版本，委托 Type 版本统一走运行期掩码）
    /// </summary>
    public static bool IsBaseGameAncientBanned(AncientEventModel ancient)
    {
        return IsBaseGameAncientBanned(ancient.GetType());
    }

    /// <summary>
    /// 检查某个基础游戏 Ancient 是否被禁止（Type 版本，供 Patch 使用）
    /// 运行期掩码优先（各端一致的主机配置）；未同步时回退本地配置。
    /// </summary>
    public static bool IsBaseGameAncientBanned(Type type)
    {
        if (BannedMask_Run is ulong mask)
        {
            int bit = GetBanBit(type);
            return bit >= 0 && ((mask >> bit) & 1UL) != 0;
        }
        return type.Name switch
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
            2 => ForcedAncient_2_Run,
            3 => ForcedAncient_3_Run,
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
    //public static bool BanMamizou { get; set; } = false;

    /// <summary>
    /// 启用测试内容·二岩猯藏（勾选后二岩猯藏才会出现在游戏中）
    /// </summary>
    // [ConfigSection("TestContent")]
    // public static bool EnableTestContentMamizou { get; set; } = false;

}