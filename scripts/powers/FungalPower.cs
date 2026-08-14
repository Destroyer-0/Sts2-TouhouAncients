using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace TouhouAncients.Scripts.powers;

/// <summary>
/// 菌类：奇幻蘑菇的孢子喷发能力。持有者死亡时，向每个玩家的弃牌堆随机位置加入孢子心灵。
/// </summary>
public class FungalPower : TouhouAncientPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;


    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<SporeMind>()];

    /// <summary>
    /// 死亡时：向每个玩家的弃牌堆随机位置加入孢子心灵。
    /// </summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != base.Owner) return;
        
        // 每个玩家的弃牌堆随机位置加入孢子心灵
        foreach (Player player in base.Owner.CombatState!.Players)
        {
            await CardPileCmd.AddToCombatAndPreview<SporeMind>(player.Creature, PileType.Discard, Amount, player, CardPilePosition.Random);
        }
    }
}
