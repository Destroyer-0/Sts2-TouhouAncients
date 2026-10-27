using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 演奏方式抉择卡基类：只在应援棒的选卡界面出现，选中后由 <see cref="IPerformanceChoice"/> 结算。
/// </summary>
public abstract class PerformanceChoiceCard : TouhouAncientCards, IPerformanceChoice
{
    protected PerformanceChoiceCard() : base(-1, CardType.Status, CardRarity.Status, TargetType.None,
        shouldShowInCardLibrary: false)
    {
    }

    public override bool CanBeGeneratedInCombat => false;

    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    // 无专属立绘时退回原版占位图，避免选卡界面加载失败的路径
    public override string PortraitPath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/cards/{GetType().Name}.png", MissingPortraitPath);

    public abstract Task OnChosen(PlayerChoiceContext choiceContext);
}
