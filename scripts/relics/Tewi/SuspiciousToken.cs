using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 可疑信物：在每个商店，你可以免费刷新一次商人出售的物品。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SuspiciousToken : TouhouAncientRelics
{
    /// <summary>本商店免费刷新是否已被使用。</summary>
    public bool CanRefresh => !TouhouAncients_FreeRefreshUsed;
    public bool TouhouAncients_FreeRefreshUsed { get; set; }

    /// <summary>当前商店的 MerchantRefreshEntry，供 UI 按钮查询状态。</summary>
    public MerchantRefreshEntry? RefreshEntry { get; set; }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (room is MerchantRoom)
        {
            TouhouAncients_FreeRefreshUsed = false;
            RefreshEntry = null;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 进入商店后创建MerchantRefreshEntry（若补丁已在Initialize时创建则跳过）。
    /// </summary>
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not MerchantRoom) return;
        if (base.Owner == null) return;

        // 补丁已在 NMerchantInventory.Initialize 中创建了入口，无需重复创建
        if (RefreshEntry != null) return;

        RefreshEntry = new MerchantRefreshEntry(base.Owner, this);
    }

    /// <summary>
    /// 执行商店物品刷新（仅数据层）。由 MerchantRefreshEntry 在购买时调用。
    /// </summary>
    public async Task DoRefresh(MerchantInventory merchantInventory)
    {
        if (TouhouAncients_FreeRefreshUsed) return;
        TouhouAncients_FreeRefreshUsed = true;

        var player = base.Owner;
        if (player == null) return;

        // 1. 刷新角色卡牌
        foreach (var entry in merchantInventory.CharacterCardEntries)
            entry.Populate();

        // 2. 刷新无色卡牌
        foreach (var entry in merchantInventory.ColorlessCardEntries)
            entry.Populate();

        // 3. 刷新遗物
        var blacklist = merchantInventory.RelicEntries
            .Select(e => e.Model?.CanonicalInstance)
            .OfType<RelicModel>()
            .ToHashSet();

        var fillRelic = HarmonyLib.AccessTools.Method(
            typeof(MerchantRelicEntry),
            "FillSlot",
            new[] { typeof(RelicRarity), typeof(IEnumerable<RelicModel>) });
        if (fillRelic != null)
        {
            foreach (var entry in merchantInventory.RelicEntries)
            {
                var rarity = RelicFactory.RollRarity(player);
                fillRelic.Invoke(entry, new object[] { rarity, blacklist });
            }
        }

        // 4. 刷新药水
        var fillPotion = HarmonyLib.AccessTools.Method(
            typeof(MerchantPotionEntry),
            "FillSlot",
            new[] { typeof(IEnumerable<PotionModel>) });
        if (fillPotion != null)
        {
            foreach (var entry in merchantInventory.PotionEntries)
                fillPotion.Invoke(entry, new object[] { System.Array.Empty<PotionModel>() });
        }

        // 5. 通知所有条目更新 UI
        foreach (var entry in merchantInventory.AllEntries)
            entry.OnMerchantInventoryUpdated();

        Flash();
    }
}
