using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 地狱猫车：有敌人死亡时，恢复4点生命。每当你洗牌时，若恢复生命的数值不为0，抽一张牌并使该效果-2。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class HellOrin : TouhouAncientRelics
{
    private const decimal DefaultHeal = 4m;

    private decimal _healAmount = DefaultHeal;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HealAmount", DefaultHeal)];

    public override Task BeforeCombatStart()
    {
        _healAmount = DefaultHeal;
        return Task.CompletedTask;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature.Player != null) return; // 只针对敌人死亡
        if (creature.CombatState == null) return;

        Flash();
        await CreatureCmd.Heal(base.Owner.Creature, _healAmount);
    }

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler != base.Owner) return;
        if (_healAmount <= 0) return;

        Flash();
        await CardPileCmd.Draw(choiceContext, 1, shuffler, fromHandDraw: true);
        _healAmount -= 2;
    }
}
