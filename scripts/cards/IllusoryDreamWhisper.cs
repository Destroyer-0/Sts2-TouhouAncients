using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 幻梦呢喃：将3（4）张随机攻击牌或技能牌加入抽牌堆，这些牌拥有消耗与虚无。
/// 休息时可变化为深梦无觉或噩梦无终。
/// </summary>
[Pool(typeof(EventCardPool))]
public class IllusoryDreamWhisper : DreamCycleCard
{
    private const int energyCost = 2;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override string? Author => "半节";
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<DeepDreamSlumber>(),
        HoverTipFactory.FromCard<EndlessNightmare>()
    ];

    public IllusoryDreamWhisper() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await AddRandomDreamCards(PileType.Draw, DynamicVars.Cards.IntValue, CardType.Attack, CardType.Skill);
    }
}
