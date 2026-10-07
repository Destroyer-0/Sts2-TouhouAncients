using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using TouhouAncients.Scripts;

//namespace TouhouAncients.Scripts.relics;

public abstract class TouhouAncientRelics : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public virtual string DefaultFileName => "default";

    /// <summary>先古之民选项条件：返回 false 时本遗物不会被抽为选项。由 TouhouAncientBase 在抽取前统一判定。</summary>
    public virtual bool CanAppear(Player? player) => true;

    /// <summary>
    /// 注册卡牌奖励的替代选项。原版 <c>CardRewardAlternative.Generate</c> 最多只接受 2 个（跳过恒占 1 个），
    /// 超了会抛异常、奖励永远领不掉，所以后来者挤掉先来者（同类遗物也互相挤）。
    /// </summary>
    protected static void AddCardRewardAlternative(List<CardRewardAlternative> alternatives, CardRewardAlternative alternative)
    {
        if (alternatives.Count > 1) alternatives.RemoveAt(alternatives.Count - 1);
        alternatives.Add(alternative);
    }

    // 小图标（原版85x85）
    public override string PackedIconPath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/icon/relics/{GetType().Name}.png",
        $"res://images/icon/relics/{DefaultFileName}.png");

    // 轮廓图标（原版85x85）
    protected override string PackedIconOutlinePath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/icon/relics/{GetType().Name}.png",
        $"res://images/icon/relics/{DefaultFileName}.png");

    // 大图标（原版256x256）
    protected override string BigIconPath => TouhouAncientCmd.CheckPathExistsWithFallback(
        $"res://images/icon/relics/IconLarge/{GetType().Name}.png",
        TouhouAncientCmd.CheckPathExistsWithFallback(
            $"res://images/icon/relics/{GetType().Name}.png",
            $"res://images/icon/relics/IconLarge/{DefaultFileName}.png"));
}