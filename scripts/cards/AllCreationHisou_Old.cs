using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 全人类的绯想天（旧版）：2费能力。消耗。保留。
/// 获得1能量，抽1张牌，获得1力量、1敏捷、1集中、1辉星，召唤1，铸造9，将正面Buff层数增加1。
/// 升级后获得固有。
/// </summary>
[Pool(typeof(EventCardPool))]
public class AllCreationHisou_Old : TouhouAncientCards
{
    private const int energyCost = 2;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new SummonVar(5m)
    ];

    public AllCreationHisou_Old() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;
        var creature = base.Owner.Creature;
        var player = base.Owner;

        List<Creature> enumerable = (from c in base.CombatState.GetTeammatesOf(base.Owner.Creature)
            where c is { IsAlive: true, IsPlayer: true }
            select c).ToList();

        async Task ApplyOnAllPlayer(Func<Creature,Task> task)
        {
            foreach (Creature item in enumerable)
            {
                await task(item);
            }
        }

        await ApplyOnAllPlayer(x => PowerCmd.Apply<AllCreationHisouPower>(x, 1m, base.Owner.Creature, this));
        await ApplyOnAllPlayer(x => PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, x.Player!));
        await ApplyOnAllPlayer(x => PlayerCmd.GainStars(1, x.Player!));
        await ApplyOnAllPlayer(x => CardPileCmd.Draw(choiceContext, 1, x.Player!));
        await ApplyOnAllPlayer(x => OstyCmd.Summon(choiceContext, x.Player!, base.DynamicVars.Summon.BaseValue, this));
        await ApplyOnAllPlayer(x => ForgeCmd.Forge(9m, x.Player!, this));
        await ApplyOnAllPlayer(x => PowerCmd.Apply<StrengthPower>(x, 1m, creature, this));
        await ApplyOnAllPlayer(x => PowerCmd.Apply<DexterityPower>(x, 1m, creature, this));
        await ApplyOnAllPlayer(x => PowerCmd.Apply<FocusPower>(x, 1m, creature, this));
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}
