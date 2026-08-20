using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 火鼠的皮衣：蓬莱谜题（类型 1）。1 费。
/// 目前暂无额外效果；打出后完成对应谜题并将此牌移出战斗。
/// </summary>
[Pool(typeof(EventCardPool))]
public sealed class HinezumiNoKawagoromoCard : HouraiPuzzleCard
{
    private const int energyCost = 1;

    public HinezumiNoKawagoromoCard() : base(energyCost)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override PuzzleCardName PuzzleType => PuzzleCardName.火鼠的皮衣;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<BlazingBodyPower>(choiceContext, [Owner.Creature], base.DynamicVars["Cards"].IntValue, Owner.Creature, this);
        await base.OnPlay(choiceContext, cardPlay);
    }
}
