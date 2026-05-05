using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Relics;

//namespace TouhouAncients.Scripts.relics;

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