using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 未名妖魔：1c（升级后0c）技能 保留。
/// 将任意张手牌变化为稀有度相同的其他颜色的牌（升级后：已升级的其他颜色的牌）。
/// 本回合内，打出与上一张打出的牌颜色不同的牌时，获得1能量并在本回合获得1（升级后2）点力量。
/// （颜色指卡牌池，随机颜色即随机角色卡池中的牌）
/// </summary>
[Pool(typeof(EventCardPool))]
public class NamelessYoukai : TouhouAncientCards
{
    public override string? Author => "茶葉馬場ば";

    private const int energyCost = 0;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Transform),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Power", 2m),
        new EnergyVar(1)
    ];

    public NamelessYoukai() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (player.Creature.CombatState == null) return;

        // 从手牌中选择任意张牌
        var selected = (await CardSelectCmd.FromHand(
            prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 0, int.MaxValue),
            context: choiceContext,
            player: player,
            filter: card => card.IsTransformable,
            source: this)).ToList();

        // 将每张选中的牌变化为相同稀有度的随机颜色的牌（升级后变化为已升级的牌）
        var rng = player.RunState.Rng.CombatCardGeneration;
        List<CardTransformation> transformations = new();
        foreach (var card in selected)
        {
            var options = GetSameRarityColoredCards(card, player);
            if (options.Count == 0) continue;
            var replacement = card.CardScope!.CreateCard(rng.NextItem(options)!, player);
            if (card.IsUpgraded && replacement.IsUpgradable)
            {
                CardCmd.Upgrade(replacement);
            }

            transformations.Add(new CardTransformation(card, replacement));
        }

        if (transformations.Count > 0)
        {
            await CardCmd.Transform(transformations, null, CardPreviewStyle.GridLayout);
        }

        // 施加 Power（参照原版独白 Monologue）：本回合打出与上一张颜色不同的牌时，获得1能量与本回合力量
        NamelessYoukaiPower? power = await PowerCmd.Apply<NamelessYoukaiPower>(choiceContext, player.Creature, 1m, player.Creature, this);
        if (power != null)
        {
            power.DynamicVars.Strength.BaseValue = base.DynamicVars["Power"].BaseValue;
            // 把未名妖魔自身作为初始"上一张牌"：之后打出的第一张牌即可与之比较颜色
            power.SetInitialPreviousCard(this);
        }
    }

    /// <summary>
    /// 收集所有角色卡池（颜色）中与指定牌稀有度相同的可选牌，排除原卡所在的卡池（颜色）。
    /// </summary>
    private static List<CardModel> GetSameRarityColoredCards(CardModel card, Player player)
    {
        return ModelDb.AllCharacterCardPools
            .Where(pool => pool.GetType() != card.Pool.GetType())
            .SelectMany(pool => pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            .Where(c => c.Rarity == card.Rarity && c.Id != card.Id && c.CanBeGeneratedInCombat)
            .ToList();
    }
}