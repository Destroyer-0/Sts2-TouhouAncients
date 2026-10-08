using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 光辉宝塔：3费攻击，消耗。
/// 对所有敌人造成30(升级后37)点伤害并击晕他们。
/// 斩杀时，获得{Gold}金币。
/// 从你的牌组中移除，并重新加入寻宝奖励中。
/// </summary>
[Pool(typeof(EventCardPool))]
public class ShiningTower : TouhouAncientCards
{
    public override string? Author => "hayaten";
    
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
        StunIntent.GetStaticHoverTip(),
        new HoverTip(
            new LocString("rest_site_ui", "OPTION_TREASURE.name"),
            DescriptionForTip())
    ];

    private LocString DescriptionForTip()
    {
        var desc = new LocString("rest_site_ui", "OPTION_TREASURE.description");
        desc.Add("Gold", DowsingRod.GoldCost);
        return desc;
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public ShiningTower() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 伤害前记下斩杀判定与特效坐标：伤害后 Creature 可能已被移出场景
        var enemies = base.CombatState.Enemies.Where(e => e.IsAlive).ToList();
        var fatalPositions = enemies
            .Where(e => e.Powers.All(p => p.ShouldOwnerDeathTriggerFatal()))
            .ToDictionary(
                e => e,
                e => TestMode.IsOff
                    ? NCombatRoom.Instance?.GetCreatureNode(e)?.VfxSpawnPosition
                    : null);

        // 对所有敌人造成伤害并击晕
        AttackCommand attackCommand = await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(base.CombatState)
            .WithHitFx("vfx/vfx_starry_impact", null, "blunt_attack.mp3")
            .SpawningHitVfxOnEachCreature()
            .Execute(choiceContext);

        // 同贪婪之手：只算被本次伤害斩杀的敌人，每个击杀各播一次金币特效
        int killCount = 0;
        foreach (var result in attackCommand.Results.SelectMany(r => r))
        {
            if (!result.WasTargetKilled || !fatalPositions.TryGetValue(result.Receiver, out var pos)) continue;
            killCount++;
            if (pos.HasValue)
            {
                VfxCmd.PlayVfx(pos.Value, "vfx/vfx_coin_explosion_regular", NCombatRoom.Instance?.CombatVfxContainer);
            }
        }

        if (killCount > 0)
        {
            await PlayerCmd.GainGold(base.DynamicVars.Gold.IntValue * killCount, base.Owner);
        }

        // 击晕所有存活敌人
        foreach (var enemy in base.CombatState.Enemies.Where(e => e.IsAlive))
        {
            await CreatureCmd.Stun(enemy);
        }
        
        // 回收：有寻龙尺才把它放回寻宝奖励
        base.Owner?.Relics.OfType<DowsingRod>().FirstOrDefault()?.AddTowerToStorage(this);

        // 打出的是战斗克隆体，牌组原牌由 DeckVersion 指向；无条件从牌组移除
        if (this.DeckVersion is { } deckCard && deckCard.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(deckCard, showPreview: false);
        }
    }

    protected override void OnUpgrade()
    {
        base.DynamicVars.Damage.UpgradeValueBy(10m);
    }
}