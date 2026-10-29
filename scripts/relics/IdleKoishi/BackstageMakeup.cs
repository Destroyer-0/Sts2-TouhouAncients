using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 台前妆后：拾起与每次休息获得 1 计数；每累计 5 气氛随机 +1 力量/敏捷，
/// 进入嗨到爆时额外触发 4 次；力量与敏捷各自最多获得「计数」次。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class BackstageMakeup : TouhouAncientRelics, IAtmosphereGainedListener, IHypedEnteredListener
{
    private const int AtmosphereThreshold = 5;
    private const int HypedTriggers = 4;
    private const int StatAmount = 1;

    private int _counts;
    private int _atmosphereProgress;
    private int _strengthGained;
    private int _dexterityGained;

    [SavedProperty]
    public int Counting
    {
        get => _counts;
        set
        {
            AssertMutable();
            _counts = value;
            base.DynamicVars["Counts"].BaseValue = value;
        }
    }

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Counts", 1m),
        new DynamicVar("AtmosphereThreshold", AtmosphereThreshold),
        new DynamicVar("HypedTriggers", HypedTriggers),
        new DynamicVar("StatAmount", StatAmount),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>(),
        .. KoishiPerformanceHoverTips,
    ];

    public override Task AfterObtained()
    {
        Counting += 1;
        return Task.CompletedTask;
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (player != base.Owner) return Task.CompletedTask;
        Counting += 1;
        Flash();
        return Task.CompletedTask;
    }

    public async Task AfterAtmosphereGained(PlayerChoiceContext choiceContext, int amount)
    {
        _atmosphereProgress += amount;
        while (_atmosphereProgress >= AtmosphereThreshold)
        {
            _atmosphereProgress -= AtmosphereThreshold;
            await GrantRandomStat();
        }
    }

    public async Task AfterHypedEntered(PlayerChoiceContext choiceContext)
    {
        for (var i = 0; i < HypedTriggers; i++)
        {
            await GrantRandomStat();
        }
    }

    public override Task BeforeCombatStart()
    {
        _atmosphereProgress = 0;
        _strengthGained = 0;
        _dexterityGained = 0;
        return Task.CompletedTask;
    }

    /// <summary>随机 +1 力量或敏捷，受「计数」上限约束。</summary>
    private async Task GrantRandomStat()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        var canStrength = _strengthGained < _counts;
        var canDexterity = _dexterityGained < _counts;
        if (!canStrength && !canDexterity) return;

        var rng = player.RunState.Rng.CombatCardGeneration;
        var useStrength = canStrength && (!canDexterity || rng.NextBool());

        if (useStrength)
        {
            _strengthGained++;
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), player.Creature,
                StatAmount, player.Creature, null);
        }
        else
        {
            _dexterityGained++;
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), player.Creature,
                StatAmount, player.Creature, null);
        }

        Flash();
    }
}
