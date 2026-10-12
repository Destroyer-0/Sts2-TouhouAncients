using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Enchantments.Mocks;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 无穷无尽的愤怒：在你的回合开始时，将一张带有随机附魔的愤怒加入你的手牌。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class Wuqiongwujindefennu : TouhouAncientRelics
{
    /// <summary>事件描述里要写出的具体牌名。</summary>
    private const string AngryCardKey = "AngryCard";

    /// <summary>
    /// 默认空串：事件选项生成阶段由 <see cref="SetupForPlayer"/> 填上牌名，
    /// 填之前（图鉴、控制台枚举选项）描述里的 <c>{AngryCard.StringValue:cond:...}</c> 走兜底分支。
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar(AngryCardKey)
    ];

    /// <summary>
    /// 全部附魔的缓存。取法与附魔「梦色」一致：遍历所有 EnchantmentModel 子类再按 ModelId 取实例，
    /// 单个类型取不到就跳过（例如尚未注册或依赖其它模组的类型）。
    /// </summary>
    private static List<EnchantmentModel>? s_allEnchantments;

    private static List<EnchantmentModel> AllEnchantments
    {
        get
        {
            if (s_allEnchantments == null)
            {
                s_allEnchantments = new List<EnchantmentModel>();
                var enchantmentTypes = ModelDb.AllAbstractModelSubtypes
                    .Where(t => t != null && t.IsSubclassOf(typeof(EnchantmentModel)) && !t.IsAbstract);

                foreach (Type type in enchantmentTypes)
                {
                    try
                    {
                        ModelId id = ModelDb.GetId(type);
                        EnchantmentModel? enchantment = ModelDb.GetByIdOrNull<EnchantmentModel>(id);

                        if (enchantment != null)
                        {
                            s_allEnchantments.Add(enchantment);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                s_allEnchantments = s_allEnchantments
                    .OrderBy(e => e.Id.ToString(), StringComparer.Ordinal)
                    .ToList();
            }

            return s_allEnchantments;
        }
    }

    private IEnumerable<EnchantmentModel> GetCandidates(CardModel card)
    {
        return AllEnchantments
            .Where(e =>
            {
                if (e is DeprecatedEnchantment) return false;
                if (e is MockFreeEnchantment) return false;
                if (e is Goopy) return false;
                if (e is Bloodshed) return false;
                if (e is Yumeiro) return false;
                if (e is TouhouAncientEnchantmentModel { CanBeRandomSelected: false }) return false;
                return e.CanEnchant(card);
            });
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            return
            [
                .. HoverTipFactory.FromCardWithCardHoverTips<Anger>(),
            ];
        }
    }

    public void SetupForPlayer(Player player)
    {
        CardModel card = ModelDb.Card<Anger>();

        ((StringVar)base.DynamicVars[AngryCardKey]).StringValue = card.Title;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        Flash();

        // 固定为愤怒。
        CardModel canonical = ModelDb.Card<Anger>();

        var card = combatState.CreateCard(canonical, player);

        // 先附魔再加入手牌：此时牌还没有牌堆，CanEnchant 里基于牌堆的限制不会误判。
        var candidates = GetCandidates(card).ToList();
        if (candidates.Count > 0)
        {
            var randomEnchantment = candidates.UnstableShuffle(player.RunState.Rng.CombatCardGeneration).First();
            CardCmd.Enchant(randomEnchantment.ToMutable(), card,
                TouhouAncientEnchantmentModel.GetRandomEnchantAmount(randomEnchantment));
        }

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, creator: base.Owner);
    }
}
