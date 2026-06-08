using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 杀戮灵气 (Killing Aura)
/// 造成0伤害，获得0格挡。
/// 这张牌在你手牌中时，你打出带有攻击/格挡的牌时，消耗之，将其对应数值添加到这张牌上。
/// （累积的伤害/格挡值通过 DynamicVars 存储）
/// </summary>
[Pool(typeof(EventCardPool))]
public class KillingAura : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    // 运行时累积的额外伤害和格挡
    private decimal TouhouAncients_StoredDamage { get; set; }
    private decimal TouhouAncients_StoredBlock { get; set; }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(0m, ValueProp.Move),
        new BlockVar(0m,ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.Static(StaticHoverTip.Block)];

    public KillingAura() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>
    /// 累积伤害到这张牌上
    /// </summary>
    public void AddDamage(decimal amount)
    {
        amount += IsUpgraded ? 1 : 0;
        TouhouAncients_StoredDamage += amount;
        base.DynamicVars.Damage.BaseValue = TouhouAncients_StoredDamage;
    }

    /// <summary>
    /// 累积格挡到这张牌上
    /// </summary>
    public void AddBlock(decimal amount)
    {
        amount += IsUpgraded ? 1 : 0;
        TouhouAncients_StoredBlock += amount;
        base.DynamicVars.Block.BaseValue = TouhouAncients_StoredBlock;
    }
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 造成累积的伤害
        if (TouhouAncients_StoredDamage > 0)
        {
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .Execute(choiceContext);
        }

        // 获得累积的格挡
        if (TouhouAncients_StoredBlock > 0)
        {
            await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);
        }
    }

    protected override void OnUpgrade()
    {
        // 杀戮灵气无法升级
    }
}
