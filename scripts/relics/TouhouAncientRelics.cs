using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public abstract class TouhouAncientRelics : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;
    // 小图标（原版85x85）
    public override string PackedIconPath => $"res://icon/relics/{GetType().Name.ToLowerInvariant()}.png";
    // 轮廓图标（原版85x85）
    protected override string PackedIconOutlinePath => $"res://icon/relics/{GetType().Name.ToLowerInvariant()}.png";
    // 大图标（原版256x256）
    protected override string BigIconPath => $"res://icon/relics/{GetType().Name.ToLowerInvariant()}.png";
}