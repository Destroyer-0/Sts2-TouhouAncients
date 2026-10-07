using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.badges;

/// <summary>同时持有真假异蛇之眼与异眼顶真的蛇瞳。</summary>
public class ThreeSnake : TouhouAncientBadge
{
    public ThreeSnake() : base(requiresWin: false, multiplayerOnly: false)
    {
    }

    public override BadgeRarity Rarity(SerializableRun run, SerializablePlayer player) => BadgeRarity.Gold;

    public override bool IsObtained(SerializableRun run, SerializablePlayer player) =>
        HasRelic<SneckoEye>(player) &&
        HasRelic<FakeSneckoEye>(player) &&
        HasRelic<Yiyandingzhen>(player);
}
