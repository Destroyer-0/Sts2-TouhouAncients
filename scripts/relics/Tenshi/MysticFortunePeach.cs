using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 奇运桃子：每当你打出攻击牌、技能牌、能力牌各一张时，清除负面效果并恢复1能量。
/// 可多次触发。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class MysticFortunePeach : TouhouAncientRelics
{
    private int _attacksPlayedThisTurn;
    private int _skillsPlayedThisTurn;
    private int _powersPlayedThisTurn;

    private int AttacksPlayedThisTurn
    {
        get => _attacksPlayedThisTurn;
        set { AssertMutable(); _attacksPlayedThisTurn = value; }
    }

    private int SkillsPlayedThisTurn
    {
        get => _skillsPlayedThisTurn;
        set { AssertMutable(); _skillsPlayedThisTurn = value; }
    }

    private int PowersPlayedThisTurn
    {
        get => _powersPlayedThisTurn;
        set { AssertMutable(); _powersPlayedThisTurn = value; }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner || !CombatManager.Instance.IsInProgress)
            return;

        if (cardPlay.Card.Type == CardType.Attack) AttacksPlayedThisTurn++;
        else if (cardPlay.Card.Type == CardType.Skill) SkillsPlayedThisTurn++;
        else if (cardPlay.Card.Type == CardType.Power) PowersPlayedThisTurn++;

        // 每次集齐攻击+技能+能力各一张时触发，可多次触发
        if (AttacksPlayedThisTurn > 0 && SkillsPlayedThisTurn > 0 && PowersPlayedThisTurn > 0)
        {
            AttacksPlayedThisTurn=0;
            SkillsPlayedThisTurn=0;
            PowersPlayedThisTurn=0;

            Flash();
            // 清除所有负面效果
            var creature = base.Owner.Creature;
            var debuffs = creature.Powers.Where(p => p.Type == PowerType.Debuff).ToList();
            foreach (var debuff in debuffs)
            {
                await PowerCmd.Remove(debuff);
            }
            // 恢复1能量
            await PlayerCmd.GainEnergy(1, base.Owner);
        }
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        AttacksPlayedThisTurn = 0;
        SkillsPlayedThisTurn = 0;
        PowersPlayedThisTurn = 0;
        return Task.CompletedTask;
    }
}
