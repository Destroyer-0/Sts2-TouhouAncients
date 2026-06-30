using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.relics;

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


    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(0m, ValueProp.Move),
        new BlockVar(0m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromKeyword(CardKeyword.Exhaust), HoverTipFactory.Static(StaticHoverTip.Block)];

    public KillingAura() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>
    /// 累积伤害到这张牌上
    /// </summary>
    public void AddDamage(int amount)
    {
        amount += IsUpgraded ? 1 : 0;
        base.DynamicVars.Damage.UpgradeValueBy(amount);
    }

    /// <summary>
    /// 累积格挡到这张牌上
    /// </summary>
    public void AddBlock(int amount)
    {
        amount += IsUpgraded ? 1 : 0;
        base.DynamicVars.Block.UpgradeValueBy(amount);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target != null)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(cardPlay.Target,
                VfxColor.Purple));
        }
        // 获得累积的格挡
        await CreatureCmd.GainBlock(base.Owner.Creature, DynamicVars.Block.BaseValue, ValueProp.Unpowered, null);

        // 造成累积的伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }


    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatManager.Instance.IsOverOrEnding) return;
        if (cardPlay.Card.Owner != Owner) return;
        if (cardPlay.Card == this) return;
        if (!PileType.Hand.GetPile(Owner).Cards.Contains(this))
        {
            return;
        }

        if (cardPlay.Card.GainsBlock)
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(base.Owner.Creature,
                VfxColor.Purple));
            await CardCmd.Exhaust(
                choiceContext,
                cardPlay.Card,
                skipVisuals: PileType.Exhaust.GetPile(Owner).Cards.Contains(cardPlay.Card)
            );
            AddBlock(cardPlay.Card.DynamicVars.Block.IntValue);
            if (cardPlay.Card.DynamicVars.ContainsKey("Damage"))
            {
                AddDamage(cardPlay.Card.DynamicVars.Damage.IntValue);
            }

            return;
        }

        if (cardPlay.Card.DynamicVars.ContainsKey("Damage"))
        {
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NGroundFireVfx.Create(base.Owner.Creature,
                VfxColor.Purple));
            await CardCmd.Exhaust(
                choiceContext,
                cardPlay.Card,
                skipVisuals: PileType.Exhaust.GetPile(Owner).Cards.Contains(cardPlay.Card)
            );
            AddDamage(cardPlay.Card.DynamicVars.Damage.IntValue);
        }
    }
}