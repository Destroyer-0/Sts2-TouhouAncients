using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 商店中的"免费刷新"入口项，价格为0，购后触发 SuspiciousToken 的刷新逻辑。
/// </summary>
public sealed class MerchantRefreshEntry : MerchantEntry
{
    private readonly SuspiciousToken _relic;

    public SuspiciousToken Relic => _relic;

    /// <summary>是否已被使用过（同一商店内）。</summary>
    public bool Used => _used;
    private bool _used;

    /// <summary>
    /// 只有当玩家持有可疑信物且本商店还未使用过时，此项才可见。
    /// </summary>
    public override bool IsStocked =>
        base._player.RunState.CurrentRoom is MerchantRoom
        && _relic.CanRefresh
        && !_used;

    public MerchantRefreshEntry(Player player, SuspiciousToken relic) : base(player)
    {
        _relic = relic;
    }

    public override void CalcCost()
    {
        _cost = 0;
    }

    protected override async Task<(bool, int)> OnTryPurchase(MerchantInventory? inventory, bool ignoreCost)
    {
        if (inventory == null) return (false, 0);

        await _relic.DoRefresh(inventory);
        _used = true;
        return (true, 0);
    }

    protected override void ClearAfterPurchase()
    {
        _used = true;
    }

    protected override void RestockAfterPurchase(MerchantInventory? inventory)
    {
        // 一次性使用，不复充
    }
}
