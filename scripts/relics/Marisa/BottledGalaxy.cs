using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 微缩银河：拾起时，从你的牌组中选择3张牌移除。在每场战斗的前三回合，回合开始时随机将其中的一张牌放置在牌堆顶，并使其耗能在下次打出前减少当前回合数。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class BottledGalaxy : TouhouAncientRelics
{
    private const string _cardTitlesKey = "CardTitles";

    private List<SerializableCard> _serializableCards = new();

    public override bool HasUponPickupEffect => true;

    public override bool ShowCounter
    {
        get
        {
            if (base.IsMutable)
            {
                return _serializableCards.Count > 0;
            }
            return false;
        }
    }

    public override int DisplayAmount
    {
        get
        {
            if (!base.IsMutable)
            {
                return 0;
            }
            return _serializableCards.Count;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(3),
        new StringVar(_cardTitlesKey)
    ];

    [SavedProperty]
    public List<SerializableCard> SerializableCards
    {
        get => _serializableCards;
        private set
        {
            AssertMutable();
            _serializableCards.Clear();
            _serializableCards.AddRange(value);
            UpdateCardList();
        }
    }

    private HashSet<SerializableCard> combatCardsList = new();
    
    protected override void AfterCloned()
    {
        base.AfterCloned();
        _serializableCards = new List<SerializableCard>();
    }

    public override async Task AfterObtained()
    {
        var selected = (await CardSelectCmd.FromDeckForRemoval(
                prefs: new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, base.DynamicVars.Cards.IntValue),
                player: base.Owner,
                filter: null))
            .OrderBy(c => c.Id.Entry, StringComparer.Ordinal);

        foreach (var card in selected)
        {
            var cloned = (CardModel)card.MutableClone();
            SerializableCards.Add(cloned.ToSerializable());
            await CardPileCmd.RemoveFromDeck(card);
        }

        UpdateCardList();
    }

    public override Task BeforeCombatStart()
    {
        combatCardsList = new HashSet<SerializableCard>(_serializableCards);
        return base.BeforeCombatStart();
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != base.Owner) return;
        if (base.Owner.Creature?.CombatState == null) return;
        if (!CombatManager.Instance.IsInProgress) return;
        if (combatCardsList.Count == 0) return;

        var roundNumber = combatState.RoundNumber;
        if (roundNumber > 3) return;

        Flash();
        await Cmd.CustomScaledWait(0.1f, 1f);

        var serializableCard = combatState.RunState.Rng.CombatCardSelection.NextItem(combatCardsList);
        if (serializableCard == null) return;

        var cardModel = CardModel.FromSerializable(serializableCard);
        if (!combatState.ContainsCard(cardModel))
        {
            combatState.AddCard(cardModel, base.Owner);
        }

        cardModel.EnergyCost.AddUntilPlayed(-roundNumber);

        await CardPileCmd.Add(cardModel, PileType.Draw, CardPilePosition.Top);
        combatCardsList.Remove(serializableCard);
    }

    private void UpdateCardList()
    {
        base.Status = _serializableCards.Count <= 0 ? RelicStatus.Disabled : RelicStatus.Normal;
        var stringVar = (StringVar)base.DynamicVars[_cardTitlesKey];
        if (_serializableCards.Count == 0)
        {
            stringVar.StringValue = string.Empty;
        }
        else
        {
            stringVar.StringValue = string.Join('\n', _serializableCards.Select(c => "- " + SaveUtil.CardOrDeprecated(c.Id!).Title));
        }

        InvokeDisplayAmountChanged();
    }

    // private static readonly ConditionalWeakTable<CardModel, BottledGalaxyCardData> CardData = new();
    //
    // protected override IEnumerable<DynamicVar> CanonicalVars => [];
    //
    // public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    // {
    //     if (potion.Owner != base.Owner) return Task.CompletedTask;
    //     if (base.Owner.PlayerCombatState == null) return Task.CompletedTask;
    //     if (!CombatManager.Instance.IsInProgress) return Task.CompletedTask;
    //
    //     var candidates = base.Owner.PlayerCombatState.Hand.Cards
    //         .Where(IsEligibleCard)
    //         .ToList();
    //
    //     if (candidates.Count == 0) return Task.CompletedTask;
    //
    //     var card = candidates
    //         .UnstableShuffle(base.Owner.RunState.Rng.CombatCardSelection)
    //         .First();
    //
    //     CardData.GetValue(card, _ => new BottledGalaxyCardData())
    //         .PotionIds.Add(potion.Id.Entry);
    //
    //     Flash();
    //     RefreshCardText(card);
    //     CardCmd.Preview(card,0.75f);
    //     return Task.CompletedTask;
    // }
    //
    // public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    // {
    //     if (cardPlay.Card.Owner != base.Owner) return;
    //     if (!CardData.TryGetValue(cardPlay.Card, out var data)) return;
    //     if (data.PotionIds.Count == 0) return;
    //
    //     var potionIds = data.PotionIds.ToList();
    //     data.PotionIds.Clear();
    //     CardData.Remove(cardPlay.Card);
    //
    //     Flash();
    //     RefreshCardText(cardPlay.Card);
    //
    //     foreach (var potionId in potionIds)
    //     {
    //         await ApplyPotionEffect(choiceContext, potionId, cardPlay);
    //     }
    // }
    //
    // private static bool IsEligibleCard(CardModel card)
    // {
    //     return card.Type is CardType.Attack or CardType.Skill or CardType.Power;
    // }
    //
    // private static void RefreshCardText(CardModel card)
    // {
    //     NCard.FindOnTable(card)?.UpdateVisuals(card.Pile?.Type ?? PileType.None, CardPreviewMode.Normal);
    // }
    //
    // internal static IReadOnlyList<string> GetExtraCardText(CardModel card)
    // {
    //     if (!CardData.TryGetValue(card, out var data) || data.PotionIds.Count == 0)
    //     {
    //         return [];
    //     }
    //
    //     return data.PotionIds
    //         .Select(potionId => $"[aqua]星赐-{GetPotionTitle(potionId)}[/aqua]")
    //         .ToList();
    // }
    //
    // private static string GetPotionTitle(string potionId)
    // {
    //     return ModelDb.AllPotions
    //         .FirstOrDefault(p => p.Id.Entry == potionId)
    //         ?.Title
    //         .GetFormattedText() ?? potionId;
    // }
    //
    // internal static IEnumerable<IHoverTip> GetPotionHoverTips(CardModel card)
    // {
    //     if (!CardData.TryGetValue(card, out var data) || data.PotionIds.Count == 0)
    //     {
    //         return [];
    //     }
    //
    //     return data.PotionIds
    //         .Distinct()
    //         .SelectMany(potionId => ModelDb.AllPotions
    //             .FirstOrDefault(p => p.Id.Entry == potionId)
    //             ?.HoverTips ?? []);
    // }
    //
    // private async Task ApplyPotionEffect(
    //     PlayerChoiceContext choiceContext,
    //     string potionId,
    //     CardPlay cardPlay)
    // {
    //     var potion = ModelDb.AllPotions
    //         .FirstOrDefault(p => p.Id.Entry == potionId)
    //         ?.ToMutable();
    //
    //     if (potion == null) return;
    //
    //     potion.Owner = base.Owner;
    //
    //     var target = ResolveTarget(potion, cardPlay);
    //     if (potion.TargetType.IsSingleTarget() && target == null) return;
    //
    //     var onUseMethod = GetOnUseMethod(potion.GetType());
    //     if (onUseMethod == null) return;
    //
    //     choiceContext.PushModel(potion);
    //     try
    //     {
    //         if (onUseMethod.Invoke(potion, [choiceContext, target]) is Task task)
    //         {
    //             await task;
    //         }
    //
    //         potion.InvokeExecutionFinished();
    //     }
    //     finally
    //     {
    //         choiceContext.PopModel(potion);
    //     }
    // }
    //
    // private Creature? ResolveTarget(PotionModel potion, CardPlay cardPlay)
    // {
    //     return potion.TargetType switch
    //     {
    //         TargetType.Self or TargetType.AnyPlayer or TargetType.AnyAlly => base.Owner.Creature,
    //         TargetType.AnyEnemy => IsLegalEnemyTarget(cardPlay.Target) ? cardPlay.Target : null,
    //         TargetType.TargetedNoCreature => null,
    //         _ => null
    //     };
    // }
    //
    // private bool IsLegalEnemyTarget(Creature? target)
    // {
    //     return target is { IsHittable: true } &&
    //         target.Side != base.Owner.Creature.Side;
    // }
    //
    // private static MethodInfo? GetOnUseMethod(Type type)
    // {
    //     var parameterTypes = new[] { typeof(PlayerChoiceContext), typeof(Creature) };
    //     for (var current = type; current != null && typeof(PotionModel).IsAssignableFrom(current); current = current.BaseType)
    //     {
    //         var method = AccessTools.DeclaredMethod(current, "OnUse", parameterTypes);
    //         if (method != null)
    //         {
    //             return method;
    //         }
    //     }
    //
    //     return null;
    // }
    //
    // private sealed class BottledGalaxyCardData
    // {
    //     public List<string> PotionIds { get; } = [];
    // }
}

// [HarmonyPatch]
// public static class BottledGalaxyCardDescriptionPatch
// {
//     private static MethodBase TargetMethod()
//     {
//         return AccessTools.Method(
//             typeof(CardModel),
//             nameof(CardModel.GetDescriptionForPile),
//             [typeof(PileType), typeof(Creature)]);
//     }
//
//     public static void Postfix(CardModel __instance, ref string __result)
//     {
//         var extraText = BottledGalaxy.GetExtraCardText(__instance);
//         if (extraText.Count == 0)
//         {
//             return;
//         }
//
//         var suffix = string.Join('\n', extraText.Where(line => !string.IsNullOrEmpty(line)));
//         if (string.IsNullOrEmpty(suffix))
//         {
//             return;
//         }
//
//         __result = string.IsNullOrEmpty(__result)
//             ? suffix
//             : string.Join('\n', __result, suffix);
//     }
// }
//
// [HarmonyPatch]
// public static class BottledGalaxyCardHoverTipsPatch
// {
//     private static MethodBase TargetMethod()
//     {
//         return AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.HoverTips));
//     }
//
//     public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
//     {
//         var potionTips = BottledGalaxy.GetPotionHoverTips(__instance).ToList();
//         if (potionTips.Count == 0)
//         {
//             return;
//         }
//
//         __result = __result.Concat(potionTips).Distinct();
//     }
// }
//
// [HarmonyPatch]
// public static class BottledGalaxyCardVisualPatch
// {
//     private static MethodBase TargetMethod()
//     {
//         return AccessTools.Method(
//             typeof(NCard),
//             nameof(NCard.UpdateVisuals),
//             [typeof(PileType), typeof(CardPreviewMode)]);
//     }
//
//     public static void Postfix(NCard __instance)
//     {
//         var card = __instance.Model;
//         if (card == null)
//         {
//             return;
//         }
//
//         var extraText = BottledGalaxy.GetExtraCardText(card);
//         if (extraText.Count == 0)
//         {
//             return;
//         }
//
//         var descriptionLabel = AccessTools.Field(typeof(NCard), "_descriptionLabel")
//             ?.GetValue(__instance);
//         if (descriptionLabel == null)
//         {
//             return;
//         }
//
//         var textProperty = AccessTools.Property(descriptionLabel.GetType(), "Text");
//         var currentText = textProperty?.GetValue(descriptionLabel) as string;
//         if (string.IsNullOrEmpty(currentText) || currentText.Contains("星赐-"))
//         {
//             return;
//         }
//
//         var suffix = string.Join('\n', extraText.Where(line => !string.IsNullOrEmpty(line)));
//         if (string.IsNullOrEmpty(suffix))
//         {
//             return;
//         }
//
//         var nextText = currentText.Contains("[/center]")
//             ? currentText.Replace("[/center]", "\n" + suffix + "[/center]")
//             : string.Join('\n', currentText, suffix);
//
//         AccessTools.Method(descriptionLabel.GetType(), "SetTextAutoSize", [typeof(string)])
//             ?.Invoke(descriptionLabel, [nextText]);
//     }
// }
