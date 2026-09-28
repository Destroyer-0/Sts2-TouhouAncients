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
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 无穷无尽的愤怒：在你的回合开始时，将一张带有随机附魔的群情激愤（多人）/愤怒（单人）加入你的手牌。
/// 群情激愤（Outrage）是原版多人限定牌，单人以愤怒（Anger）代替。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class Wuqiongwujindefennu : TouhouAncientRelics
{
    /// <summary>事件描述里要写出的具体牌名（多人：群情激愤；单人：愤怒）。</summary>
    private const string AngryCardKey = "AngryCard";

    /// <summary>
    /// 默认空串：事件选项生成阶段由 <see cref="SetupForPlayer"/> 填上当前人数对应的牌名，
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
                if (e.IsMock) return false;
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
            // IsCanonical 不够：遗物图鉴 / 控制台 / BaseLib 枚举 AllPossibleOptions 时，选项是在事件
            // 绑定玩家之前用 ModelDb.Relic<T>().ToMutable() 造出来的 mutable 克隆，
            // 此时 IsCanonical == false 而 Owner == null，直接读 Owner 会 NRE。
            // 注意 RelicModel._owner 字段其实是 Player?（字段带 NullableAttribute，属性却没标可空），
            // 所以用可空局部变量承接：保留运行时保护，同时不会被 IDE 判成"恒为 false"。
            // Owner 的 getter 会 AssertMutable()，必须先判 IsCanonical 再读 Owner。
            if (!IsCanonical)
            {
                Player? owner = Owner;
                if (owner != null)
                {
                    return owner.RunState.Players.Count > 1
                        ? HoverTipFactory.FromCardWithCardHoverTips<Outrage>()
                        : HoverTipFactory.FromCardWithCardHoverTips<Anger>();
                }
            }

            // canonical / 尚未绑定玩家：显示两种可能出现的牌。
            return
            [
                .. HoverTipFactory.FromCardWithCardHoverTips<Outrage>(),
                .. HoverTipFactory.FromCardWithCardHoverTips<Anger>(),
            ];
        }
    }

    public void SetupForPlayer(Player player)
    {
        CardModel card = player.RunState.Players.Count > 1
            ? ModelDb.Card<Outrage>()
            : ModelDb.Card<Anger>();

        ((StringVar)base.DynamicVars[AngryCardKey]).StringValue = card.Title;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;

        var combatState = player.Creature.CombatState;
        if (combatState == null) return;

        Flash();

        // 多人为群情激愤（原版多人限定牌），单人为愤怒。
        CardModel canonical = combatState.Players.Count > 1
            ? ModelDb.Card<Outrage>()
            : ModelDb.Card<Anger>();

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
