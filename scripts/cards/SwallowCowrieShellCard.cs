using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 燕之子安贝：蓬莱谜题（类型 2）。0 费。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class SwallowCowrieShellCard : HouraiPuzzleCard
{
    private const int energyCost = 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("DrawPileLimit", 1m)];

    public SwallowCowrieShellCard() : base(energyCost)
    {
    }

    protected override PuzzleCardName PuzzleType => PuzzleCardName.燕之子安贝;

    /// <summary>
    /// 只有抽牌堆的牌数量不大于{DrawPileLimit}时才能打出。
    /// </summary>
    protected override bool IsPlayable => PileType.Draw.GetPile(base.Owner).Cards.Count <= base.DynamicVars["DrawPileLimit"].IntValue;
}