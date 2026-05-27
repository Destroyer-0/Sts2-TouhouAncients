using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 开海的奇迹：3费攻击（升级后2费）。
/// 对所有敌人造成等同于其当前生命值一半的伤害，该伤害不受易伤与力量影响。
/// </summary>
[Pool(typeof(EventCardPool))]
public class SeaOpeningMiracle : TouhouAncientCards
{
    private const int energyCost = 2;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    public SeaOpeningMiracle() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;
        var enemies = base.CombatState.HittableEnemies.ToList();

        foreach (var enemy in enemies)
        {
            if (CombatManager.Instance.IsOverOrEnding) break;

            var halfHp = Math.Floor(enemy.CurrentHp / 2m);
            await CreatureCmd.Damage(choiceContext, [enemy], halfHp, ValueProp.Unpowered, creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}