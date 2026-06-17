using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Rewards;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts;

public class HouraisanKaguyaAncient : CustomAncientModel
{
    public override Color ButtonColor => new(0.1f, 0.1f, 0.1f, 0.7f);
    public override Color DialogueColor => new(0.9f, 0.3f, 0.5f, 1f);

    public override string? CustomMapIconPath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomMapIconOutlinePath => "res://images/icon/MapNode/WatariNina_MapNode.png";
    public override string? CustomRunHistoryIconPath => "res://images/icon/Character/HouraisanKaguya.png";
    public override string? CustomRunHistoryIconOutlinePath => "res://images/icon/Character/Outline/HouraisanKaguya.png";

    public override bool IsValidForAct(ActModel act)
    {
        return act.ActNumber() == 3;
    }

    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced<HouraisanKaguyaAncient>(act.ActNumber());
    }

    protected override OptionPools MakeOptionPools => new OptionPools(
        MakePool(
            AncientOption<KonshiiNoKusuri>(),
            AncientOption<RyukeiNoTama>(),
            AncientOption<HinezumiNoKawagoromo>(),
            AncientOption<TsubameNoKoyasugai>(),
            AncientOption<HotokeMishiIshiNoHachi>(),
            AncientOption<HouraiNoTamae>(),
            AncientOption<EienteiZakushi>(),
            AncientOption<KaguyaSecretTreasure>()
        ));

    public override IEnumerable<EventOption> AllPossibleOptions => BaseOptionPool;

    private IEnumerable<EventOption> BaseOptionPool =>
    [
        RelicOption<KonshiiNoKusuri>(),
        RelicOption<RyukeiNoTama>(),
        RelicOption<HinezumiNoKawagoromo>(),
        RelicOption<TsubameNoKoyasugai>(),
        RelicOption<HotokeMishiIshiNoHachi>().ThatDecreasesMaxHp(30),
        RelicOption<HouraiNoTamae>(),
        RelicOption<EienteiZakushi>(),
        RelicOption<KaguyaSecretTreasure>()
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var current = base.GenerateInitialOptions().ToList();
        var textKey = "PUNCH_OFF.pages.INITIAL.options.I_CAN_TAKE_THEM";
        current.Add(new EventOption(this, TakeThem,
            L10NLookup(textKey + ".title"),
            L10NLookup(textKey + ".description"),
            textKey,
            Enumerable.Empty<IHoverTip>()));
        return current;
    }

    private Task TakeThem()
    {
        var fightKey = "PUNCH_OFF.pages.I_CAN_TAKE_THEM.options.FIGHT";
        var fightOption = new EventOption(this, Fight,
            L10NLookup(fightKey + ".title"),
            L10NLookup(fightKey + ".description"),
            fightKey,
            Enumerable.Empty<IHoverTip>());
        SetEventState(L10NLookup("PUNCH_OFF.pages.I_CAN_TAKE_THEM.description"), new List<EventOption> { fightOption });
        return Task.CompletedTask;
    }

    private Task Fight()
    {
        base.Owner.CanRemovePotions = true;
        EnterCombatWithoutExitingEvent<PunchOffEventEncounter>(new List<Reward>(new Reward[2]
        {
            new RelicReward(base.Owner),
            new PotionReward(base.Owner)
        }), shouldResumeAfterCombat: false);
        return Task.CompletedTask;
    }
}