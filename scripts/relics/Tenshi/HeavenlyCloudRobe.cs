using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 天云羽衣：在每场战斗开始时，获得3敏捷。
/// 当你打出攻击牌后，该效果转化为3力量；
/// 当你打出技能牌后，该效果转化为3敏捷。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HeavenlyCloudRobe : TouhouAncientRelics
{
    private enum Mode
    {
        Dexterity,
        Strength
    }

    private Mode _currentMode;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BuffAmount", 3)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        // 初始为敏捷形态
        _currentMode = Mode.Dexterity;
        Flash();
        await PowerCmd.Apply<HeavenlyCloudRobeDexterityPower>(
            new ThrowingPlayerChoiceContext(),
            base.Owner.Creature,
            base.DynamicVars["BuffAmount"].BaseValue,
            base.Owner.Creature,
            null);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;

        if (cardPlay.Card.Type == CardType.Attack && _currentMode == Mode.Dexterity)
        {
            // 切换到力量形态
            Flash();
            var existing = base.Owner.Creature.GetPower<HeavenlyCloudRobeDexterityPower>();
            if (existing != null)
            {
                await PowerCmd.Remove(existing);
            }

            await PowerCmd.Apply<HeavenlyCloudRobeStrengthPower>(
                context,
                base.Owner.Creature,
                base.DynamicVars["BuffAmount"].BaseValue,
                base.Owner.Creature,
                null);
            _currentMode = Mode.Strength;
        }
        else if (cardPlay.Card.Type == CardType.Skill && _currentMode == Mode.Strength)
        {
            // 切换到敏捷形态
            Flash();
            var existing = base.Owner.Creature.GetPower<HeavenlyCloudRobeStrengthPower>();
            if (existing != null)
            {
                await PowerCmd.Remove(existing);
            }

            await PowerCmd.Apply<HeavenlyCloudRobeDexterityPower>(
                context,
                base.Owner.Creature,
                base.DynamicVars["BuffAmount"].BaseValue,
                base.Owner.Creature,
                null);
            _currentMode = Mode.Dexterity;
        }
    }
}