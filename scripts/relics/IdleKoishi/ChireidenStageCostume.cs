using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 地灵打歌服：嗨到爆下第 1 张攻击/技能牌额外打出一次；本场战斗每次进入嗨到爆，
/// 可触发的张数 +1（首次 1 张，第二次 2 张……）。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class ChireidenStageCostume : TouhouAncientRelics, IHypedEnteredListener
{
    private const int BasePlays = 1;
    private const int ExtraPlays = 1;

    private int _entriesBeforeThisHyped;
    private int _triggeredThisHyped;
    private int _limitThisHyped = BasePlays;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("BasePlays", BasePlays),
        new DynamicVar("ExtraPlays", ExtraPlays),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => KoishiPerformanceHoverTips;

    public override Task BeforeCombatStart()
    {
        _entriesBeforeThisHyped = 0;
        _triggeredThisHyped = 0;
        _limitThisHyped = BasePlays;
        return Task.CompletedTask;
    }

    public Task AfterHypedEntered(PlayerChoiceContext choiceContext)
    {
        _limitThisHyped = BasePlays + _entriesBeforeThisHyped;
        _entriesBeforeThisHyped++;
        _triggeredThisHyped = 0;
        return Task.CompletedTask;
    }

    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        var player = base.Owner;
        if (player == null || card.Owner != player) return playCount;
        if (card.Type is not (CardType.Attack or CardType.Skill)) return playCount;
        if (_triggeredThisHyped >= _limitThisHyped) return playCount;
        if (!KoishiPerformanceCmd.IsHyped(player)) return playCount;

        return playCount + ExtraPlays;
    }

    public override Task AfterModifyingCardPlayCount(CardModel card)
    {
        if (base.Owner == null || card.Owner != base.Owner) return Task.CompletedTask;
        if (_triggeredThisHyped >= _limitThisHyped) return Task.CompletedTask;

        _triggeredThisHyped++;
        Flash();
        return Task.CompletedTask;
    }
}
