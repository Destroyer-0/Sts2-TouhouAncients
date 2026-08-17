using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 蓬莱的玉枝：蓬莱谜题（类型 4）。0 费。
/// 只有抽牌堆的牌数量不大于 1 时才能打出；
/// 打出后保留你的手牌、结束你的回合，并完成对应谜题、将此牌移出战斗。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class HouraiNoTamaeCard : HouraiPuzzleCard
{
    private const int energyCost = 0;

    public HouraiNoTamaeCard() : base(energyCost)
    {
    }

    protected override int PuzzleType => 4;

    /// <summary>
    /// 只有抽牌堆的牌数量不大于 1 时才能打出。
    /// </summary>
    protected override bool IsPlayable => PileType.Draw.GetPile(base.Owner).Cards.Count <= 1;
}
