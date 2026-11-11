using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class KomejiSatoriAncient : TouhouAncientBase
{
    public override int? ShowAct => 2;
    public override Color ButtonColor => new(0.75f, 0.25f, 0.55f, 0.7f);
    public override Color DialogueColor => new(0.85f, 0.35f, 0.75f, 1f);


    public override string? CustomMapIconPath => "res://images/icon/MapNode/KomejiSatori_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/Outline/KomejiSatori_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/KomejiSatori.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/KomejiSatori.png";

    /// <summary>恋 mod 的角色 Entry。</summary>
    private const string KoishiCharacterEntry = "KOISHI-KOISHI";

    /// <summary>恋 mod 的「我们」：恋玩家第一行用它顶替第三只眼。</summary>
    private const string KoishiUsEntry = "KOISHI-US";

    private bool IsKoishiPlayer =>
        Owner != null &&
        string.Equals(Owner.Character.Id.Entry, KoishiCharacterEntry, StringComparison.OrdinalIgnoreCase);

    /// <summary>本 Ancient 的选项（四行 = 四个选项）。</summary>
    protected override IReadOnlyList<TARelicOptionGroup> TARelicOptionPools =>
    [
        CreateTARelicOptionPool(FirstRowOption),
        CreateTARelicOptionPool(
            TARelicOption<HellOrin>(),
            TARelicOption<HellOkuu>(),
            TARelicOption<DustyRose>().Prep((DustyRose relic) =>
            {
                if (Owner != null) relic.SetupForPlayer(Owner);
            })
            ),
        CreateTARelicOptionPool(
            TARelicOption<MindProbe>(),
            TARelicOption<DetectiveStory>(),
            TARelicOption<BitterCoffee>()
            ),
        CreateTARelicOptionPool(
            TARelicOption<OblivionFragment>(),
            TARelicOption<BrainInAVat>(),
            TARelicOption<MemoryFlask>()
            )
    ];

    /// <summary>第一行：恋玩家给恋 mod 的「我们」；模型取不到（恋 mod 未安装）退回第三只眼，避免这一行空掉。</summary>
    private TARelicOption FirstRowOption =>
        IsKoishiPlayer && FindRelic(KoishiUsEntry) is { } koishiUs
            ? new TARelicOption(RelicOption(koishiUs.ToMutable()))
            : TARelicOption<TheThirdEye>();

    /// <summary>按 Entry 跨 mod 找遗物（不引对方 DLL）；先直查再扫类型，Category/Entry 大小写不一定与本地化键一致。</summary>
    private RelicModel? FindRelic(string entry)
    {
        if (ModelDb.GetByIdOrNull<AbstractModel>(new ModelId("RELIC", entry)) is RelicModel direct)
        {
            return direct;
        }

        foreach (Type type in ModelDb.AllAbstractModelSubtypes)
        {
            if (!type.IsSubclassOf(typeof(RelicModel))) continue;

            ModelId id = ModelDb.GetId(type);
            if (!string.Equals(id.Entry, entry, StringComparison.OrdinalIgnoreCase)) continue;

            if (ModelDb.GetByIdOrNull<AbstractModel>(id) is RelicModel found) return found;
        }

        return null;
    }
}