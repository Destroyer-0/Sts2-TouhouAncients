using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 时间侍从：保留格挡并结束你的回合，开始一个没有抽牌阶段的回合，并将5（升级后9）张小刀加入手牌。
/// 实现参考：佩尔之眼（PaelsEye）的额外回合机制。
/// </summary>
[Pool(typeof(EventCardPool))]
public class ServantSakuya : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    private const int ShivCount = 5;
    private const int ShivCountUpgraded = 9;

    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Minion };
    /// <summary>
    /// 标记是否需要触发额外回合。卡牌在弃牌堆中仍会被 IterateHookListeners 遍历。
    /// </summary>
    private bool _willTakeExtraTurn;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("ShivCount", ShivCount)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    public ServantSakuya() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;

        // 1. 保留格挡（BlurPower 使格挡在回合结束时不会消失）
        await PowerCmd.Apply<BlurPower>(creature, 1m, creature, this);

        // 2. 标记需要额外回合
        _willTakeExtraTurn = true;

        // 3. 结束当前回合
        PlayerCmd.EndTurn(base.Owner, canBackOut: false);
    }

    /// <summary>
    /// 在当前回合结束时被调用，判断是否需要给玩家额外回合。
    /// </summary>
    public override bool ShouldTakeExtraTurn(Player player)
    {
        return _willTakeExtraTurn && player == base.Owner;
    }

    /// <summary>
    /// 额外回合开始前被调用。在这里生成小刀并施加无法抽牌效果。
    /// </summary>
    public override async Task AfterTakingExtraTurn(Player player)
    {
        if (player != base.Owner) return;
        _willTakeExtraTurn = false;

        var creature = player.Creature;
        var combatState = creature.CombatState;

        // 施加无法抽牌（新回合的抽牌阶段将被跳过）
        await PowerCmd.Apply<NoDrawPower>(creature, 1m, creature, this);

        // 生成小刀到手牌
        int shivCount = IsUpgraded ? ShivCountUpgraded : ShivCount;
        await Shiv.CreateInHand(player, shivCount, combatState);
    }
}
