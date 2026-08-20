using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 光辉宝塔：3费攻击，消耗。
/// 对所有敌人造成30(升级后37)点伤害并击晕他们。
/// 斩杀时，获得77金币。
/// 从你的牌组中移除，并重新加入寻宝奖励中。
/// </summary>
[Pool(typeof(EventCardPool))]
public class ShiningTower : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Event;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;
    public override bool UseAncientFrame => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(37m, ValueProp.Move),
        new GoldVar(188)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Fatal),
        StunIntent.GetStaticHoverTip()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public ShiningTower() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 参照 Feed 的斩杀判定：伤害前记录 shouldTriggerFatal
        var enemies = base.CombatState.Enemies.Where(e => e.IsAlive).ToList();
        var fatalStates = enemies.ToDictionary(
            e => e,
            e => e.Powers.All(p => p.ShouldOwnerDeathTriggerFatal()));

        // 对所有敌人造成伤害并击晕
        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_starry_impact", null, "blunt_attack.mp3")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);

        // 击晕所有存活敌人
        foreach (var enemy in base.CombatState.Enemies.Where(e => e.IsAlive))
        {
            await CreatureCmd.Stun(enemy);
        }

        await PlayerCmd.GainGold(base.DynamicVars.Gold.IntValue * fatalStates.Count(x => x is { Value: true, Key.IsDead: true }), base.Owner);
    }

    /// <summary>
    /// 当卡牌被消耗（进入弃牌堆以外的移除）时，重新加入寻龙尺的存储。
    /// </summary>
    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card != this || !CombatManager.Instance.IsInProgress) return;
        if (card.Pile?.Type == PileType.Exhaust && oldPileType == PileType.Play)
        {
            // 卡牌被消耗（正常打出后进入消耗堆），重新加入寻龙尺存储
            var dowsingRod = base.Owner?.Relics.OfType<DowsingRod>().FirstOrDefault();
            if (dowsingRod != null)
            {
                dowsingRod.AddCardToStorage(this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(10m);
    }
}