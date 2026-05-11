using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 梦想封印：1费，对所有敌人造成2(升级后3)点伤害6次。
/// 若其意图为攻击，给予1层易伤并使其在本回合失去8(升级后10)点力量。
/// 意图检测参考：GoForTheEyes。
/// </summary>
[Pool(typeof(EventCardPool))]
public class DreamSeal : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    private const int HitCount = 6;
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (base.CombatState == null)
            {
                return false;
            }
            return base.CombatState.HittableEnemies.Any((Creature e) => e.Monster?.IntendsToAttack ?? false);
        }
    }
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(2m, ValueProp.Move),
        new DynamicVar("StrengthLoss", 6m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<StrengthPower>(),
        base.EnergyHoverTip
    ];
    
    public DreamSeal() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;

        // 1. 6次伤害（对所有敌人）
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .WithHitCount(HitCount)
            .FromCard(this)
            .TargetingAllOpponents(base.CombatState)
            .Execute(choiceContext);

        // 2. 对每个意图为攻击的敌人附加易伤+临时减力量
        decimal strengthLoss = base.DynamicVars["StrengthLoss"].BaseValue;
        var enemies = base.CombatState.HittableEnemies.ToList();

        foreach (var enemy in enemies)
        {
            if (enemy.Monster?.IntendsToAttack ?? false)
            {
                await PowerCmd.Apply<VulnerablePower>(enemy, 1m, creature, this);
                await PowerCmd.Apply<DreamSealStrengthDownPower>(enemy, strengthLoss, creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(1m);
        base.DynamicVars["StrengthLoss"].UpgradeValueBy(2m);
    }
}
