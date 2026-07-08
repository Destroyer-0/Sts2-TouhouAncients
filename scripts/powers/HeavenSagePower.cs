using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts.powers;

public class HeavenSagePower : TouhouAncientPowerModel
{
    private class Data
    {
        public readonly HashSet<CardModel> autoPlayingCards = new HashSet<CardModel>();

        public int cardPlayNum;

        public bool showedCapReachedMessage;
    }

    private const int _maxCardPlay = 99;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterCardDrawnEarly(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner.Creature != base.Owner || !card.Tags.Contains(CardTag.Defend))
        {
            return;
        }

        Data data = GetInternalData<Data>();
        if (data.cardPlayNum >= _maxCardPlay)
        {
            if (!data.showedCapReachedMessage)
            {
                Flash();
                ThinkCmd.Play(new LocString("powers", "TOUHOUANCIENTS-HEAVEN_SAGE_POWER.infiniteAutoPlayCapReached"), base.Owner);
                data.showedCapReachedMessage = true;
            }
            return;
        }
        data.autoPlayingCards.Add(card);
        data.cardPlayNum++;
        Flash();
        await CardCmd.AutoPlay(choiceContext, card, null);
        data.autoPlayingCards.Remove(card);
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!participants.Contains(base.Owner))
        {
            return Task.CompletedTask;
        }

        ResetInfiniteAutoPlayData();
        return Task.CompletedTask;
    }

    private void ResetInfiniteAutoPlayData()
    {
        Data internalData = GetInternalData<Data>();
        internalData.cardPlayNum = 0;
        internalData.showedCapReachedMessage = false;
    }
}