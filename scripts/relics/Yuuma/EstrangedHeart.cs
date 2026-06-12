using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 陌路之心：在你的回合开始时（在抽牌阶段开始前），选择任意张抽牌堆的牌加入手牌。
/// 累计使用此方式获得30张牌后，此遗物在本场战斗结束后失效（永久标记为已用尽）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class EstrangedHeart : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    private const int Threshold = 30;

    private int cardsTakenThisCombat;
    private bool hitThresholdThisCombat;

    [SavedProperty]
    public bool TouhouAncients_IsPermanentlyUsedUp
    {
        get => isPermanentlyUsedUp;
        set
        {
            AssertMutable();
            isPermanentlyUsedUp = value;
            InvokeDisplayAmountChanged();
        }
    }

    private bool isPermanentlyUsedUp;

    public override bool HasUponPickupEffect => false;
    public override bool IsUsedUp => TouhouAncients_IsPermanentlyUsedUp;
    
    public override bool ShowCounter => !IsUsedUp;
    
    public override int DisplayAmount => TouhouAncients_IsPermanentlyUsedUp ? 0 : Threshold - cardsTakenThisCombat;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Threshold", Threshold),
    ];

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        if (IsUsedUp) return;
        if (player.Creature.CombatState == null) return;

        var drawPile = PileType.Draw.GetPile(player);
        if (drawPile.IsEmpty) return;

        var drawCards = drawPile.Cards.ToList();
        if (drawCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(base.SelectionScreenPrompt, 0, drawCards.Count);
        var selected = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            drawCards,
            player,
            prefs)).ToList();

        if (selected.Count == 0) return;

        Flash();

        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand);
            cardsTakenThisCombat++;
        }

        // 检查是否达到阈值
        if (cardsTakenThisCombat >= Threshold && !hitThresholdThisCombat)
        {
            hitThresholdThisCombat = true;
            InvokeDisplayAmountChanged();
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (hitThresholdThisCombat)
        {
            TouhouAncients_IsPermanentlyUsedUp = true;
        }
        cardsTakenThisCombat = 0;
        hitThresholdThisCombat = false;
        return Task.CompletedTask;
    }
}
