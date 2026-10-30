using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 献给你的荆棘之歌：造成 14 + 当前气氛的伤害，获得 2 荆棘；嗨到爆下改为攻击全体。
/// 升级后进入嗨到爆时从牌堆回到手牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class ThornSongForYou : TouhouAncientCards, IHypedEnteredListener
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;

    private const string ThornsKey = "Thorns";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(14m, MegaCrit.Sts2.Core.ValueProps.ValueProp.Move),
        new DynamicVar(ThornsKey, 2m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.FromPower<ThornsPower>()];

    public ThornSongForYou() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (player?.Creature?.CombatState is not { } combatState) return;

        var damage = base.DynamicVars.Damage.BaseValue + KoishiPerformanceCmd.GetAtmosphere(player);

        if (KoishiPerformanceCmd.IsHyped(player))
        {
            await DamageCmd.Attack(damage).FromCard(this, cardPlay).TargetingAllOpponents(combatState)
                .Execute(choiceContext);
        }
        else
        {
            var target = cardPlay.Target ?? combatState.HittableEnemies.FirstOrDefault();
            if (target != null)
            {
                await DamageCmd.Attack(damage).FromCard(this, cardPlay).Targeting(target).Execute(choiceContext);
            }
        }

        await PowerCmd.Apply<ThornsPower>(choiceContext, player.Creature,
            base.DynamicVars[ThornsKey].BaseValue, player.Creature, this);
    }

    /// <summary>升级后才有：进入嗨到爆时从战斗牌堆回手。</summary>
    public async Task AfterHypedEntered(PlayerChoiceContext choiceContext)
    {
        if (!IsUpgraded) return;
        if (base.Owner == null || base.Pile == null) return;
        if (base.Pile.Type is not (PileType.Draw or PileType.Discard or PileType.Exhaust)) return;

        await CardPileCmd.Add(this, PileType.Hand);
    }
}
