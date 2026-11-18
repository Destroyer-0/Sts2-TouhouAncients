using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class DecisiveMoment() : TouhouAncientCards(1, CardType.Quest, CardRarity.Quest, TargetType.AnyEnemy, true)
{
    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
    public override int MaxUpgradeLevel => 0;
    public override string PortraitPath => PhotoFilm.FallbackPortrait;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<PhotoFilm>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { Monster: { IntendsToAttack: true } monster } target) return;

        int damage = monster.NextMove.Intents.OfType<AttackIntent>()
            .Sum(intent => intent.GetTotalDamage(CombatState.PlayerCreatures, target));
        bool perfect = damage >= Owner.Creature.CurrentHp;

        var film = Owner.RunState.CreateCard<PhotoFilm>(Owner);
        film.SetPhoto(monster.Id, perfect ? damage * 2 : damage, perfect);
        var result = await CardPileCmd.Add(film, PileType.Deck);
        if (result is { success: true, cardAdded: PhotoFilm original })
        {
            var copy = CombatState.CloneCard(original);
            copy.DeckVersion = original;
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
            CardCmd.PreviewCardPileAdd(result, 2f);
        }
    }
}
