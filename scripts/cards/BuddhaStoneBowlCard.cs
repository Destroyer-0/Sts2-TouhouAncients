using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 佛御石之钵：蓬莱谜题（类型 3）。0 费。
/// 只有你的格挡大于 35 时才能打出；打出后完成对应谜题并将此牌移出战斗。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class BuddhaStoneBowlCard : HouraiPuzzleCard
{
    private const int energyCost = 0;

    private const int RequiredBlock = 35;

    public BuddhaStoneBowlCard() : base(energyCost)
    {
    }

    protected override PuzzleCardName PuzzleType => PuzzleCardName.佛御石之钵;

    /// <summary>
    /// 只有你的格挡大于 35 时才能打出。
    /// </summary>
    protected override bool IsPlayable => base.Owner != null && base.Owner.Creature.Block > RequiredBlock;
}
