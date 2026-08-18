using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 蓬莱的玉枝：蓬莱谜题（类型 4）。3 费。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class HouraiNoTamaeCard : HouraiPuzzleCard
{
    private const int energyCost = 3;

    public HouraiNoTamaeCard() : base(energyCost)
    {
    }

    protected override PuzzleCardName PuzzleType => PuzzleCardName.蓬莱的玉枝;
}
