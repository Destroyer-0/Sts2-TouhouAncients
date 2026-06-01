using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 梦想封印：1费，对所有敌人造成2(升级后3)点伤害6次。
/// 若其意图为攻击，给予1层易伤并使其在本回合失去8(升级后10)点力量。
/// 意图检测参考：GoForTheEyes。
/// </summary>
[Pool(typeof(EventCardPool))]
public class MasterSpark : TouhouAncientCards
{
    private const int energyCost = 0;
    private const CardType type = CardType.Attack;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.AllEnemies;
    private const bool shouldShowInCardLibrary = true;

    protected override bool HasEnergyCostX => true;

    protected override bool ShouldGlowGoldInternal => base.Owner.PlayerCombatState.Energy >= base.DynamicVars["Need"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(7m, ValueProp.Move),
        new DynamicVar("Need",4),
        new DynamicVar("ExtraHit", 1),
        new EnergyVar(1)
    ];

    public MasterSpark() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        //ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        int num = ResolveEnergyXValue();
        var gainEnergy = false;
        if (num >= base.DynamicVars["Need"].IntValue)
        {
            gainEnergy = true;
            num *= IsUpgraded ? 3 : 2;
        }

        num += DynamicVars["ExtraHit"].IntValue;

        await Cmd.Wait(0.5f);

        var target = base.CombatState.Enemies.Where((Creature e) => e.IsAlive).ToList().Last();
        var owner = base.Owner.Creature;
        NCreature creatureNode1 = NCombatRoom.Instance?.GetCreatureNode(owner);
        NCreature creatureNode2 = NCombatRoom.Instance?.GetCreatureNode(target);
        var shouldSpawnSpark = creatureNode2 != null && creatureNode1 != null;
        Vector2 vfxSpawnPosition = Vector2.Zero;
        if (shouldSpawnSpark)
        { 
            vfxSpawnPosition = creatureNode1.VfxSpawnPosition;
            Player player = owner.Player;
            if (player is { Character: Defect })
                vfxSpawnPosition += Defect.EyelineOffset;
            // NHyperbeamVfx nHyperbeamVfx = NHyperbeamVfx.Create(vfxSpawnPosition, creatureNode2.VfxSpawnPosition);
            // if (nHyperbeamVfx != null)
            // {
            //     NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamVfx);
            //     await Cmd.Wait(0.5f);
            // }
        }

        await DamageCmd.Attack(base.DynamicVars.Damage.BaseValue).WithHitCount(num).FromCard(this)
            .TargetingAllOpponents(base.CombatState)
            .WithAttackerAnim("Cast", 0.5f)
            .WithHitFx("vfx/vfx_starry_impact", null, "slash_attack.mp3")
            .SpawningHitVfxOnEachCreature()
            .BeforeDamage(async delegate
            {
                if (shouldSpawnSpark)
                {
                    //List<Creature> enemies = base.CombatState.Enemies.Where((Creature e) => e.IsAlive).ToList();
                    NHyperbeamVfx nHyperbeamVfx2 = NHyperbeamVfx.Create(vfxSpawnPosition, creatureNode2.VfxSpawnPosition);
                    if (nHyperbeamVfx2 != null)
                    {
                        NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamVfx2);
                        //await Cmd.Wait(0.5f);
                    }
                    //
                    // foreach (Creature item in enemies)
                    // {
                    //     NHyperbeamImpactVfx nHyperbeamImpactVfx = NHyperbeamImpactVfx.Create(base.Owner.Creature, item);
                    //     if (nHyperbeamImpactVfx != null)
                    //     {
                    //         NCombatRoom.Instance?.CombatVfxContainer.AddChildSafely(nHyperbeamImpactVfx);
                    //     }
                    // }
                }
            })
            .Execute(choiceContext);
        if (gainEnergy)
        {
            await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        }
    }
}