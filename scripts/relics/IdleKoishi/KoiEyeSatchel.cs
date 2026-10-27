using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Hooks;
using TouhouAncients.Scripts.potions;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 恋之瞳挎包：拾起时获得 1 个药水栏位与 2 份特制造型巧克力；此后每获得 14 气氛再获得 1 份。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KoiEyeSatchel : TouhouAncientRelics, IAtmosphereGainedListener
{
    private const int PotionSlots = 1;
    private const int Chocolates = 2;
    private const int AtmospherePerChocolate = 14;
    private const int StatAmount = 1;

    private int _progress;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("PotionSlots", PotionSlots),
        new DynamicVar("Chocolates", Chocolates),
        new DynamicVar("AtmospherePerChocolate", AtmospherePerChocolate),
        new DynamicVar("StatAmount", StatAmount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPotion<IdolChocolatePotion>()];

    public override Task BeforeCombatStart()
    {
        _progress = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return;

        Flash();
        await PlayerCmd.GainMaxPotionCount(PotionSlots, player);
        for (var i = 0; i < Chocolates; i++)
        {
            await GiveChocolate();
        }
    }

    public async Task AfterAtmosphereGained(PlayerChoiceContext choiceContext, int amount)
    {
        _progress += amount;
        while (_progress >= AtmospherePerChocolate)
        {
            _progress -= AtmospherePerChocolate;
            await GiveChocolate();
        }
    }

    private async Task GiveChocolate()
    {
        var player = base.Owner;
        if (player == null || !player.HasOpenPotionSlots) return;

        Flash();
        await PotionCmd.TryToProcure(ModelDb.Potion<IdolChocolatePotion>().ToMutable(), player);
    }
}
