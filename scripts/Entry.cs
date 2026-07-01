using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.Enchantment;
using TouhouAncients.Scripts.Patches;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer(nameof(Init))]
public class Entry
{
    public const string ModId = "TouhouAncients";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    // 初始化函数
    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 注册 Mod 中的 [SavedProperty] 类型到缓存中，
        // 否则读档时 SavedProperties 无法正确地序列化/反序列化这些属性。
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HighQuality));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BrainInAVat));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(Tribute));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(FourLeafClover));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(InkDyedCherryBlossoms));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(KonshiiNoKusuri));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(KompeitoPot));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(StardustBroom));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(RyukeiNoTama));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(WindPriestessWine));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(DuplexBarrier));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BottomlessStomach));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BloodlickingTongue));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(SkySwallowingSpoon));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(EstrangedHeart));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BottledGalaxy));
        
        
        // 打patch（即修改游戏代码的功能）用
        // 传入参数随意，只要不和其他人撞车即可
        var harmony = new Harmony("sts2.reme.TouhouAncients");
        harmony.PatchAll();

        // 注册 RitsuLib 反射式设置页面
        RitsuLibFramework.RegisterModSettingsReflectionProvider<TouhouAncientsConfig>();
        
        Logger.Info("Mod initialized!");
    }
}