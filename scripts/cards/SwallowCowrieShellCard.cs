using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 燕之子安贝：蓬莱谜题（类型 2）。0 费。
/// 目前暂无额外效果；打出后完成对应谜题并将此牌移出战斗。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class SwallowCowrieShellCard : HouraiPuzzleCard
{
    private const int energyCost = 0;

    public SwallowCowrieShellCard() : base(energyCost)
    {
    }

    protected override int PuzzleType => 2;
}
