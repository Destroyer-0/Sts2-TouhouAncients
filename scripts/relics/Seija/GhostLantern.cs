using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 亡灵提灯：从15张无色牌中选择任意张加入牌组，这些牌拥有附魔：付丧之力。
/// 回合结束时，将弃牌堆中所有带付丧之力的牌移回手牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class GhostLantern : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("EnchantmentName", ModelDb.Enchantment<Tsukumogami>().Title.GetFormattedText()),
        new DynamicVar("Amount", 15)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromEnchantment<Tsukumogami>();

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 拾起时：选15张无色牌中的任意张加入牌组，附魔付丧之力。
    /// </summary>
    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return;

        // 获取无色牌池，生成15张
        var colorlessPool = ModelDb.CardPool<ColorlessCardPool>();
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { colorlessPool },
                _ => true)
            .WithFlags(CardCreationFlags.NoRarityModification
                       | CardCreationFlags.NoCardPoolModifications);

        var creationResults = CardFactory.CreateForReward(player, (int)base.DynamicVars["Amount"].BaseValue, options).ToList();

        foreach (var option in creationResults)
        {
            var card = option.Card;

            if (card.Enchantment != null)
            {
                CardCmd.ClearEnchantment(card);
            }

            option.ModifyCard(EnchantCard(card), this);
        }
        // 网格多选
        var selected = (await CardSelectCmd.FromSimpleGridForRewards(
                new BlockingPlayerChoiceContext(),
                creationResults,
                player,
                new CardSelectorPrefs(base.SelectionScreenPrompt, 0, 15)))
            .ToList();

        if (selected.Count <= 0) return;

        // 附魔付丧之力并加入牌组
        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Deck);
        }
    }


    private CardModel EnchantCard(CardModel card)
    {
        var enchanted = base.Owner.RunState.CloneCard(card);
        CardCmd.Enchant<Tsukumogami>(enchanted, 0);
        return enchanted;
    }
}