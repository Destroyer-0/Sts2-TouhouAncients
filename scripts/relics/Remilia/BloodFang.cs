using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.Enchantment;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(EventRelicPool))]
public class BloodFang : TouhouAncientRelics
{
    /// <summary>失去量占最大生命的比例（33%）。</summary>
    private const decimal LossRatio = 0.33m;

    protected override IEnumerable<DynamicVar> CanonicalVars => 
        [
            new DynamicVar("LoseHp", 0),
            new StringVar("EnchantmentName", ModelDb.Enchantment<Bloodshed>().Title.GetFormattedText())
        ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromEnchantment<Bloodshed>();

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 失去的最大生命按「最大生命 × 33%」向下取整，最少 1 点。
    /// </summary>
    private static decimal GetLossAmount(Player player) =>
        Math.Max(1m, Math.Floor(player.Creature.MaxHp * LossRatio));

    /// <summary>
    /// 最大生命为 1 时不出现（此时至少失去 1 点最大生命会直接致死）。
    /// </summary>
    public override bool CanAppear(Player? player) => player == null || player.Creature.MaxHp > 1;

    /// <summary>
    /// 在「事件选项生成」阶段把要失去的最大生命写进描述变量，
    /// 由 <c>RemiliaScarletAncient</c> 的选项候选（<c>TARelicOption&lt;BloodFang&gt;().Prep(...)</c>）调用。
    /// 此阶段遗物自身的 Owner 尚未设置，所以必须由调用方把玩家传进来。
    /// </summary>
    /// <param name="player">正在生成遗物选项的玩家。</param>
    public void SetupForPlayer(Player player)
    {
        base.DynamicVars["LoseHp"].BaseValue = GetLossAmount(player);
    }

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
        Bloodshed bloodshed = ModelDb.Enchantment<Bloodshed>();
        var enchantSuccess = false;
        foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(base.Owner, bloodshed, 1, prefs))
        {
            CardCmd.Enchant(bloodshed.ToMutable(), item, 1m);
            CardCmd.Preview(item);
            enchantSuccess = true;
        }

        if (enchantSuccess)
        {
            // 拾起时按当前最大生命重新计算，失去其中的 33%（向下取整，最少 1 点）
            var loseHp = GetLossAmount(player);
            base.DynamicVars["LoseHp"].BaseValue = loseHp;
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), player.Creature, loseHp, isFromCard: false);
        }
    }
}