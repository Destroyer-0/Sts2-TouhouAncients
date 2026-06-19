using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 纯粹的弹幕地狱 (Pure Hell of Bullets)
/// 3c，消耗。升级后获得保留。
/// 选择任意张手牌，变化为无名的弹幕。
/// 在这个回合，攻击牌免费打出并抽一张牌，当你手中没有攻击牌时，结束你的回合。
/// </summary>
[Pool(typeof(EventCardPool))]
public class PureHellOfBullets : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<NamelessBullets>(),
        base.EnergyHoverTip
    ];

    public PureHellOfBullets() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        var hand = PileType.Hand.GetPile(player).Cards.ToList();

        // 选择任意张手牌变为无名的弹幕
        
        List<CardModel> selected = (await CardSelectCmd.FromHand(prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 0, 999999999), context: choiceContext, player: base.Owner, filter: null, source: this)).ToList();


        NGroundFireVfx nGroundFireVfx = NGroundFireVfx.Create(Owner.Creature, VfxColor.Purple);
        if (nGroundFireVfx != null)
        {
            SfxCmd.Play("event:/sfx/characters/attack_fire");
            nGroundFireVfx.Scale = Vector2.One * 3;
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGroundFireVfx);
        }
        
        foreach (var card in selected)
        {
            // 将选中的牌变化为无名的弹幕
            var namelessBullet = player.Creature.CombatState.CreateCard<NamelessBullets>(player);
            await CardCmd.Transform(card, namelessBullet);
        }

        // 应用弹幕地狱 Power（让攻击牌免费 + 抽牌 + 检测无攻击牌结束回合）
        await PowerCmd.Apply<HellOfBulletsPower>(choiceContext, player.Creature, 1m, player.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级后获得保留
        AddKeyword(CardKeyword.Retain);
    }
}
