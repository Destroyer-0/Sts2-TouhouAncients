using System.Threading.Tasks;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TouhouAncients.Scripts.cards;

/// <summary>演奏方式抉择卡：在应援棒的选卡界面里被选中后立即结算。</summary>
public interface IPerformanceChoice
{
    Task OnChosen(PlayerChoiceContext choiceContext);
}
