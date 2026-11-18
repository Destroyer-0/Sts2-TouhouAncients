using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class ReporterCamera : TouhouAncientRelics
{
    public override bool HasUponPickupEffect => true;
    public override string PackedIconPath => PhotoFilm.FallbackPortrait;
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromCardWithCardHoverTips<DecisiveMoment>(),
        new HoverTip(PublishPhotoOption.Loc("name"), PublishPhotoOption.Loc("description")),
    ];

    public override async Task AfterObtained()
    {
        var card = Owner.RunState.CreateCard<DecisiveMoment>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        options.Add(new PublishPhotoOption(player, this));
        return true;
    }
}

internal sealed class PublishPhotoOption(Player owner, ReporterCamera camera) : RestSiteOption(owner)
{
    private const string Id = "PUBLISH_PHOTO";
    public override string OptionId => Id;
    public override bool IsEnabled => Owner.Deck.Cards.Any(CanPublish);

    internal static LocString Loc(string suffix) => new("rest_site_ui", $"OPTION_{Id}.{suffix}");
    private static bool CanPublish(CardModel card) => card is PhotoFilm && card.IsRemovable;

    public override async Task<bool> OnSelect()
    {
        if (!IsEnabled) return false;
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 1) { Cancelable = true };
        var selected = (await CardSelectCmd.FromDeckGeneric(Owner, prefs, CanPublish)).FirstOrDefault();
        if (selected is not PhotoFilm film) return false;

        int gold = film.PhotoFilm_Value * (film.PhotoFilm_IsPerfect ? 10 : 5);
        camera.Flash();
        await CardPileCmd.RemoveFromDeck(film);
        await PlayerCmd.GainGold(gold, Owner);
        return true;
    }
}
