using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;
using TouhouAncients.Scripts.Vfx;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 极奢形态 (Richest Form)
/// </summary>
[Pool(typeof(EventCardPool))]
public class RichestForm : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Eternal
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new EnergyVar(1),
        new EnergyVar("Energy2", 5),
        new EnergyVar("Energy3", 5),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        base.EnergyHoverTip
    ];

    public RichestForm() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var creature = base.Owner.Creature;
        Owner.PlayerCombatState.GainEnergy(base.DynamicVars["Energy2"].IntValue);
        var power = await PowerCmd.Apply<RichestFormPower>(choiceContext, creature, DynamicVars["Energy3"].IntValue, creature, this);
        if (power != null)
        {
            power.ExtraCost += DynamicVars.Energy.IntValue;
        }

        // 玩家身后播放女苑财富字符特效（1 秒）
        NCreature? playerNode = NCombatRoom.Instance?.GetCreatureNode(creature);
        Control? backVfxContainer = NCombatRoom.Instance?.BackCombatVfxContainer;
        if (playerNode != null && backVfxContainer != null)
        {
            NJoonWealthBurstVfx? vfx = NJoonWealthBurstVfx.Create(playerNode.VfxSpawnPosition + new Vector2(0f, -60f));
            if (vfx != null)
            {
                backVfxContainer.AddChildSafely(vfx);
                vfx.PlayForSeconds(1f);
            }
        }
    }
    
    protected override void OnUpgrade()
    {
        base.DynamicVars["Energy2"].UpgradeValueBy(1m);
        base.DynamicVars["Energy3"].UpgradeValueBy(1m);
    }
}