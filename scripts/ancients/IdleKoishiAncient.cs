using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using TouhouAncients.Scripts.relics.IdleKoishi;

namespace TouhouAncients.Scripts;

/// <summary>
/// 先古之民：蔷薇偶像（古明地恋）。首屏只有「为蔷薇偶像应援！」一个选项，
/// 拾起应援棒后同一事件翻页到正常的十选三遗物列表。
/// </summary>
public class IdleKoishiAncient : TouhouAncientBase
{
    private const string AncientEntry = "TOUHOUANCIENTS-IDLE_KOISHI_ANCIENT";

    /// <summary>第二页（遗物选择）的页面文本键。</summary>
    private const string RelicPageKey = AncientEntry + ".pages.IDOL_CHOICE.description";

    private const string SupportOptionKey = AncientEntry + ".pages.INITIAL.options.SUPPORT";

    public override int? ShowAct => 2;

    public override Color ButtonColor => new(0.85f, 0.45f, 0.55f, 0.7f);
    public override Color DialogueColor => new(0.95f, 0.6f, 0.7f, 1f);

    // 暂无专属立绘，暂用地底蔷薇的主唱古明地觉素材占位
    public override string? CustomMapIconPath => "res://images/icon/MapNode/KomejiSatori_MapNode.png";
    public override string? CustomMapIconOutlinePath =>
        "res://images/icon/MapNode/Outline/KomejiSatori_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KomejiSatori.png";
    public override string? CustomRunHistoryIconOutlinePath =>
        "res://images/icon/Character/Outline/KomejiSatori.png";

    /// <summary>十选三：10 件遗物同池抽 3 个选项。</summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        (CreateTARelicOptionPool(
            TARelicOption<BackstageMakeup>(),
            TARelicOption<RoseCage>(),
            TARelicOption<SpecialTicket>(),
            TARelicOption<UnconsciousMicrophone>(),
            TARelicOption<ShowmanHatMan>(),
            TARelicOption<ChireidenStageCostume>(),
            TARelicOption<KoiHeadset>(),
            TARelicOption<KoiEyeSatchel>(),
            TARelicOption<IdolAlbum>(),
            TARelicOption<PortraitOfSomeone>()
        ), 3)
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        // 首屏只有应援选项，遗物列表在拾起应援棒后翻页给出
        return [CreateSupportOption()];
    }

    /// <summary>「为蔷薇偶像应援！」：拾起应援棒后翻到遗物选择页。</summary>
    private EventOption CreateSupportOption()
    {
        return new EventOption(
            this,
            Support,
            new LocString("ancients", SupportOptionKey + ".title"),
            new LocString("ancients", SupportOptionKey + ".description"),
            SupportOptionKey,
            HoverTipFactory.FromRelic<SupportBaton>());
    }

    private async Task Support()
    {
        var player = Owner;
        if (player == null) return;

        await RelicCmd.Obtain(ModelDb.Relic<SupportBaton>().ToMutable(), player);

        SetEventState(new LocString("ancients", RelicPageKey), GenerateRelicOptions());
    }

    /// <summary>按遗物池生成十选三选项（过滤条件与基类一致，抽签用事件 Rng）。</summary>
    private IReadOnlyList<EventOption> GenerateRelicOptions()
    {
        var options = new List<EventOption>();
        var taken = new HashSet<ModelId>();

        foreach (var (pool, count) in TARelicOptionPools)
        {
            var available = new List<TARelicOption>();
            foreach (var candidate in pool.Options)
            {
                if (candidate.Weight <= 0) continue;
                if (candidate.Relic is RelicModel relic && taken.Contains(relic.Id)) continue;
                if (candidate.Relic is TouhouAncientRelics ancientRelic && !ancientRelic.CanAppear(Owner)) continue;
                available.Add(candidate);
            }

            for (var i = 0; i < count && available.Count > 0; i++)
            {
                var chosen = available[base.Rng.NextInt(available.Count)];
                if (chosen.Relic is RelicModel picked) taken.Add(picked.Id);
                available.Remove(chosen);
                options.Add(chosen);
            }
        }

        return options;
    }
}
