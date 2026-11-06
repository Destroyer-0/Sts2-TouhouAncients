using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 幻想乡缘起：从 3 张升级过的东方先古之民卡牌中选一张加入手牌，本回合免费打出。
/// </summary>
[Pool(typeof(EventCardPool))]
public class GensokyoChronicle : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public GensokyoChronicle() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 自身是 Skill 且属 TouhouAncientCards，不排掉会把自己塞进自己的候选池
    public override bool CanBeGeneratedByModifiers => true;
    public override bool CanBeGeneratedInCombat => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    public override string? Author => "カズ";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 与超ZUN啤酒同池：TouhouAncientCards 里可战斗生成的攻/技/能力牌
        List<CardModel> pool = ModelDb.AllCards
            .Where(c => c is TouhouAncientCards && c.CanBeGeneratedInCombat)
            .Where(c => c.Type is CardType.Attack or CardType.Skill or CardType.Power)
            .Where(c => c is not GensokyoChronicle)
            .ToList();

        List<CardModel> options = TouhouAncientCmd.CreateCombatOptions(
            base.Owner, pool, base.DynamicVars.Cards.IntValue, IsUpgraded);

        await TouhouAncientCmd.ChooseCardsToPile(
            choiceContext, base.Owner, options, 1, () => base.SelectionScreenPrompt, PileType.Hand, freeThisTurn: true);
    }
}