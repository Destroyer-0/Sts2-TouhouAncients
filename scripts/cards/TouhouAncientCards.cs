using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace TouhouAncients.Scripts.cards;

public abstract class TouhouAncientCards : CustomCardModel
{
    public override string PortraitPath => $"res://images/cards/{GetType().Name}.png";

    // /// <summary>
    // /// ModelDb 反射实例化所需的无参构造函数。
    // /// 实际使用时应调用带参构造函数。
    // /// </summary>
    // protected TouhouAncientCards() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.None, true)
    // {
    // }

    public TouhouAncientCards(int energyCost, CardType type, CardRarity rarity, TargetType targetType,
        bool shouldShowInCardLibrary) : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }
}