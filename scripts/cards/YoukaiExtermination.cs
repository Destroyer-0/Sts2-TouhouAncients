using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 妖怪治退：1费攻击，单体造成 4（升级后 6）点伤害 2 次。
/// 斩杀时，获得 1 能量，这张牌在本局游戏中的伤害永久性增加 1。
/// 永久成长照原版「巨镰」（TheScythe）的 [SavedProperty] 写法。
/// </summary>
[Pool(typeof(EventCardPool))]
public class YoukaiExtermination : TouhouAncientCards
{
    private const int BaseDamageNormal = 6;

    /// <summary>升级给本牌的永久伤害加成；不能累加进 [SavedProperty]（见 <see cref="UpdateDamage"/>）。</summary>
    private const int UpgradeDamageBonus = 2;

    private int _currentDamage = BaseDamageNormal;
    private int _increasedDamage;

    private int UpgradeDamage => CurrentUpgradeLevel * UpgradeDamageBonus;

    public override string? Author => "TOKIAME";
    public YoukaiExtermination()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy, true)
    {
    }

    [SavedProperty]
    public int CurrentDamage
    {
        get => _currentDamage;
        set
        {
            AssertMutable();
            _currentDamage = value;
            DynamicVars.Damage.BaseValue = _currentDamage;
        }
    }

    [SavedProperty]
    public int IncreasedDamage
    {
        get => _increasedDamage;
        set
        {
            AssertMutable();
            _increasedDamage = value;
            UpdateDamage();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(CurrentDamage, ValueProp.Move),
        new RepeatVar(2),
        new EnergyVar(1),
        new IntVar("Increase", 1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [HoverTipFactory.Static(StaticHoverTip.Fatal)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 参照 Feed / 光辉宝塔：伤害前记录哪些敌人死亡会触发「斩杀」
        var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
        var fatalStates = enemies.ToDictionary(
            e => e,
            e => e.Powers.All(p => p.ShouldOwnerDeathTriggerFatal()));

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .WithHitCount(DynamicVars.Repeat.IntValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        int kills = fatalStates.Count(x => x is { Value: true, Key.IsDead: true });
        if (kills <= 0) return;

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        int gain = DynamicVars["Increase"].IntValue;
        BuffFromPlay(gain);
        // 战斗副本与牌组原件同时成长，跨战斗持久（参照巨镰）
        (DeckVersion as YoukaiExtermination)?.BuffFromPlay(gain);
    }

    protected override void OnUpgrade()
    {
        // 只改不存档的 DynamicVars；升级加成走 UpgradeDamage 推导，累加进 [SavedProperty] 会被读档/联机重建重放一次
        DynamicVars["Increase"].UpgradeValueBy(1m);
        UpdateDamage();
    }

    protected override void AfterDowngraded()
    {
        UpdateDamage();
    }

    private void BuffFromPlay(int extraDamage)
    {
        IncreasedDamage += extraDamage;
        UpdateDamage();
    }

    private void UpdateDamage()
    {
        CurrentDamage = BaseDamageNormal + IncreasedDamage + UpgradeDamage;
    }
}
