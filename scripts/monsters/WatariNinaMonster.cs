using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 渡里贝子：先古之民挑战 Boss。
/// 开局不是满血；三次蜃影阴谋结束后失去「觉醒之贝?」。
/// 状态机：蜃影阴谋 → 狂热信息流 → 真伪诳语 → 宇宙冷漠 →（还有未选阴谋论则回蜃影阴谋）。
/// </summary>
public sealed class WatariNinaMonster : TouhouAncientMonsterBase
{
    protected override bool HasAnimation => false;

    private const int InformationFlowDamage = 8;

    private const int CosmicIndifferenceBlock = 9;

    private const int FictionalCityCount = 3;

    private const int ConspiracyUsesUntilPowerLost = 3;

    private const int CuriosityStrengthGain = 2;

    private const int InitialArtifact = 3;

    private int _conspiracyUses;

    /// <summary>每个玩家各自记录已选过的阴谋论，避免多人下互相挤占选项。</summary>
    private readonly Dictionary<ulong, HashSet<ModelId>> _chosenConspiracies = new();

    protected override int InitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 288, 270);

    private int StartingHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 233, 220);

    private int AwakenedShellHeal =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        if (base.Creature.CombatState is { } combatState)
        {
            decimal currentHp = Creature.ScaleHpForMultiplayer(
                StartingHp, combatState.Encounter, combatState.Players.Count, combatState.RunState.CurrentActIndex);
            base.Creature.SetCurrentHpInternal(currentHp);

            int playerCount = combatState.Players.Count;
            var choiceContext = new ThrowingPlayerChoiceContext();
            // 「觉醒之贝?」显示/回复量按联机人数放大
            await PowerCmd.Apply<AwakenedShellPower>(
                choiceContext, base.Creature, AwakenedShellHeal * playerCount, base.Creature, null);
            CuriosityPower? curiosity = await PowerCmd.Apply<CuriosityPower>(
                choiceContext, base.Creature, CuriosityStrengthGain, base.Creature, null);
            curiosity?.Configure(playerCount);

            await PowerCmd.Apply<ArtifactPower>(choiceContext, base.Creature, InitialArtifact, base.Creature, null);
        }
        else
        {
            base.Creature.SetCurrentHpInternal(StartingHp);
        }
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState conspiracy = new("MIRAGE_CONSPIRACY", ConspiracyMove, new DebuffIntent());
        MoveState informationFlow = new("FRENZIED_INFORMATION_FLOW", InformationFlowMove,
            new SingleAttackIntent(InformationFlowDamage));
        MoveState truthAndLie = new("TRUTH_AND_LIE", TruthAndLieMove, new StatusIntent(FictionalCityCount));
        MoveState cosmicIndifference = new("COSMIC_INDIFFERENCE", CosmicIndifferenceMove, new DefendIntent());

        ConditionalBranchState afterCosmic = new("AFTER_COSMIC_INDIFFERENCE");
        afterCosmic.AddState(conspiracy, HasRemainingConspiracy);
        afterCosmic.AddState(informationFlow, () => !HasRemainingConspiracy());

        conspiracy.FollowUpState = informationFlow;
        informationFlow.FollowUpState = truthAndLie;
        truthAndLie.FollowUpState = cosmicIndifference;
        cosmicIndifference.FollowUpState = afterCosmic;

        return new MonsterMoveStateMachine(
            [conspiracy, informationFlow, truthAndLie, cosmicIndifference, afterCosmic], conspiracy);
    }

    private async Task ConspiracyMove(IReadOnlyList<Creature> targets)
    {
        foreach (Creature target in targets.Where(target => !target.IsDead))
        {
            Player? player = target.Player;
            if (player == null) continue;

            List<CardModel> remaining = GetRemainingConspiracies(player);
            if (remaining.Count == 0) continue;

            List<CardModel> choices = remaining
                .Select(card => base.CombatState.CreateCard(card, player))
                .ToList();
            CardModel? chosen = await CardSelectCmd.FromChooseACardScreen(
                new BlockingPlayerChoiceContext(), choices, player);
            if (chosen is not IConspiracyChoice conspiracy) continue;

            await conspiracy.OnChosen(new ThrowingPlayerChoiceContext(), base.Creature);
            GetChosen(player).Add(chosen.Id);
        }

        _conspiracyUses++;
        if (_conspiracyUses >= ConspiracyUsesUntilPowerLost)
        {
            await PowerCmd.Remove<AwakenedShellPower>(base.Creature);
        }
    }

    private bool HasRemainingConspiracy()
    {
        if (base.Creature.CombatState is not { } combatState) return true;

        return combatState.Players
            .Where(player => !player.Creature.IsDead)
            .Any(player => GetRemainingConspiracies(player).Count > 0);
    }

    private List<CardModel> GetRemainingConspiracies(Player player)
    {
        HashSet<ModelId> chosen = GetChosen(player);
        List<CardModel> remaining = [];

        AddIfNotChosen(ModelDb.Card<ConspiracyMazares>(), chosen, remaining);
        AddIfNotChosen(ModelDb.Card<ConspiracySerpent>(), chosen, remaining);
        AddIfNotChosen(ModelDb.Card<ConspiracyHeavenlyEye>(), chosen, remaining);

        return remaining;
    }

    private static void AddIfNotChosen(CardModel card, HashSet<ModelId> chosen, List<CardModel> remaining)
    {
        if (!chosen.Contains(card.Id))
        {
            remaining.Add(card);
        }
    }

    private HashSet<ModelId> GetChosen(Player player)
    {
        if (!_chosenConspiracies.TryGetValue(player.NetId, out HashSet<ModelId>? chosen))
        {
            chosen = [];
            _chosenConspiracies[player.NetId] = chosen;
        }

        return chosen;
    }

    private async Task InformationFlowMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(InformationFlowDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task TruthAndLieMove(IReadOnlyList<Creature> targets)
    {
        foreach (Creature target in targets.Where(target => !target.IsDead))
        {
            Player? player = target.Player;
            if (player == null) continue;

            await CardPileCmd.AddToCombatAndPreview<FictionalCity>(
                player.Creature, PileType.Draw, FictionalCityCount, null, CardPilePosition.Random);
        }
    }

    private async Task CosmicIndifferenceMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(base.Creature, CosmicIndifferenceBlock, ValueProp.Move, null);
    }
}
