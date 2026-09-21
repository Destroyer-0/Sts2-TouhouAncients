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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
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

    /// <summary>选项条件：至少有一个可吞噬的遗物（既非初始、也非先古），否则不出现。</summary>
    public override bool CanAppear(Player? player)
    {
        if (player?.Creature == null) return false;

        // 统一用 Rarity 判断，不列举具体遗物：
        // - 初始遗物（Rarity 为 Starter）：包含「欧洛巴斯之触」等升级后的版本，
        //   它们已不在 Character.StartingRelics 列表里，但 Rarity 仍是 Starter；
        // - 先古遗物（Rarity 为 Ancient）：覆盖所有先古事件的选项遗物，
        //   不必再遍历 ModelDb.AllAncients 去收集 AllPossibleOptions。
        return player.Relics.Any(r => r.Rarity != RelicRarity.Starter && r.Rarity != RelicRarity.Ancient);
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new MaxHpVar(7),
        new DynamicVar("Strength", 1m),
        new DynamicVar("Dexterity", 1m),
        new DynamicVar("Focus", 1m),
        new EnergyVar(1),
        new DynamicVar("CardsPerTurn", 1),
        new DynamicVar("StrengthTrigger", 4),
        new DynamicVar("EnergyTrigger", 6),
        new DynamicVar("DrawTrigger", 8),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        HoverTipFactory.FromPower<FocusPower>(),
        HoverTipFactory.ForEnergy(this),
    ];

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 统一用 Rarity 判断（同 CanAppear）：
        // - 初始遗物 Rarity 为 Starter，含「欧洛巴斯之触」等升级后的版本；
        // - 先古遗物 Rarity 为 Ancient，本遗物自身也是 Ancient 稀有度，因此已自动排除。
        var toConsume = player.Relics
            .Where(r => r.Rarity != RelicRarity.Starter && r.Rarity != RelicRarity.Ancient)
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

        Grow();

        // 每8个：+1 能量上限（通过 ModifyMaxEnergy 已在下面处理）
        // 每12个：+1 每回合抽牌
    }

    private void Grow()
    {
        NCombatRoom.Instance?.GetCreatureNode(base.Owner.Creature)
            ?.ScaleTo(1 + TouhouAncients_ConsumedCount * 0.1f, 0f);
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner) return amount;
        int energyBonus = TouhouAncients_ConsumedCount / DynamicVars["EnergyTrigger"].IntValue;
        return amount + energyBonus * DynamicVars.Energy.IntValue;
    }

    public override decimal ModifyHandDraw(Player player, decimal count)
    {
        int drawBonus = TouhouAncients_ConsumedCount / DynamicVars["DrawTrigger"].IntValue;
        if (drawBonus > 0)
        {
            Flash();
            return count + drawBonus;
        }

        return count;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is CombatRoom)
        {
            // 每4个：+1 力量、+1 敏捷、+1 集中
            int strDexCount = TouhouAncients_ConsumedCount / DynamicVars["StrengthTrigger"].IntValue;
            if (strDexCount > 0)
            {
                Flash();
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
                    strDexCount * DynamicVars["Strength"].BaseValue, Owner.Creature, null);
                await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
                    strDexCount * DynamicVars["Dexterity"].BaseValue, Owner.Creature, null);
                await PowerCmd.Apply<FocusPower>(new ThrowingPlayerChoiceContext(), Owner.Creature,
                    strDexCount * DynamicVars["Focus"].BaseValue, Owner.Creature, null);
            }
        }

        Grow();
    }
}