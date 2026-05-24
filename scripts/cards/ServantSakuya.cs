using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 时间仆从：生成2把小刀。回合结束时，本回合每消耗过一张牌，对目标造成4（5）点伤害。
/// </summary>
[Pool(typeof(EventCardPool))]
public class ServantSakuya : TouhouAncientCards
{
    private const int energyCost = 1;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AnyEnemy;
    private const bool shouldShowInCardLibrary = true;


    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Minion };

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Shiv>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    /// <summary>记录打出时选择的目标，供回合结束时造成伤害。</summary>
    private List<Creature?> _target = new();

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new DamageVar(5m, ValueProp.Move)
    ];

    public ServantSakuya() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 生成2把小刀
        await Shiv.CreateInHand(base.Owner, 2, base.Owner.Creature.CombatState);

        // 2. 记录目标，供回合结束时使用
        _target.Add(cardPlay.Target);
    }

    /// <summary>
    /// 在 BeforeFlush 之后、AfterTurnEnd 之前，根据本回合消耗牌数造成伤害。
    /// BeforeFlush 在虚无消耗之后触发，故用 BeforeFlush 确保时机正确。
    /// </summary>
    public override async Task BeforeFlush(PlayerChoiceContext choiceContext, Player player)
    {
        if (base.Owner != player) return;
        if (base.CombatState == null) return;
        if (_target.Count == 0) return;

        // 统计本回合消耗过的牌数
        var combatState = player.Creature.CombatState;
        int exhaustedCount = CombatManager.Instance.History.Entries
            .OfType<CardExhaustedEntry>()
            .Count(e => e.HappenedThisTurn(combatState) && e.Card.Owner == player);

        if (exhaustedCount <= 0) return;
        if (!Owner.Creature.IsAlive) return;

        foreach (var target in _target)
        {
            if (target == null || !target.IsEnemy || !target.IsAlive) continue;
            await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
                .WithHitCount(exhaustedCount)
                .FromCard(this)
                .Targeting(target)
                .Execute(choiceContext);
        }

        _target.Clear();
        // 享受活力与力量加成（ValueProp.Attack | ValueProp.Powered 自带力量加成）
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Cards.UpgradeValueBy(1m);
    }

    // /// <summary>
    // /// 旧实现：保留格挡并结束你的回合，开始一个没有抽牌阶段的额外回合。
    // /// </summary>
    // private bool _willTakeExtraTurn;
    // protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    // {
    //     var creature = base.Owner.Creature;
    //     await PowerCmd.Apply<BlurPower>(creature, 1m, creature, this);
    //     _willTakeExtraTurn = true;
    //     PlayerCmd.EndTurn(base.Owner, canBackOut: false);
    // }
    // public override bool ShouldTakeExtraTurn(Player player)
    // {
    //     return _willTakeExtraTurn && player == base.Owner;
    // }
    // public override async Task AfterTakingExtraTurn(Player player)
    // {
    //     if (player != base.Owner) return;
    //     _willTakeExtraTurn = false;
    //     var creature = player.Creature;
    //     var combatState = creature.CombatState;
    //     await PowerCmd.Apply<NoDrawPower>(creature, 1m, creature, this);
    //     int shivCount = IsUpgraded ? ShivCountUpgraded : ShivCount;
    //     await Shiv.CreateInHand(player, shivCount, combatState);
    // }
}