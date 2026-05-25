using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 燕之子安贝：拾起时，获得15最大生命；进入新的房间时，若你的生命值少于40%则回复到40%。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class TsubameNoKoyasugai : TouhouAncientRelics
{
    private const decimal HealThresholdPercent = 0.4m;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("MaxHpGain", 16m),
    ];

    public override async Task AfterObtained()
    {
        await CreatureCmd.GainMaxHp(base.Owner.Creature, base.DynamicVars["MaxHpGain"].BaseValue);
    }

    /// <summary>
    /// 进入新房间时，若生命值少于40%则回复到40%。
    /// </summary>
    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        var creature = base.Owner?.Creature;
        if (creature == null) return;

        var threshold = (decimal)creature.MaxHp * HealThresholdPercent;
        if (creature.CurrentHp < threshold)
        {
            Flash();
            var healAmount = threshold - creature.CurrentHp;
            await CreatureCmd.Heal(creature, healAmount);
        }
    }
}
