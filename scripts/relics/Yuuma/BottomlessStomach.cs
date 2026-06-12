using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 无底之胃：吞噬你初始遗物以外的全部遗物，每个为你提供8最大生命，
/// 每4个提供1力量、1敏捷，每8个提供1能量上限，每12个提供每回合额外抽一。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BottomlessStomach : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    [SavedProperty]
    public int TouhouAncients_ConsumedCount
    {
        get => consumedCount;
        set
        {
            AssertMutable();
            consumedCount = value;
            InvokeDisplayAmountChanged();
        }
    }

    private int consumedCount;

    public override bool HasUponPickupEffect => true;
    public override bool ShowCounter => true;
    public override int DisplayAmount => TouhouAncients_ConsumedCount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(8),
        new DynamicVar("Strength", 1m),
        new DynamicVar("Dexterity", 1m),
        new EnergyVar(1),
        new DynamicVar("CardsPerTurn", 1),
        new DynamicVar("StrengthTrigger", 4),
        new DynamicVar("EnergyTrigger", 8),
        new DynamicVar("DrawTrigger", 12),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.ForEnergy(this),
    ];

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 获取初始遗物列表
        var startingRelicIds = player.Character.StartingRelics.Select(r => r.Id).ToHashSet();

        // 收集要吞噬的遗物（排除初始遗物和自身）
        var toConsume = player.Relics
            .Where(r => !startingRelicIds.Contains(r.Id) && r != this)
            .ToList();

        int count = toConsume.Count;
        if (count == 0) return;

        // 移除遗物
        foreach (var relic in toConsume)
        {
            await RelicCmd.Remove(relic);
        }

        TouhouAncients_ConsumedCount = count;
        Flash();

        // 每1个：+8 最大生命
        await CreatureCmd.GainMaxHp(player.Creature, count * DynamicVars["MaxHp"].IntValue);

        // 每4个：+1 力量、+1 敏捷
        int strDexCount = count / 4;
        if (strDexCount > 0)
        {
            await PowerCmd.Apply<StrengthPower>(player.Creature, strDexCount * DynamicVars["Strength"].BaseValue, player.Creature, null);
            await PowerCmd.Apply<DexterityPower>(player.Creature, strDexCount * DynamicVars["Dexterity"].BaseValue, player.Creature, null);
        }

        // 每8个：+1 能量上限（通过 ModifyMaxEnergy 已在下面处理）
        // 每12个：+1 每回合抽牌
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner) return amount;
        int energyBonus = TouhouAncients_ConsumedCount / 8;
        return amount + energyBonus * DynamicVars.Energy.IntValue;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        int drawBonus = TouhouAncients_ConsumedCount / 12;
        if (drawBonus > 0)
        {
            Flash();
            await CardPileCmd.Draw(choiceContext, drawBonus * DynamicVars["CardsPerTurn"].IntValue, player);
        }
    }
}
