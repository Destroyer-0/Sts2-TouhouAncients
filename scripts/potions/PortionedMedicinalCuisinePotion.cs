using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.potions;

/// <summary>
/// 分装药膳：回复你最大生命值的20%。战斗结束时它若仍在药水栏位中，额外回复2点生命值。
/// </summary>
[Pool(typeof(EventPotionPool))]
public sealed class PortionedMedicinalCuisinePotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Event;

    public override PotionUsage Usage => PotionUsage.AnyTime;

    public override TargetType TargetType => TargetType.Self;

    /// <summary>回复类药水不进战斗内随机生成，同原版果汁/再生药水。</summary>
    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HealPercent", 20m),
        new DynamicVar("CombatEndHeal", 2m)
    ];

    public override string? CustomPackedImagePath => $"res://images/potion/{GetType().Name}.png";
    public override string? CustomPackedOutlinePath => $"res://images/potion/{GetType().Name}_outline.png";

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        var creature = target ?? base.Owner.Creature;
        await CreatureCmd.Heal(creature, creature.MaxHp * base.DynamicVars["HealPercent"].BaseValue / 100m);
    }

    /// <summary>
    /// 药水能收到 Hook 就说明它此刻在药水栏位里（喝掉/丢弃后 HasBeenRemovedFromState 置位，不再回调），
    /// 所以「按瓶累计」天然成立：两瓶会回调两次。
    /// </summary>
    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (base.Owner.Creature.IsDead) return;
        await CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars["CombatEndHeal"].IntValue);
    }
}
