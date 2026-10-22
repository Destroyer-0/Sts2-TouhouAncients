using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace TouhouAncients.Scripts.potions;

/// <summary>恶魔之狞笑：失去最大生命并对单体造成伤害（效果未实现）。</summary>
[Pool(typeof(EventPotionPool))]
public sealed class DemonsGrinPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Event;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyEnemy;

    // 事件专属，不参与战斗内随机生成
    public override bool CanBeGeneratedInCombat => false;

    public override string? CustomPackedImagePath => $"res://images/potion/{GetType().Name}.png";
    public override string? CustomPackedOutlinePath => $"res://images/potion/{GetType().Name}_outline.png";

    protected override Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        // TODO: 未实现——失去6最大生命、造成66伤害、斩杀追加奖励与永久增伤
        return Task.CompletedTask;
    }
}
