using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

namespace TouhouAncients.Scripts.Enchantment;

public abstract class TouhouAncientEnchantmentModel : CustomEnchantmentModel
{
    public virtual bool CanBeRandomSelected => true;

    /// <summary>
    /// 随机附魔的层数：默认按附魔自身有没有「数值」决定——<see cref="EnchantmentModel.ShowAmount"/> 为 true 的附魔
    /// （原版：锋利、灵巧、伶俐、动量、迅速、活力；本 Mod：蛊毒、付丧之力）取 3 层，
    /// 为 false 的附魔没有数值概念（原版：播种、墨影、腐化、注能、黏糊等；本 Mod：奇迹、喋血、优质等）取 1 层。
    /// 播种、迅速、活力再按设计单独取值。
    /// 「梦色」（<see cref="Yumeiro"/>）与遗物「无穷无尽的愤怒」共用这份特判，两边必须一致。
    /// </summary>
    /// <param name="enchantment">随机抽中的附魔。</param>
    /// <returns>该附魔要附上的层数。</returns>
    public static decimal GetRandomEnchantAmount(EnchantmentModel enchantment)
    {
        return enchantment switch
        {
            // 播种：特判 1 层。
            Sown => 1m,
            // 迅速：特判 2 层。
            Swift => 2m,
            // 活力：特判 6 层。
            Vigorous => 6m,
            // 其余按有没有「数值」区分：没有数值概念的附魔附 3 层没有意义，取 1 层。
            _ => enchantment.ShowAmount ? 3m : 1m,
        };
    }

    protected override string? CustomIconPath => TouhouAncientCmd.CheckPathExists($"res://images/icon/enchantment/{GetType().Name}.png");
}