using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.Rewards;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 幻梦无垠：获得5（7）格挡，造成5（7）伤害，将随机2（3）张牌加入抽牌堆。
/// 每次休息可选一个「梦」来决定本次数值变化的方向（见 <see cref="DreamChoiceReward"/>）。
/// </summary>
[Pool(typeof(EventCardPool))]
public class IllusoryDreamWhisper : TouhouAncientCards
{
    private const int BaseDamage = 5;
    private const int BaseBlock = 5;
    private const int BaseCards = 2;

    public override string? Author => "半节";

    public override bool GainsBlock => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>「泡影」变化规则说明；卡面与「梦」奖励共用。</summary>
    public static HoverTip BubbleIllusionTip => new(
        new LocString("gameplay_ui", "TOUHOUANCIENTS-BUBBLE_ILLUSION.title"),
        new LocString("gameplay_ui", "TOUHOUANCIENTS-BUBBLE_ILLUSION.description"));

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [BubbleIllusionTip];

    private int _damageBonus;
    private int _blockBonus;
    private int _cardsBonus;

    /// <summary>三围的持久增量：升级与「幻梦变化」都累加在这里，反序列化即恢复。</summary>
    [SavedProperty]
    public int DamageBonus
    {
        get => _damageBonus;
        set
        {
            AssertMutable();
            _damageBonus = Math.Max(value, 1 - BaseDamage);
            DynamicVars.Damage.BaseValue = BaseDamage + _damageBonus;
        }
    }

    [SavedProperty]
    public int BlockBonus
    {
        get => _blockBonus;
        set
        {
            AssertMutable();
            _blockBonus = Math.Max(value, 1 - BaseBlock);
            DynamicVars.Block.BaseValue = BaseBlock + _blockBonus;
        }
    }

    [SavedProperty]
    public int CardsBonus
    {
        get => _cardsBonus;
        set
        {
            AssertMutable();
            _cardsBonus = Math.Max(value, 1 - BaseCards);
            DynamicVars.Cards.BaseValue = BaseCards + _cardsBonus;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage + DamageBonus, ValueProp.Move),
        new BlockVar(BaseBlock + BlockBonus, ValueProp.Move),
        new CardsVar(BaseCards + CardsBonus)
    ];

    public IllusoryDreamWhisper()
        : base(2, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy, true)
    {
    }

    protected override void OnUpgrade()
    {
        DamageBonus += 2;
        BlockBonus += 2;
        CardsBonus += 1;
    }

    protected override void AfterDowngraded()
    {
        DamageBonus -= 2;
        BlockBonus -= 2;
        CardsBonus -= 1;
    }

    /// <summary>「幻梦变化」：三围各加一个增量，最终值不低于 1。</summary>
    public void ApplyDreamChange(int damageDelta, int blockDelta, int cardsDelta)
    {
        DamageBonus += damageDelta;
        BlockBonus += blockDelta;
        CardsBonus += cardsDelta;
    }

    /// <summary>每次休息都给这张牌挂一个（只含本牌的）三选一「梦」奖励组。</summary>
    public override bool TryModifyRestSiteHealRewards(Player player, List<Reward> rewards, bool isMimicked)
    {
        // 事件假冒的休息不算
        if (player != base.Owner || isMimicked) return false;

        rewards.Add(DreamChoiceReward.CreateSet(this));
        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(base.Owner.Creature, base.DynamicVars.Block, cardPlay);

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        await AddRandomCards(DynamicVars.Cards.IntValue);
    }

    /// <summary>随机牌来源与「添柴」一致：当前角色全部已解锁卡池，不限类型、不限稀有度。</summary>
    private async Task AddRandomCards(int count)
    {
        var player = base.Owner;
        if (player.Creature.CombatState == null) return;

        var pool = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint);
        var cards = CardFactory
            .GetForCombat(player, pool, count, player.RunState.Rng.CombatCardGeneration)
            .ToList();

        // 逐张加入并预览（同原版「羽化」），位置随机
        foreach (var card in cards)
        {
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Random));
        }
    }
}
