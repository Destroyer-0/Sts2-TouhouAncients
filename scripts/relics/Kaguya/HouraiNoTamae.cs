using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 蓬莱的玉枝：每场战斗开始时，为你的随机7张牌附魔"梦色"。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HouraiNoTamae : TouhouAncientRelics
{
    private const int EnchantCount = 7;

    public override bool HasUponPickupEffect => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EnchantCount", EnchantCount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Yumeiro>();

    /// <summary>
    /// 战斗开始时，为随机7张牌附魔"梦色"。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        var player = base.Owner;

        var deck = PileType.Deck.GetPile(player);
        if (deck.IsEmpty) return;

        var model = ModelDb.Enchantment<Yumeiro>();
        
        // 找出可附魔"梦色"的牌
        var enchantable = deck.Cards
            .Where(c => model.CanEnchant(c))
            .ToList();

        if (enchantable.Count == 0) return;

        var rng = player.RunState.Rng.CombatCardSelection;
        var toEnchant = enchantable.UnstableShuffle(rng).Take(Math.Min(enchantable.Count, EnchantCount));

        foreach (var card in toEnchant)
        {
            CardCmd.Enchant<Yumeiro>(card,1);
        }
    }
}
