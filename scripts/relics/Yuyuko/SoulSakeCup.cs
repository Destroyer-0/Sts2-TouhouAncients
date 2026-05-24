using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 幽魂酒盅：获得70最大生命，将生命值降低至20。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class SoulSakeCup : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MaxHpGain", 80m),
        new DynamicVar("SetHp", 40m)
    ];

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        var creature = player.Creature;
        await CreatureCmd.GainMaxHp(creature, base.DynamicVars["MaxHpGain"].BaseValue);

        // 将当前生命值降低至20
        var targetHp = (int)base.DynamicVars["SetHp"].BaseValue;
        var currentHp = (int)creature.CurrentHp;
        if (currentHp > targetHp)
        {
            var diff = currentHp - targetHp;
            // 对自身造成伤害以降低生命值
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                creature,
                diff,
                ValueProp.Unpowered | ValueProp.Unblockable,
                dealer: null,
                cardSource: null
            );
        }
    }
}
