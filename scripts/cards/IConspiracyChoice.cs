using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TouhouAncients.Scripts.cards;

/// <summary>蜃影阴谋选出的「阴谋论」卡牌，负责结算选中后的效果。</summary>
public interface IConspiracyChoice
{
    Task OnChosen(PlayerChoiceContext choiceContext, Creature beiko);
}
