using System.Collections.Generic;
using BaseLib.Config;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.Enchantment;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.Patches;
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
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(FourLeafClover));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(InkDyedCherryBlossoms));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(KonshiiNoKusuri));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(KompeitoPot));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(StardustBroom));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(WindPriestessWine));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(DuplexBarrier));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BottomlessStomach));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BloodlickingTongue));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(SkySwallowingSpoon));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(EstrangedHeart));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BottledGalaxy));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(OilFutures));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(JyoonFan));
        
        
        // 打patch（即修改游戏代码的功能）用
        // 传入参数随意，只要不和其他人撞车即可
        var harmony = new Harmony("sts2.reme.TouhouAncients");
        harmony.PatchAll();
        // 订阅战斗开始/结束事件，为挑战战斗（YorigamiSistersEncounter）播放自定义 BGM
        EncounterBgm.Initialize();
        // 注册挑战 Encounter 为 RunState Hook 监听者：使其 TryModifyRewards 生效
        // （挑战战斗不生成默认战斗奖励，只保留 StartChallenge 传入的挑战遗物）
        ModHelper.SubscribeForRunStateHooks("touhouancients.challenge", ChallengeRunStateHooks);
        // 使得tscn可以加载自定义脚本
        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        // Mod 配置：只需创建 TouhouAncientsConfig 类，BaseLib 自动发现并注册
        Log.Info("Mod initialized!");
        ModConfigRegistry.Register("TouhouAncients", new TouhouAncientsConfig());
    }

    /// <summary>
    /// 返回当前挑战战斗的 Encounter（作为 RunState Hook 监听者）。ModHelper 的原生扩展点，
    /// 让 <see cref="TouhouAncientEncounter.TryModifyRewards"/> 在奖励生成时被调用。
    /// 按类型判断（TouhouAncientEncounter 只用于挑战战斗），正常/读档奖励流程都生效。
    /// </summary>
    private static IEnumerable<AbstractModel> ChallengeRunStateHooks(RunState runState)
    {
        if (runState.CurrentRoom is CombatRoom { Encounter: TouhouAncientEncounter } combatRoom)
        {
            yield return combatRoom.Encounter;
        }
    }
}