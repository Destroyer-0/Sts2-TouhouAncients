using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cardTags;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 墨染的樱花：每回合获得1费并失去1最大生命。你可以在休息处赏樱。
/// 赏樱：恢复因为此遗物失去的生命上限。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class InkDyedCherryBlossoms : TouhouAncientRelics
{
    private int _lostMaxHpTotal;

    public override bool ShowCounter => LostMaxHpTotal > 0;

    public override int DisplayAmount => LostMaxHpTotal;

    /// <summary>
    /// 累计因本遗物失去的最大生命值（持久化保存）
    /// </summary>
    [SavedProperty]
    public int LostMaxHpTotal
    {
        get => _lostMaxHpTotal;
        set
        {
            AssertMutable();
            _lostMaxHpTotal = value;
            InvokeDisplayAmountChanged();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.ForEnergy(this), HoverTipFactory.FromKeyword(TouhouAncientKeywords.TouhouAncientCherryBlossoms)
    ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (player != base.Owner) return amount;
        return amount + (decimal)base.DynamicVars.Energy.IntValue;
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side != base.Owner.Creature.Side) return;
        // 每回合失去1最大生命
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), base.Owner.Creature, 1m, isFromCard: false);
        LostMaxHpTotal++;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner) return false;
        options.Add(new InkDyedCherryBlossomsRestSiteOption(player, this));
        return true;
    }

    /// <summary>
    /// 赏樱：恢复因为此遗物失去的生命上限
    /// </summary>
    public async Task RestoreLostMaxHp()
    {
        if (LostMaxHpTotal > 0)
        {
            await CreatureCmd.GainMaxHp(base.Owner.Creature, LostMaxHpTotal);
            LostMaxHpTotal = 0;
        }
    }

    public override Task AfterCombatEnd(CombatRoom _)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// 赏樱休息处选项
/// </summary>
public class InkDyedCherryBlossomsRestSiteOption : RestSiteOption
{
    private readonly InkDyedCherryBlossoms _relic;

    public override string OptionId => "INK_DYED_CHERRY_BLOSSOMS";

    public override LocString Description
    {
        get
        {
            if (base.IsEnabled && _relic.LostMaxHpTotal > 0)
            {
                LocString locString = new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");
                locString.Add("MaxHpGain", _relic.LostMaxHpTotal);
                return locString;
            }

            return new LocString("rest_site_ui", "OPTION_" + OptionId + ".descriptionDisabled");
        }
    }

    public InkDyedCherryBlossomsRestSiteOption(Player owner, InkDyedCherryBlossoms relic) : base(owner)
    {
        _relic = relic;
    }

    public override async Task<bool> OnSelect()
    {
        if (_relic != null)
        {
            await _relic.RestoreLostMaxHp();
            return true;
        }

        return false;
    }
}