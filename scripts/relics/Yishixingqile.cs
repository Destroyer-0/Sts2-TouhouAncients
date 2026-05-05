using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class Yishixingqile : TouhouAncientRelics
{
    private bool _hasTriggered;

    public override bool IsUsedUp => _hasTriggered;

    [SavedProperty]
    public bool HasTriggered
    {
        get => _hasTriggered;
        set
        {
            AssertMutable();
            _hasTriggered = value;
            if (IsUsedUp)
            {
                base.Status = RelicStatus.Disabled;
            }
        }
    }

    public override Task AfterItemPurchased(Player player, MerchantEntry itemPurchased, int goldSpent)
    {
        if (player != base.Owner) return Task.CompletedTask;
        if (HasTriggered) return Task.CompletedTask;
        if (goldSpent <= 0) return Task.CompletedTask;
        if (itemPurchased is not MerchantRelicEntry) return Task.CompletedTask;

        Flash();
        HasTriggered = true;
        return Task.CompletedTask;
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal cost)
    {
        if (player != base.Owner) return cost;
        if (!HasTriggered) return cost;

        return 0m;
    }
}