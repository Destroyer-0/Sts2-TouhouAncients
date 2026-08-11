using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 丝带蝴蝶结：
/// 拾起时，将一张遗恨加入牌组。从你的牌组选择 SelectCount 张攻击牌或技能牌，为它们附魔"誓约"。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class RibbonBow : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("EnchantmentName", ModelDb.Enchantment<Oath>().Title.GetFormattedText()),
        new DynamicVar("SelectCount", 2)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Oath>()
            .Concat(HoverTipFactory.FromCardWithCardHoverTips<YuanHen>());

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 拾起时，将一张遗恨加入牌组
        CardModel grudge = player.RunState.CreateCard<YuanHen>(player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(grudge, PileType.Deck), 2f);

        // 从牌组中选择 SelectCount 张攻击牌或技能牌附魔誓约
        var selectCount = DynamicVars["SelectCount"].IntValue;
        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, selectCount);
        Oath recollection = ModelDb.Enchantment<Oath>();
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(base.Owner, recollection, selectCount, prefs))
        {
            CardCmd.Enchant(recollection.ToMutable(), item, 1m);
            CardCmd.Preview(item);
        }
    }
}
