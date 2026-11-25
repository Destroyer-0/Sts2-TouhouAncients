using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.Hooks;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.cards;

[Pool(typeof(EventCardPool))]
public class PhotoFilm() : TouhouAncientCards(0, CardType.Skill, CardRarity.Event, TargetType.AnyEnemy, true)
{
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
    public override bool DontUsePortraitBorderAndTitleBanner => HasRealPhoto;
    public override string? Author => HasRealPhoto ? null : "-不死蓬萊-"; 
    public override string Title => PhotoFilm_IsPerfect
        ? new LocString("cards", Id.Entry + ".perfectTitle").GetFormattedText()
        : base.Title;

    /// <summary>图鉴/悬停预览（canonical）或拍不到怪物时回落 PhotoFilm.png，否则用拍立得底图 + 怪物拓印。</summary>
    public override Texture2D? CustomPortrait => PhotoFilmPhoto.Get(this);

    public override IEnumerable<string> AllPortraitPaths => [PortraitPath, PhotoFilmPhoto.BasePath];

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
        new DamageVar(0m, ValueProp.Move | ValueProp.Unpowered | ValueProp.Unblockable),
        new BlockVar(0m, ValueProp.Move),
        new IntVar(RemainingCountKey, MaxPlays),
    ];

    internal void SetPhoto(ModelId monsterId, int value, bool perfect)
    {
        PhotoFilm_MonsterEntry = monsterId.Entry;
        PhotoFilm_Value = value;
        PhotoFilm_IsPerfect = perfect;
    }

    /// <summary>拍到过怪物才算真照片。不能用 IsCanonical：图鉴/衍生预览里的模型是可变克隆，那里 IsCanonical 为 false。</summary>
    internal bool HasRealPhoto => _monsterEntry.Length > 0;

    /// <summary>登报可得的金币：拍摄效果 ×10，完美底片 ×20。</summary>
    internal int PublishGold => PhotoFilm_Value * (PhotoFilm_IsPerfect ? 20 : 10);

    /// <summary>只在真底片上给登报提示，否则图鉴/衍生预览会显示 0 金币。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HasRealPhoto
        ? [new HoverTip(PublishGoldLoc())]
        : [];

    /// <summary>牌组里有底片就能登报，不必留着相机（没有相机时选项不闪）。</summary>
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner || options.Any(o => o.OptionId == PublishPhotoOption.OptionIdValue)) return false;
        options.Add(new PublishPhotoOption(player, player.Relics.OfType<ReporterCamera>().FirstOrDefault()));
        return true;
    }

    private LocString PublishGoldLoc()
    {
        var loc = new LocString("cards", Id.Entry + ".publishGoldTip");
        loc.Add("Gold", PublishGold);
        return loc;
    }

    // show 只认 IfUpgradedVar，布尔条件要走智能格式的 | 分支
    protected override void AddExtraArgsToDescription(LocString description)
    {
        description.Add("ShowValue", HasRealPhoto);
        description.Add("IfPerfect", PhotoFilm_IsPerfect);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (PhotoFilm_RemainingCount <= 0 || cardPlay.Target is not { } target) return;

        var original = DeckVersion as PhotoFilm;
        PhotoFilm_RemainingCount = Math.Max(0, (original?.PhotoFilm_RemainingCount ?? PhotoFilm_RemainingCount) - 1);
        if (original?.Pile?.Type == PileType.Deck)
            original.PhotoFilm_RemainingCount = PhotoFilm_RemainingCount;

        await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage, (CardModel)this, cardPlay);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (PhotoFilm_RemainingCount > 0) return;
        ExhaustOnNextPlay = true;
        if (original?.Pile?.Type == PileType.Deck)
            await CardPileCmd.RemoveFromDeck(original, showPreview: false);
    }
}

// 旧实现：把渲染图塞进 %AncientPortrait。PhotoFilm 改成 Event 稀有度后走的是 _portrait，这段已无效果，留作参考。
// [HarmonyPatch(typeof(NCard), "UpdatePortrait")]
// internal static class PhotoFilmPortraitPatch
// {
//     private static readonly Dictionary<string, ImageTexture> Portraits = [];
//     private static readonly Vector2I PortraitSize = new(320, 240);
//
//     private static void Postfix(NCard __instance, TextureRect ____ancientPortrait)
//     {
//         if (__instance.Model is not PhotoFilm { PhotoFilm_MonsterEntry.Length: > 0 } film) return;
//         if (GetPortrait(film.PhotoFilm_MonsterEntry, __instance.GetTree()) is { } portrait)
//             ____ancientPortrait.Texture = portrait;
//     }
//
//     private static ImageTexture? GetPortrait(string entry, SceneTree? tree)
//     {
//         if (Portraits.TryGetValue(entry, out var cached)) return cached;
//         if (tree == null || ModelDb.GetByIdOrNull<MonsterModel>(new ModelId("MONSTER", entry)) is not { } canonical)
//             return null;
//
//         var placeholder = GD.Load<Texture2D>("res://images/icon/relics/IconLarge/SpireHeroesLegend.png").GetImage();
//         if (placeholder.IsCompressed()) placeholder.Decompress();
//         placeholder.Resize(PortraitSize.X, PortraitSize.Y);
//         placeholder.Convert(Image.Format.Rgba8);
//         var portrait = ImageTexture.CreateFromImage(placeholder);
//         Portraits.Add(entry, portrait);
//         tree.Root.CallDeferred(Node.MethodName.AddChild, CreateViewport(canonical, portrait));
//         return portrait;
//     }
//
//     private static SubViewport CreateViewport(MonsterModel canonical, ImageTexture portrait)
//     {
//         var monster = canonical.ToMutable();
//         monster.Rng = Rng.Chaotic;
//         monster.RunRng = new RunRngSet(Rng.Chaotic);
//         monster.SetUpForCombat();
//         var creature = NCreature.Create(new Creature(monster, CombatSide.Enemy, null)
//         {
//             CombatState = NullCombatState.Instance,
//         });
//         var viewport = new SubViewport
//         {
//             Size = PortraitSize,
//             TransparentBg = true,
//             RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
//         };
//         viewport.AddChild(creature);
//         viewport.Ready += () =>
//         {
//             creature.SetupForBestiary();
//             var bounds = creature.Hitbox.GetGlobalRect();
//             var fit = Mathf.Min(PortraitSize.X / Mathf.Max(1, bounds.Size.X), PortraitSize.Y / Mathf.Max(1, bounds.Size.Y));
//             creature.Scale *= fit * 0.9f;
//             creature.Position += (Vector2)PortraitSize * 0.5f - creature.Hitbox.GetGlobalRect().GetCenter();
//             RenderingServer.Singleton.Connect(RenderingServer.SignalName.FramePostDraw, Callable.From(() =>
//             {
//                 if (!GodotObject.IsInstanceValid(viewport)) return;
//                 var image = viewport.GetTexture().GetImage();
//                 if (image != null && !image.IsEmpty())
//                 {
//                     image.Convert(Image.Format.Rgba8);
//                     portrait.Update(image);
//                 }
//                 viewport.QueueFree();
//             }), (uint)GodotObject.ConnectFlags.OneShot);
//         };
//         return viewport;
//     }
// }

/// <summary>拍立得立绘：PhotoFilm_Base.png 当底图，把怪物渲染图拓印进黑区；按 entry 缓存，同一怪物只合成一次。</summary>
internal static class PhotoFilmPhoto
{
    internal const string BasePath = "res://images/cards/PhotoFilm_Base.png";

    // PhotoFilm_Base.png(1000x762) 的黑色相框区域，换底图要同步改
    private static readonly Rect2I PhotoRect = new(36, 38, 929, 577);

    private const string IdleAnim = "idle_loop";

    // 骨骼是异步加载的，等这么久还没好就按没 spine 处理，别把发牌卡死
    private const int SpineReadyMaxFrames = 60;

    // 渲染可能因为节点没进树之类的原因永远完不成；等够帧数就放行，别把读档/出牌一起卡死
    private const int MaxWaitFrames = 600;

    private static readonly Dictionary<string, RenderJob> Jobs = [];

    /// <summary>一次拓印：Composite 一开始是空相框，怪物真正贴进去后 Done 才完成。</summary>
    private sealed class RenderJob(ImageTexture composite)
    {
        public ImageTexture Composite { get; } = composite;

        public TaskCompletionSource Done { get; } = new();

        /// <summary>真贴进底图了；渲染失败时保持 false，让卡面回落 PhotoFilm.png 而不是空相框。</summary>
        public bool Painted { get; set; }
    }

    internal static Texture2D? Get(PhotoFilm film)
    {
        // 没拍到怪物的（图鉴、衍生预览）返回 null 即回落 PortraitPath
        string entry = film.PhotoFilm_MonsterEntry;
        if (entry.Length == 0) return null;

        // 合成完才能返回：ImageTexture.Update 不换引用，PortraitCanvasGroup 不会重建，会永远显示空相框
        var job = Start(entry);
        return job is { Painted: true } && job.Done.Task.IsCompleted ? job.Composite : null;
    }

    /// <summary>先拍照再发牌：确保拓印开始并等怪物真画进底图；拍过则立即返回。</summary>
    internal static Task Capture(ModelId monsterId) => Wait(Start(monsterId.Entry));

    /// <summary>读档一开始就把牌组里所有照片的拓印跑起来，等 LoadRun 结束时它们已经画完。</summary>
    internal static void PrepareRun(RunState runState)
    {
        foreach (var player in runState.Players)
        {
            foreach (var card in player.Deck.Cards)
            {
                if (card is PhotoFilm { HasRealPhoto: true } film)
                {
                    Start(film.PhotoFilm_MonsterEntry);
                }
            }
        }
    }

    /// <summary>读档收尾：等所有拓印画完再放行，否则读档期间建的卡节点会停在回落图/空相框上且不会自己刷新。</summary>
    internal static async Task WaitAll()
    {
        try
        {
            for (int i = 0; i < MaxWaitFrames; i++)
            {
                // 读档过程中还会新建卡节点、起新任务，每帧重新检查
                if (Jobs.Values.All(job => job.Done.Task.IsCompleted)) return;
                if (Engine.GetMainLoop() is not SceneTree tree) return;
                await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"PhotoFilm: 等待拓印失败：{ex}");
        }
    }

    private static async Task Wait(RenderJob? job)
    {
        if (job == null || Engine.GetMainLoop() is not SceneTree tree) return;
        for (int i = 0; i < MaxWaitFrames && !job.Done.Task.IsCompleted; i++)
        {
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }

    private static RenderJob? Start(string entry)
    {
        // 读档期间同一张牌会被反复取立绘（Model setter 和 _Ready 各一次 Reload），重复 Add 会抛异常并打断 NCard.Reload
        if (Jobs.TryGetValue(entry, out var running)) return running;

        // 解析不到 = 对方 mod 已卸载；失败不缓存，ModelDb 还没就绪时下次仍能重试
        if (ModelDb.GetByIdOrNull<MonsterModel>(new ModelId("MONSTER", entry)) is not { } canonical) return null;
        if (GD.Load<Texture2D>(BasePath)?.GetImage() is not { } baseImage) return null;

        if (baseImage.IsCompressed()) baseImage.Decompress();
        baseImage.Convert(Image.Format.Rgba8);
        var job = new RenderJob(ImageTexture.CreateFromImage(baseImage));
        // 先占位再搭视口：搭建期间若被重入取立绘，上面的缓存判断能挡住二次渲染
        Jobs.Add(entry, job);

        // 视口搭建失败不能把读档/卡节点一起带崩，直接按「拍不到」处理
        SubViewport viewport;
        try
        {
            viewport = CreateViewport(canonical, baseImage, job);
        }
        catch (Exception ex)
        {
            Jobs.Remove(entry);
            job.Done.TrySetResult();
            GD.PushWarning($"PhotoFilm: 创建拓印视口失败（{entry}）：{ex}");
            return null;
        }

        ((SceneTree)Engine.GetMainLoop()).Root.CallDeferred(Node.MethodName.AddChild, viewport);
        return job;
    }

    private static SubViewport CreateViewport(MonsterModel canonical, Image baseImage, RenderJob job)
    {
        var monster = canonical.ToMutable();
        monster.Rng = Rng.Chaotic;
        monster.RunRng = new RunRngSet(Rng.Chaotic);
        monster.SetUpForCombat();
        // 只在 TestMode 下返回 null，那时根本不会走到这里
        var creature = NCreature.Create(new Creature(monster, CombatSide.Enemy, null)
        {
            CombatState = NullCombatState.Instance,
        })!;
        var viewport = new SubViewport
        {
            Size = PhotoRect.Size,
            TransparentBg = true,
            // 保持 Once（原能出图的路径）；需要重绘时走 RequestRender 重新触发
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
        };
        viewport.AddChild(creature);
        viewport.Ready += () => TaskHelper.RunSafely(Shoot(viewport, creature, baseImage, job));
        return viewport;
    }

    private static async Task Shoot(SubViewport viewport, NCreature creature, Image baseImage, RenderJob job)
    {
        try
        {
            var tree = viewport.GetTree();
            creature.SetupForBestiary();

            // 进树当帧 Layout 还没算，Hitbox 会是 0 尺寸
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

            var spine = await WaitForSpine(creature, tree);
            if (spine != null)
            {
                // 走兜底包装时 vanilla 的 SetUpSkin 也被跳过了，这里补上
                if (creature.Visuals.SpineBody == null && spine.GetSkeleton() is { } skeleton)
                    creature.Entity.Monster?.SetupSkins(spine, skeleton);
                PoseIdle(spine);
            }

            var bounds = creature.Hitbox.GetGlobalRect();
            var fit = Mathf.Min(PhotoRect.Size.X / Mathf.Max(1f, bounds.Size.X), PhotoRect.Size.Y / Mathf.Max(1f, bounds.Size.Y));
            creature.Scale *= fit * 0.9f;
            creature.Position += (Vector2)PhotoRect.Size * 0.5f - creature.Hitbox.GetGlobalRect().GetCenter();

            // 位置/姿势刚改，要重新触发一次渲染才是新画面
            RequestRender(viewport);
            await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

            var shot = viewport.GetTexture().GetImage();
            if (shot.IsCompressed()) shot.Decompress();
            shot.Convert(Image.Format.Rgba8);
            if (shot.GetSize() != PhotoRect.Size) shot.Resize(PhotoRect.Size.X, PhotoRect.Size.Y);
            baseImage.BlendRect(shot, new Rect2I(Vector2I.Zero, PhotoRect.Size), PhotoRect.Position);
            job.Composite.Update(baseImage);
            job.Painted = true;
        }
        finally
        {
            // 必须放行 Capture 的等待方，否则 OnPlay 会卡住
            viewport.QueueFreeSafely();
            job.Done.TrySetResult();
        }
    }

    // 重复赋相同值会被 Godot 忽略，必须绕过 Disabled 才能再触发一帧
    private static void RequestRender(SubViewport viewport)
    {
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
        viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
    }

    /// <summary>
    /// 取可用的 spine。骨骼异步未就绪时 <see cref="NCreature.Visuals"/> 的 SpineBody 会被置空（连带跳过 animator/skin），
    /// 所以直接包原始 SpineSprite 节点等待。非骨骼立绘没有姿势可调，直接返回 null。
    /// </summary>
    private static async Task<MegaSprite?> WaitForSpine(NCreature creature, SceneTree tree)
    {
        if (creature.Body is not { } body || body.GetClass() != "SpineSprite") return null;

        var spine = creature.Visuals.SpineBody ?? new MegaSprite(body);
        for (int i = 0; i < SpineReadyMaxFrames && !spine.IsAnimationStateReady(); i++)
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);

        return spine;
    }

    /// <summary>强制播 idle 并定位到中段：没有 animator 时骨骼停在 setup pose，有 animator 时也停在循环接缝。</summary>
    private static void PoseIdle(MegaSprite spine)
    {
        if (spine.TryGetAnimationState() is not { } state) return;

        // 实际 DLL 里 SetAnimation 返回 void，只能播完再取 track
        state.SetAnimation(IdleAnim, loop: true);
        if (state.GetCurrent(0) is not { } track) return;

        float end = track.GetAnimationEnd();
        if (end > 0f) track.SetTrackTime(end * 0.5f);

        state.Update(0f);
        if (spine.GetSkeleton() is { } skeleton) state.Apply(skeleton);
    }
}

/// <summary>
/// 读档时先起拓印，整个 LoadRun 跑完后再等拓印画完才放行。
/// 否则读档期间创建的卡节点会拿到回落图/空相框，而 NCard 之后不会自己刷新。
/// </summary>
[HarmonyPatch(typeof(NGame), nameof(NGame.LoadRun))]
internal static class PhotoFilmLoadPatch
{
    [HarmonyPrefix]
    private static void Prefix(RunState runState)
    {
        try
        {
            PhotoFilmPhoto.PrepareRun(runState);
        }
        catch (Exception ex)
        {
            GD.PushWarning($"PhotoFilm: 读档预渲染失败：{ex}");
        }
    }

    // LoadRun 是 async：Postfix 拿到的是 kickoff 返回的 Task，接上读档后的等待
    [HarmonyPostfix]
    private static void Postfix(ref Task __result)
        => __result = HarmonyAsyncTaskBridge.After(__result, PhotoFilmPhoto.WaitAll);
}
