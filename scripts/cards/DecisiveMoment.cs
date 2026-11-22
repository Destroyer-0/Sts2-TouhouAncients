using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using TouhouAncients.Scripts.Vfx;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class DecisiveMoment() : TouhouAncientCards(1, CardType.Quest, CardRarity.Ancient, TargetType.AnyEnemy, true)
{
    //public override bool UseAncientFrame => true;
    public override string? Author => "わんこソラ";

    public override bool CanBeGeneratedByModifiers => false;
    public override int MaxUpgradeLevel => 0;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<PhotoFilm>()];

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (base.CombatState == null)
            {
                return false;
            }

            return base.CombatState.HittableEnemies.Any((Creature e) =>
                e.Monster is { IntendsToAttack: true }
                && e.Monster?.NextMove.Intents.OfType<AttackIntent>()
                    .Sum(intent => intent.GetTotalDamage(CombatState.PlayerCreatures, e)) >= Owner.Creature.CurrentHp);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target is not { Monster: { } monster } target) return;

        bool attacking = monster.IntendsToAttack;
        int damage = monster.NextMove.Intents.OfType<AttackIntent>()
            .Sum(intent => intent.GetTotalDamage(CombatState.PlayerCreatures, target));
        bool perfect = damage >= Owner.Creature.CurrentHp;

        // 先拍：合焦 → 白闪，闪光瞬间拓印，拓完才把照片发出来
        var shutter = NCombatRoom.Instance?.GetCreatureNode(target) is { } targetNode
            ? NPhotoShutterVfx.Create(targetNode, perfect, attacking)
            : null;
        if (shutter != null)
        {
            NCombatRoom.Instance?.AddChildSafely(shutter);
            await shutter.Closed;
        }

        // 目标不出手就是空拍，只放特效不给底片
        if (attacking)
        {
            await PhotoFilmPhoto.Capture(monster.Id);

            var film = Owner.RunState.CreateCard<PhotoFilm>(Owner);
            film.SetPhoto(monster.Id, perfect ? damage * 2 : damage, perfect);
            var result = await CardPileCmd.Add(film, PileType.Deck);
            if (result is { success: true, cardAdded: PhotoFilm original })
            {
                CardCmd.PreviewCardPileAdd(result, 2f);
                var copy = CombatState.CloneCard(original);
                copy.DeckVersion = original;
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
            }
        }

        shutter?.ShowBadges();
        if (shutter != null) await shutter.Finished;
    }
}