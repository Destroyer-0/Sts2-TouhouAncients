using BaseLib.Config;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.Enchantment;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer(nameof(Init))]
public class Entry
{
    // 初始化函数
    public static void Init()
    {
        // 注册 Mod 中的 [SavedProperty] 类型到缓存中，
        // 否则读档时 SavedProperties 无法正确地序列化/反序列化这些属性。
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(HighQuality));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BrainInAVat));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(Tribute));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(LoseMoney));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(InkDyedCherryBlossoms));
        
        // 打patch（即修改游戏代码的功能）用
        // 传入参数随意，只要不和其他人撞车即可
        var harmony = new Harmony("sts2.reme.TouhouAncients");
        harmony.PatchAll();
        // 使得tscn可以加载自定义脚本
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        // Mod 配置：只需创建 TouhouAncientsConfig 类，BaseLib 自动发现并注册
        Log.Info("Mod initialized!");
        ModConfigRegistry.Register("TouhouAncients", new TouhouAncientsConfig());
    }
}