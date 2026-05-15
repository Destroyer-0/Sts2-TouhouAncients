using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天罚石柱：使用技能牌时插下要石，使用攻击牌时拔出要石，对所有敌人造成伤害，并使要石本场战斗伤害提高。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class KeystoneFloatingCannon : TouhouAncientRelics
{
    private bool _keystoneInserted;
    private decimal _extraDamage;

    public override bool ShowCounter => _keystoneInserted;
    public override int DisplayAmount => _keystoneInserted ? 1 : 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Unpowered),
        new DynamicVar("ExtraDamage", 1)
    ];

    public override async Task BeforeCombatStart()
    {
        var creature = base.Owner.Creature;
        await PowerCmd.Apply<KeystonePower>(creature, base.DynamicVars.Damage.BaseValue, creature, null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;
        if (base.Owner.Creature.CombatState == null) return;

        if (cardPlay.Card.Type == CardType.Skill)
        {
            // 使用技能牌 → 插下要石
            _keystoneInserted = true;
            Flash();
            base.Status = RelicStatus.Active;
        }
        else if (cardPlay.Card.Type == CardType.Attack && _keystoneInserted)
        {
            // 使用攻击牌且要石已插下 → 拔出要石，造成伤害
            _keystoneInserted = false;
//
            var buffs = Owner.Creature.Powers.Where(p => p is KeystonePower).ToList();
            if(buffs.Count<=0)return;
            var keystonePower = buffs[0];
            var damage = keystonePower.Amount;
            
            var enemies = base.Owner.Creature.CombatState.GetOpponentsOf(base.Owner.Creature)
                .Where(c => c.IsAlive)
                .ToList();

            foreach (var enemy in enemies)
            {
                await CreatureCmd.Damage(
                    new BlockingPlayerChoiceContext(),
                    enemy,
                    damage,
                    ValueProp.Unpowered,
                    base.Owner.Creature,
                    null);
            }
            await PowerCmd.ModifyAmount(keystonePower, base.DynamicVars["ExtraDamage"].BaseValue, Owner.Creature, null);
            Flash();
            base.Status = RelicStatus.Normal;
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _keystoneInserted = false;
        _extraDamage = 0m;
        return Task.CompletedTask;
    }

}
