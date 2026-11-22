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
        // 牌组里的写真底片也会添加同一选项，先到先得
        if (options.Any(o => o.OptionId == PublishPhotoOption.OptionIdValue)) return false;
        options.Add(new PublishPhotoOption(player, this));
        return true;
    }
}

internal sealed class PublishPhotoOption(Player owner, RelicModel? source) : RestSiteOption(owner)
{
    internal const string OptionIdValue = "PUBLISH_PHOTO";
    public override string OptionId => OptionIdValue;
    public override bool IsEnabled => Owner.Deck.Cards.Any(CanPublish);

    internal static LocString Loc(string suffix) => new("rest_site_ui", $"OPTION_{OptionIdValue}.{suffix}");
    private bool CanPublish(CardModel card) => card is PhotoFilm && card.IsRemovable;

    public override async Task<bool> OnSelect()
    {
        if (!IsEnabled) return false;
        // 只剩一张也弹窗，给玩家反悔余地
        var prefs = new CardSelectorPrefs(Loc("selectPrompt"), 1)
        {
            Cancelable = true,
            RequireManualConfirmation = true,
        };
        var selected = (await CardSelectCmd.FromDeckGeneric(Owner, prefs, CanPublish)).FirstOrDefault();
        if (selected is not PhotoFilm film) return false;

        source?.Flash();
        await CardPileCmd.RemoveFromDeck(film);
        await PlayerCmd.GainGold(film.PublishGold, Owner);
        return true;
    }
}
