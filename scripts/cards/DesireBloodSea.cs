using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Patches.Features;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.monsters;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 欲壑血海：饕餮尤魔的战斗状态牌（1 费，不消耗）。
/// 目标为任意生物（自己 / 队友 / 敌人）。打出后目标失去 4 点生命并获得 1 点力量；
/// 这张牌在本场战斗中每次打出，额外失去的生命 +4、给予的力量 +1（每张牌实例各自累计）。
/// 玩家用此牌给尤魔喂力量，配合尤魔的"血煞祸劫"使输出成倍增长。
/// </summary>
[Pool(typeof(StatusCardPool))]
public class DesireBloodSea : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Status;
    private const CardRarity rarity = CardRarity.Status;
    
    public override bool CanBeGeneratedByModifiers => false;

    public override int MaxUpgradeLevel => -1;

    public DesireBloodSea() : base(energyCost, type, rarity, CustomTargetType.Anyone, true)
    {
    }

    /// <summary>
    /// 基础数值：目标失去 4 生命、获得 1 力量。
    /// 额外失去生命（ExtraHpLoss）与额外力量（ExtraStrength）随实例持久化，
    /// 每次打出 +4 / +1（每张牌实例各自累计，参照 MagicWallet 的 UpgradeValueBy 模式）。
    /// </summary>
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("HpLoss", 4m),
        new DynamicVar("Strength", 1m),
        new DynamicVar("ExtraHpLoss", 4m),
        new DynamicVar("ExtraStrength", 1m)
    ];

    /// <summary>
    /// 打出：目标失去（4 + 本牌累计额外失去）点生命，并获得（1 + 本牌累计额外力量）点力量。
    /// 每张牌实例各自累计（DynamicVar 随实例克隆/序列化保留）。
    /// 若目标是饕餮尤魔，通知尤魔本回合已被"欲壑血海"瞄准（重置连续未喂牌计数）。
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Creature target = cardPlay.Target!;


        await CreatureCmd.Damage(choiceContext, [target], base.DynamicVars["HpLoss"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            base.Owner.Creature, this, cardPlay);
        await PowerCmd.Apply<StrengthPower>(choiceContext, target, base.DynamicVars["Strength"].BaseValue, base.Owner.Creature, this);
        
        // 通知饕餮尤魔：本回合已被欲壑血海瞄准
        if (target.Monster is ToutetsuYuumaMonster yuuma)
        {
            yuuma.NotifyTargetedByDesireBloodSea();
        }
        
        // 本牌实例累计：额外失去生命 +4、额外力量 +1
        base.DynamicVars["HpLoss"].UpgradeValueBy(base.DynamicVars["ExtraHpLoss"].BaseValue);
        base.DynamicVars["Strength"].UpgradeValueBy(base.DynamicVars["ExtraStrength"].BaseValue);

        // 不消耗：留在弃牌堆（由基类默认行为处理）
    }
}