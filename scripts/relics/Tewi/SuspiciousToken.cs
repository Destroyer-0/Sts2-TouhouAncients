using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 可疑信物：在每个商店，你可以免费刷新一次商人出售的物品。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class SuspiciousToken : TouhouAncientRelics
{
    public bool TouhouAncients_FreeRefreshUsed { get; set; }

    /// <summary>
    /// 当前商店中是否还有免费刷新可用。
    /// </summary>
    public bool CanRefresh => !TouhouAncients_FreeRefreshUsed;

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        if (room is MerchantRoom)
        {
            TouhouAncients_FreeRefreshUsed = false;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 执行免费刷新。返回 true 表示刷新成功，false 表示无法刷新。
    /// </summary>
    public async Task<bool> TryRefresh(NMerchantInventory uiInventory)
    {
        if (TouhouAncients_FreeRefreshUsed) return false;

        var merchantInventory = uiInventory.Inventory;
        if (merchantInventory == null) return false;

        var player = base.Owner;
        if (player == null) return false;

        TouhouAncients_FreeRefreshUsed = true;

        await RefreshInventory(merchantInventory, uiInventory, player);
        return true;
    }

    private async Task RefreshInventory(
        MerchantInventory merchantInventory,
        NMerchantInventory uiInventory,
        Player player)
    {
        // 1. 刷新角色卡牌（数据 + UI）
        var charCardContainer = uiInventory.GetNodeOrNull<Control>("%CharacterCards");
        if (charCardContainer != null)
        {
            var charCardSlots = charCardContainer.GetChildren().OfType<NMerchantCard>().ToList();
            for (int i = 0; i < merchantInventory.CharacterCardEntries.Count && i < charCardSlots.Count; i++)
            {
                var entry = merchantInventory.CharacterCardEntries[i];
                entry.Populate();
                charCardSlots[i].FillSlot(entry);
            }
        }

        // 2. 刷新无色卡牌（数据 + UI）
        var colorlessCardContainer = uiInventory.GetNodeOrNull<Control>("%ColorlessCards");
        if (colorlessCardContainer != null)
        {
            var colorlessCardSlots = colorlessCardContainer.GetChildren().OfType<NMerchantCard>().ToList();
            for (int i = 0; i < merchantInventory.ColorlessCardEntries.Count && i < colorlessCardSlots.Count; i++)
            {
                var entry = merchantInventory.ColorlessCardEntries[i];
                entry.Populate();
                colorlessCardSlots[i].FillSlot(entry);
            }
        }

        // 3. 刷新遗物
        var relicContainer = uiInventory.GetNodeOrNull<Control>("%Relics");
        if (relicContainer != null)
        {
            var relicSlots = relicContainer.GetChildren().OfType<NMerchantRelic>().ToList();
            var blacklist = merchantInventory.RelicEntries
                .Select(e => e.Model?.CanonicalInstance)
                .OfType<RelicModel>()
                .ToHashSet();

            var fillRelicMethod = HarmonyLib.AccessTools.Method(
                typeof(MerchantRelicEntry),
                "FillSlot",
                new[] { typeof(RelicRarity), typeof(IEnumerable<RelicModel>) });
            if (fillRelicMethod == null) return;

            for (int i = 0; i < merchantInventory.RelicEntries.Count; i++)
            {
                var entry = merchantInventory.RelicEntries[i];
                var rarity = RelicFactory.RollRarity(player);
                fillRelicMethod.Invoke(entry, new object[] { rarity, blacklist });

                if (i < relicSlots.Count)
                {
                    relicSlots[i].FillSlot(entry);
                }
            }
        }

        // 4. 刷新药水
        var potionContainer = uiInventory.GetNodeOrNull<Control>("%Potions");
        if (potionContainer != null)
        {
            var potionSlots = potionContainer.GetChildren().OfType<NMerchantPotion>().ToList();
            var fillPotionMethod = HarmonyLib.AccessTools.Method(
                typeof(MerchantPotionEntry),
                "FillSlot",
                new[] { typeof(IEnumerable<PotionModel>) });
            if (fillPotionMethod == null) return;

            for (int i = 0; i < merchantInventory.PotionEntries.Count; i++)
            {
                var entry = merchantInventory.PotionEntries[i];
                fillPotionMethod.Invoke(entry, new object[] { System.Array.Empty<PotionModel>() });

                if (i < potionSlots.Count)
                {
                    potionSlots[i].FillSlot(entry);
                }
            }
        }

        // 刷新计数显示
        Flash();
    }
}
