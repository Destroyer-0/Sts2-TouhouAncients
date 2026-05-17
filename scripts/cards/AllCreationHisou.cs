using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 全人类的绯想天：3费能力。保留。
/// 打出并消耗所有当前手牌与消耗堆里的牌。
/// 升级后：升级、打出并消耗所有当前手牌与消耗堆里的牌。
/// </summary>
[Pool(typeof(EventCardPool))]
public class AllCreationHisou : TouhouAncientCards
{
    private const int energyCost = 3;
    private const CardType type = CardType.Power;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.Self;
    private const bool shouldShowInCardLibrary = true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    //protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Amount", 5m)];

    public AllCreationHisou() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = base.Owner;
        if (player?.PlayerCombatState == null) return;

        
        // 快照：记录当前手牌（排除自身）和消耗堆的所有牌
        var handCards = player.PlayerCombatState.Hand.Cards
            .Where(c => c != cardPlay.Card)
            .ToList();

        var exhaustCards = player.PlayerCombatState.ExhaustPile.Cards.ToList();

        var hitNum = 0;
        var allTargetCards = handCards.Concat(exhaustCards).ToList();
        bool shouldUpgrade = cardPlay.Card.IsUpgraded;

        var playerCreature = player.Creature;
        var a = NCombatRoom.Instance?.GetCreatureNode(playerCreature);
        Vector2 vfxSpawnPosition = a.VfxSpawnPosition;

        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(NFireSmokePuffVfx.Create(playerCreature));
        await Cmd.CustomScaledWait(0.2f, 0.4f);
        VfxCmd.PlayOnCreatureCenter(playerCreature, "vfx/vfx_scream");
        NHyperbeamVfx? nHyperbeamVfx = NHyperbeamVfx.Create(vfxSpawnPosition + new Vector2(0, 100), new Vector2(vfxSpawnPosition.X, vfxSpawnPosition.Y - 10));
        if (nHyperbeamVfx != null)
        {
            nHyperbeamVfx.GlobalScale = new Vector2(2, 2);
            NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamVfx);
            float scale = 2.8f;
            NGroundFireVfx nGroundFireVfx = NGroundFireVfx.Create(playerCreature);
            if (nGroundFireVfx != null)
            {
                SfxCmd.Play("event:/sfx/characters/attack_fire");
                nGroundFireVfx.Scale = Vector2.One * scale;
                NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nGroundFireVfx);
                scale += 0.2f;
            }
            await Cmd.Wait(0.5f);
        }
        
        List<Creature> enumerable = (from c in base.CombatState.GetTeammatesOf(base.Owner.Creature)
            where c is { IsAlive: true, IsPlayer: true }
            select c).ToList();
        foreach (var card in allTargetCards)
        {
            // 升级后：先升级每张牌
            if (shouldUpgrade && card.IsUpgradable)
            {
                CardCmd.Upgrade(card);
            }

            // foreach (Creature item in enemies)
            // {
            //     NHyperbeamImpactVfx nHyperbeamImpactVfx = NHyperbeamImpactVfx.Create(base.Owner.Creature, item);
            //     if (nHyperbeamImpactVfx != null)
            //     {
            //         NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamImpactVfx);
            //     }
            // }
            // 自动打出
            await CardCmd.AutoPlay(new BlockingPlayerChoiceContext(), card, target: null);

            // 打出后若牌还存在且不在消耗堆，则消耗它
            if (card.Pile != null && card.Pile.Type != PileType.None && !card.HasBeenRemovedFromState)
            {
                await CardCmd.Exhaust(new BlockingPlayerChoiceContext(), card, skipVisuals: true);
            }
            //
            // hitNum++;
            // if (hitNum == base.DynamicVars["Amount"].BaseValue)
            // {
            //     hitNum = 0;
            //     
            //     await ApplyOnAllPlayer(x => PowerCmd.Apply<StrengthPower>(x, 1m, playerCreature, this));
            //     await ApplyOnAllPlayer(x => PowerCmd.Apply<DexterityPower>(x, 1m, playerCreature, this));
            //     await ApplyOnAllPlayer(x => PowerCmd.Apply<FocusPower>(x, 1m, playerCreature, this));
            // }

        }
        async Task ApplyOnAllPlayer(Func<Creature,Task> task)
        {
            foreach (Creature item in enumerable)
            {
                await task(item);
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 OnPlay 中通过 cardPlay.Card.IsUpgraded 判断
    }
}
