using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class PhotoFilm() : TouhouAncientCards(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy, true)
{
    internal const string FallbackPortrait = "res://images/icon/relics/IconLarge/camera.png";
    private const string RemainingCountKey = "RemainingCount";
    private const int MaxPlays = 2;
    private int _remainingCount = MaxPlays;
    private int _photoValue;
    private bool _isPerfect;
    private string _monsterEntry = "";

    public override bool CanBeGeneratedByModifiers => false;
    public override bool CanBeGeneratedInCombat => false;
    public override int MaxUpgradeLevel => 0;
    public override bool GainsBlock => true;
    public override string PortraitPath => FallbackPortrait;
    public override string Title => PhotoFilm_IsPerfect
        ? new LocString("cards", Id.Entry + ".perfectTitle").GetFormattedText()
        : base.Title;

    protected override CardLocation GetResultLocationForCardPlay() =>
        PhotoFilm_RemainingCount <= 1
            ? new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom)
            : base.GetResultLocationForCardPlay();

    [SavedProperty]
    public int PhotoFilm_RemainingCount
    {
        get => _remainingCount;
        set
        {
            AssertMutable();
            _remainingCount = value;
            DynamicVars[RemainingCountKey].BaseValue = value;
        }
    }

    [SavedProperty]
    public int PhotoFilm_Value
    {
        get => _photoValue;
        set
        {
            AssertMutable();
            _photoValue = value;
            DynamicVars.Block.BaseValue = value;
            DynamicVars.Damage.BaseValue = value;
        }
    }

    [SavedProperty]
    public bool PhotoFilm_IsPerfect
    {
        get => _isPerfect;
        set
        {
            AssertMutable();
            _isPerfect = value;
        }
    }

    [SavedProperty]
    public string PhotoFilm_MonsterEntry
    {
        get => _monsterEntry;
        set
        {
            AssertMutable();
            _monsterEntry = value;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(0m, ValueProp.Move),
        new BlockVar(0m, ValueProp.Move),
        new IntVar(RemainingCountKey, MaxPlays),
    ];

    internal void SetPhoto(ModelId monsterId, int value, bool perfect)
    {
        PhotoFilm_MonsterEntry = monsterId.Entry;
        PhotoFilm_Value = value;
        PhotoFilm_IsPerfect = perfect;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (PhotoFilm_RemainingCount <= 0 || cardPlay.Target is not { } target) return;

        var original = DeckVersion as PhotoFilm;
        PhotoFilm_RemainingCount = Math.Max(0, (original?.PhotoFilm_RemainingCount ?? PhotoFilm_RemainingCount) - 1);
        if (original?.Pile?.Type == PileType.Deck)
            original.PhotoFilm_RemainingCount = PhotoFilm_RemainingCount;

        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay).Targeting(target).Execute(choiceContext);

        if (PhotoFilm_RemainingCount > 0) return;
        if (original?.Pile?.Type == PileType.Deck)
            await CardPileCmd.RemoveFromDeck(original, showPreview: false);
    }
}

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
internal static class PhotoFilmPortraitPatch
{
    private static readonly Dictionary<string, ImageTexture> Portraits = [];
    private static readonly Vector2I PortraitSize = new(320, 240);

    private static void Postfix(NCard __instance, TextureRect ____ancientPortrait)
    {
        if (__instance.Model is not PhotoFilm { PhotoFilm_MonsterEntry.Length: > 0 } film) return;
        if (GetPortrait(film.PhotoFilm_MonsterEntry, __instance.GetTree()) is { } portrait)
            ____ancientPortrait.Texture = portrait;
    }

    private static ImageTexture? GetPortrait(string entry, SceneTree? tree)
    {
        if (Portraits.TryGetValue(entry, out var cached)) return cached;
        if (tree == null || ModelDb.GetByIdOrNull<MonsterModel>(new ModelId("MONSTER", entry)) is not { } canonical)
            return null;

        var placeholder = GD.Load<Texture2D>(PhotoFilm.FallbackPortrait).GetImage();
        if (placeholder.IsCompressed()) placeholder.Decompress();
        placeholder.Resize(PortraitSize.X, PortraitSize.Y);
        placeholder.Convert(Image.Format.Rgba8);
        var portrait = ImageTexture.CreateFromImage(placeholder);
        Portraits.Add(entry, portrait);
        tree.Root.CallDeferred(Node.MethodName.AddChild, CreateViewport(canonical, portrait));
        return portrait;
    }

    private static SubViewport CreateViewport(MonsterModel canonical, ImageTexture portrait)
    {
        var monster = canonical.ToMutable();
        monster.Rng = Rng.Chaotic;
        monster.RunRng = new RunRngSet(Rng.Chaotic);
        monster.SetUpForCombat();
        var creature = NCreature.Create(new Creature(monster, CombatSide.Enemy, null)
        {
            CombatState = NullCombatState.Instance,
        });
        var viewport = new SubViewport
        {
            Size = PortraitSize,
            TransparentBg = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
        };
        viewport.AddChild(creature);
        viewport.Ready += () =>
        {
            creature.SetupForBestiary();
            var bounds = creature.Hitbox.GetGlobalRect();
            var fit = Mathf.Min(PortraitSize.X / Mathf.Max(1, bounds.Size.X), PortraitSize.Y / Mathf.Max(1, bounds.Size.Y));
            creature.Scale *= fit * 0.9f;
            creature.Position += (Vector2)PortraitSize * 0.5f - creature.Hitbox.GetGlobalRect().GetCenter();
            RenderingServer.Singleton.Connect(RenderingServer.SignalName.FramePostDraw, Callable.From(() =>
            {
                if (!GodotObject.IsInstanceValid(viewport)) return;
                var image = viewport.GetTexture().GetImage();
                if (image != null && !image.IsEmpty())
                {
                    image.Convert(Image.Format.Rgba8);
                    portrait.Update(image);
                }
                viewport.QueueFree();
            }), (uint)GodotObject.ConnectFlags.OneShot);
        };
        return viewport;
    }
}
