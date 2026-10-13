using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 侦探小说手稿：回合结束时，若所有敌人的意图都不是攻击，则在下回合获得2点临时敏捷。
/// 玩家回合结束时敌人意图已是「即将执行」的那一个（§7.20），故此时判定即可。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DetectiveManuscript : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<DetectiveManuscriptDexterityPower>()];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Dexterity", 2m)];

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != base.Owner.Creature.Side) return;

        var combatState = base.Owner.Creature.CombatState;
        if (combatState == null) return;
        if (combatState.Enemies.Any(e => !e.IsDead && e.Monster?.IntendsToAttack == true)) return;

        Flash();
        await PowerCmd.Apply<DetectiveManuscriptDexterityPower>(choiceContext, base.Owner.Creature,
            base.DynamicVars["Dexterity"].BaseValue, base.Owner.Creature, null);
    }
}
