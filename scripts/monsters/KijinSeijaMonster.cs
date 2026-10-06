using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.Afflictions;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 鬼人正邪：逆转牌运的天邪鬼。
/// 状态机：入口条件分支（妖器再塑可用？）→ 妖器再塑 / 天壤梦弓 → 逆命诏敕 → 条件分支（有不语之物？）→ 喑哑泣啼 / 顺延回入口。
/// </summary>
public sealed class KijinSeijaMonster : TouhouAncientMonsterBase
{
    private const int MaxSilentObjects = 4;
    private const int SummonsPerMove = 2;
    private const int DreamBowHits = 3;
    private const int DecreeVulnerable = 2;
    private const int WeepingBuff = 3;

    protected override bool HasAnimation => false;

    protected override int InitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 187, 178);

    private int DreamBowDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);

    private int DecreeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

    private MoveState _fightForMe = null!;
    private MoveState _dreamBow = null!;
    private MoveState _decree = null!;
    private MoveState _silentWeeping = null!;
    private ConditionalBranchState _entryBranch = null!;
    private ConditionalBranchState _weepingBranch = null!;

    private int _fightForMeBanterIndex;
    private int _silentWeepingBanterIndex;

    /// <summary>历史牌是否可作为虚拟叛乱之潮的缓存（重建卡牌开销大）。</summary>
    private readonly Dictionary<SerializableCard, bool> _historyEligibleCache = new();

    /// <summary>场上存活的不语之物数量。</summary>
    private int AliveSilentObjectCount => base.CombatState.Enemies
        .Count(c => c is { Monster: SilentObjectMonster, IsDead: false });

    /// <summary>妖器再塑可用：可用列表含叛乱之潮，且衍生物未达上限。</summary>
    private bool CanUseFightForMe => AliveSilentObjectCount < MaxSilentObjects && BuildCandidates().Count > 0;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ReverseHeavenPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        _fightForMe = new MoveState("FIGHT_FOR_ME", FightForMeMove, new SummonIntent());
        _dreamBow = new MoveState("DREAM_BOW", DreamBowMove,
            new MultiAttackIntent(DreamBowDamage, DreamBowHits));
        _decree = new MoveState("DECREE_OF_DEFIANCE", DecreeMove,
            new SingleAttackIntent(DecreeDamage), new DebuffIntent());
        _silentWeeping = new MoveState("SILENT_WEEPING", SilentWeepingMove, new BuffIntent());

        // 入口分支：不满足妖器再塑则顺延到天壤梦弓
        _entryBranch = new ConditionalBranchState("ENTRY_BRANCH");
        _entryBranch.AddState(_fightForMe, () => CanUseFightForMe);
        _entryBranch.AddState(_dreamBow, () => true);

        // 喑哑泣啼分支：无不语之物则顺延回入口（因此可能再顺延至天壤梦弓）
        _weepingBranch = new ConditionalBranchState("WEEPING_BRANCH");
        _weepingBranch.AddState(_silentWeeping, () => AliveSilentObjectCount > 0);
        _weepingBranch.AddState(_entryBranch, () => true);

        _fightForMe.FollowUpState = _dreamBow;
        _dreamBow.FollowUpState = _decree;
        _decree.FollowUpState = _weepingBranch;
        _silentWeeping.FollowUpState = _entryBranch;

        list.Add(_entryBranch);
        list.Add(_weepingBranch);
        list.Add(_fightForMe);
        list.Add(_dreamBow);
        list.Add(_decree);
        list.Add(_silentWeeping);

        return new MonsterMoveStateMachine(list, _entryBranch);
    }

    /// <summary>妖器再塑：从可用列表抽牌塑成不语之物，最多两次且衍生物不超过上限。</summary>
    private async Task FightForMeMove(IReadOnlyList<Creature> targets)
    {
        PlayCycledBanter(ref _fightForMeBanterIndex, 3, "FIGHT_FOR_ME", VfxColor.Purple, VfxDuration.VeryLong);

        List<Candidate> pool = BuildCandidates();
        int summoned = 0;
        while (summoned < SummonsPerMove && pool.Count > 0 && AliveSilentObjectCount < MaxSilentObjects)
        {
            Candidate chosen = base.RunRng.MonsterAi.NextItem(pool)!;
            pool.Remove(chosen);

            CardModel? card = Materialize(chosen);
            if (card == null || !SilentObjectMonster.CanEmbody(card)) continue;

            await SummonSilentObject(card, chosen.Card != null);
            summoned++;
        }

        await Cmd.Wait(0.5f);
    }

    /// <summary>天壤梦弓：对玩家造成 3 段伤害。</summary>
    private async Task DreamBowMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DreamBowDamage)
            .FromMonster(this)
            .WithHitCount(DreamBowHits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>逆命诏敕：造成伤害并给予易伤，随后获得 1 力量。</summary>
    private async Task DecreeMove(IReadOnlyList<Creature> targets)
    {
        var decreeLine = new LocString("monsters", "TOUHOUANCIENTS-KIJIN_SEIJA_MONSTER.moves.DECREE_OF_DEFIANCE.banter1");
        TalkCmd.Play(decreeLine, base.Creature, VfxColor.Red, VfxDuration.Long);

        await DamageCmd.Attack(DecreeDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(), targets, DecreeVulnerable, base.Creature, null);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
    }

    /// <summary>喑哑泣啼：所有不语之物获得力量与敏捷。</summary>
    private async Task SilentWeepingMove(IReadOnlyList<Creature> targets)
    {
        PlayCycledBanter(ref _silentWeepingBanterIndex, 3, "SILENT_WEEPING", VfxColor.Gold, VfxDuration.Long);

        List<Creature> minions = base.CombatState.Enemies
            .Where(c => c is { Monster: SilentObjectMonster, IsDead: false })
            .ToList();
        foreach (Creature minion in minions)
        {
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), minion, WeepingBuff, base.Creature, null);
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), minion, WeepingBuff, base.Creature, null);
        }
    }

    /// <summary>召唤不语之物：先写入被代表的牌（创构时读 HP 与状态机），再移出游戏并加入战斗。</summary>
    private async Task SummonSilentObject(CardModel card, bool fromPlayer)
    {
        SilentObjectMonster monster = (SilentObjectMonster)ModelDb.Monster<SilentObjectMonster>().ToMutable();
        monster.InitFromCard(card, fromPlayer);

        // 移出游戏须在取面板值之后（移出后 CombatState 失效，附魔预览取不到）
        if (card.Pile != null)
        {
            await CardPileCmd.RemoveFromCombat(card);
        }

        string? slot = base.CombatState.Encounter?.GetNextSlot(base.CombatState);
        if (string.IsNullOrEmpty(slot)) return;

        await CreatureCmd.Add(monster, base.CombatState, CombatSide.Enemy, slot);
    }

    /// <summary>意图为喑哑泣啼时不语之物全灭：改回入口分支的求值结果（保留二次顺延）。</summary>
    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        await base.AfterDeath(choiceContext, creature, wasRemovalPrevented, deathAnimLength);
        if (wasRemovalPrevented) return;
        if (creature.Monster is not SilentObjectMonster) return;
        if (base.Creature.IsDead || base.IsPerformingMove) return;
        if (NextMove != _silentWeeping || AliveSilentObjectCount > 0) return;

        SetMoveImmediate(CanUseFightForMe ? _fightForMe : _dreamBow, forceTransition: true);
    }

    private void PlayCycledBanter(ref int index, int count, string moveKey, VfxColor color, VfxDuration duration)
    {
        index = index % count + 1;
        var line = new LocString("monsters", $"TOUHOUANCIENTS-KIJIN_SEIJA_MONSTER.moves.{moveKey}.banter{index}");
        TalkCmd.Play(line, base.Creature, color, duration);
    }

    /// <summary>可用列表：全体玩家的抽/弃/消耗堆内带叛乱之潮的牌 + 本局历史中被移除的基础打击/防御牌（虚拟叛乱之潮）。</summary>
    private List<Candidate> BuildCandidates()
    {
        List<Candidate> result = new List<Candidate>();
        ICombatState combat = base.CombatState;

        foreach (Player player in combat.Players)
        {
            PlayerCombatState? playerCombatState = player.PlayerCombatState;
            if (playerCombatState == null) continue;

            foreach (CardModel card in playerCombatState.DrawPile.Cards
                         .Concat(playerCombatState.DiscardPile.Cards)
                         .Concat(playerCombatState.ExhaustPile.Cards))
            {
                if (card.HasBeenRemovedFromState) continue;
                if (card.Affliction is RebellionTide)
                {
                    result.Add(new Candidate(player, card, null));
                }
            }

            foreach (SerializableCard serialized in CollectRemovedCards(player))
            {
                result.Add(new Candidate(player, null, serialized));
            }
        }

        // 多人同步：候选顺序必须各端一致
        result.Sort((a, b) => string.CompareOrdinal(a.SortKey, b.SortKey));
        return result;
    }

    /// <summary>汇总本局历史中被移除的基础打击/防御牌。</summary>
    private List<SerializableCard> CollectRemovedCards(Player player)
    {
        List<SerializableCard> result = new List<SerializableCard>();
        foreach (IReadOnlyList<MapPointHistoryEntry> act in base.CombatState.RunState.MapPointHistory)
        {
            foreach (MapPointHistoryEntry entry in act)
            {
                PlayerMapPointHistoryEntry? stats = entry.PlayerStats.FirstOrDefault(p => p.PlayerId == player.NetId);
                if (stats == null) continue;
                foreach (SerializableCard serialized in stats.CardsRemoved)
                {
                    if (IsBasicStrikeOrDefend(serialized)) result.Add(serialized);
                }
            }
        }
        return result;
    }

    private bool IsBasicStrikeOrDefend(SerializableCard serialized)
    {
        if (_historyEligibleCache.TryGetValue(serialized, out bool cached)) return cached;
        bool result = ComputeBasicStrikeOrDefend(serialized);
        _historyEligibleCache[serialized] = result;
        return result;
    }

    private static bool ComputeBasicStrikeOrDefend(SerializableCard serialized)
    {
        if (serialized.Id is not { } id) return false;
        CardModel? canonical = ModelDb.GetByIdOrNull<CardModel>(id);
        if (canonical == null || canonical.Rarity != CardRarity.Basic) return false;
        if (!canonical.Tags.Contains(CardTag.Strike) && !canonical.Tags.Contains(CardTag.Defend)) return false;

        // 永恒（含附魔赋予的）不产生虚拟叛乱之潮
        if (canonical.CanonicalKeywords.Contains(CardKeyword.Eternal)) return false;
        if (serialized.Enchantment != null)
        {
            try
            {
                if (CardModel.FromSerializable(serialized).Keywords.Contains(CardKeyword.Eternal)) return false;
            }
            catch
            {
                // 重建失败按非永恒处理
            }
        }
        return true;
    }

    /// <summary>把候选还原为真实可变卡牌：战斗牌直接返回，历史牌重建并注册进 CombatState。</summary>
    private CardModel? Materialize(Candidate candidate)
    {
        if (candidate.Card != null) return candidate.Card;
        if (candidate.Serialized == null) return null;
        try
        {
            // 必须注册进 CombatState，否则这张牌之后加入战斗牌堆（弃牌堆/消耗堆）时会抛
            // "must be added to a CombatState before adding it to this pile"
            CardModel card = CardModel.FromSerializable(candidate.Serialized);
            base.CombatState.AddCard(card, candidate.Owner);
            return card;
        }
        catch
        {
            return null;
        }
    }

    private sealed class Candidate
    {
        public Candidate(Player owner, CardModel? card, SerializableCard? serialized)
        {
            Owner = owner;
            Card = card;
            Serialized = serialized;
        }

        public Player Owner { get; }

        public CardModel? Card { get; }

        public SerializableCard? Serialized { get; }

        public string SortKey => Card != null
            ? $"{Owner.NetId}|C|{Card.Id}|{Card.CurrentUpgradeLevel}"
            : $"{Owner.NetId}|H|{Serialized!.Id}";
    }
}
