using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 全人类的绯想天：2费技能。消耗。保留。
/// 获得1能量，抽1张牌，获得1力量、1敏捷、1集中、1辉星，召唤1，铸造9，将正面Buff层数增加1。
/// 升级后获得固有。
/// </summary>
[Pool(typeof(EventCardPool))]
public class AllCreationHisou : TouhouAncientCards
{
    private const int energyCost = 2;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        // base.EnergyHoverTip,
        // HoverTipFactory.Static(StaticHoverTip.SummonDynamic, base.DynamicVars.Summon), 
        // //..HoverTipFactory.FromForge(),
        // HoverTipFactory.FromPower<StrengthPower>(),
        // HoverTipFactory.FromPower<DexterityPower>(),
        // HoverTipFactory.FromPower<FocusPower>()
    ];
        

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new SummonVar(5m)
    ];

    public AllCreationHisou() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;
        var player = base.Owner;

        // 1能量
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, player);
        // 抽1张牌
        await CardPileCmd.Draw(choiceContext, 1, player);
        // 1力量
        await PowerCmd.Apply<StrengthPower>(creature, 1m, creature, this);
        // 1敏捷
        await PowerCmd.Apply<DexterityPower>(creature, 1m, creature, this);
        // 1集中
        await PowerCmd.Apply<FocusPower>(creature, 1m, creature, this);
        // 1辉星
        await PlayerCmd.GainStars(1, player);
        // 召唤1
        await OstyCmd.Summon(choiceContext, player, base.DynamicVars.Summon.BaseValue, this);
        // 铸造9
        await ForgeCmd.Forge(9m, player, this);

        // 将所有正面Buff层数增加1
        var buffs = creature.Powers.Where(p => p.Type == PowerType.Buff).ToList();
        foreach (var buff in buffs)
        {
            await PowerCmd.ModifyAmount(buff, 1m, creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级后获得固有 — 由 CanonicalKeywords 处理
        
        AddKeyword(CardKeyword.Innate);
    }
}
