using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class BloodFang : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("LoseHp", 0),
            new StringVar("EnchantmentName", ModelDb.Enchantment<Miracle>().Title.GetFormattedText())
        ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Bloodshed>();

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        Bloodshed bloodshed = ModelDb.Enchantment<Bloodshed>();
        var enchantSuccess = false;
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(base.Owner, bloodshed, 1, prefs))
        {
            CardCmd.Enchant(bloodshed.ToMutable(), item, 1m);
            CardCmd.Preview(item);
            enchantSuccess = true;
        }

        if (enchantSuccess)
        {
            // 1. 失去最大生命的一半（向下取整）
            var halfHp = Math.Floor(player.Creature.MaxHp * 0.5m);
            base.DynamicVars["LoseHp"].BaseValue = halfHp;
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), player.Creature, halfHp, isFromCard: false);
        }
    }
}