using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 火鼠的皮衣：蓬莱谜题（类型 1）。1 费。
/// 目前暂无额外效果；打出后完成对应谜题并将此牌移出战斗。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class HinezumiNoKawagoromoCard : HouraiPuzzleCard
{
    private const int energyCost = 1;

    public HinezumiNoKawagoromoCard() : base(energyCost)
    {
    }

    protected override int PuzzleType => 1;
}
