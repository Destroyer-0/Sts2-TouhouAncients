using System.Drawing;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 地狱猫车：有敌人死亡时，恢复4点生命。每当你洗牌时，若恢复生命的数值不为0，抽一张牌并使该效果-2。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HellOrin : TouhouAncientRelics
{
    private const int DefaultHeal = 5;

    private int _healAmount = DefaultHeal;

    private int HealAmount
    {
        get => _healAmount;
        set
        {
            _healAmount = value;
            if (_healAmount <= 0)
            {
                base.Status = RelicStatus.Disabled;
            }
            else
            {
                base.Status = RelicStatus.Normal;
            }
            InvokeDisplayAmountChanged();
        }
    }

    public override bool ShowCounter => DisplayAmount > -1;

    public override int DisplayAmount =>
        !CombatManager.Instance.IsInProgress ? -1 : IsCanonical ? -1 : _healAmount;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("HealAmount", DefaultHeal)];

    public override Task BeforeCombatStart()
    {
        HealAmount = DefaultHeal;
        return Task.CompletedTask;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!creature.IsEnemy) return; // 只针对敌人死亡
        if (creature.CombatState == null) return;

        Flash();
        await CreatureCmd.Heal(base.Owner.Creature, _healAmount);
        await CardPileCmd.Draw(choiceContext, 1, Owner, fromHandDraw: true);
        HealAmount -= 2;
    }

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player shuffler)
    {
        if (shuffler != base.Owner) return;
        if (_healAmount <= 0) return;

        Flash();
        await CardPileCmd.Draw(choiceContext, 1, shuffler, fromHandDraw: true);
        HealAmount -= 2;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        HealAmount = DefaultHeal;
        return base.AfterCombatEnd(room);
    }
}
