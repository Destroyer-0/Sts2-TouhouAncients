using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using TouhouAncients.Scripts.Background;
using TouhouAncients.Scripts.cards;
using TouhouAncients.Scripts.encounters;
using TouhouAncients.Scripts.powers;
using TouhouAncients.Scripts.Vfx;

namespace TouhouAncients.Scripts.monsters;

/// <summary>
/// 蓬莱山辉夜：永远亭的月之公主。
/// 状态机：五道难题 → 苍白之瀑 → 虚假之月 → 永夜归返 →（回环）苍白之瀑。
/// 五道难题向每位玩家发放五种谜题卡（龙颈之玉/火鼠的皮衣/燕之子安贝/佛御石之钵/蓬莱的玉枝），
/// 并施加"公主的谜题"：未完成的谜题每道在辉夜回合开始时为她提供格挡。
/// </summary>
public sealed class HouraisanKaguyaMonster : TouhouAncientMonsterBase
{
    // --- HP ---
    protected override int InitialHp => AscensionHelper.GetValueIfAscension(
        AscensionLevel.ToughEnemies, 255, 240);

    // --- 伤害/数值 ---
    private int PaleWhiteWaterfallDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 9, 8);

    private const int PaleWhiteWaterfallHits = 2;

    private int FalseMoonDamage => AscensionHelper.GetValueIfAscension(
        AscensionLevel.DeadlyEnemies, 14, 13);

    private const int EternalNightReturnStrength = 3;
    private const int BaseBlockPerPuzzle = 12;

    private const int EternalNightReturnHeal = 30;

    /// <summary>拥有无实体期间，辉夜立绘的透明度（其余时间恢复为 1）。</summary>
    private const float IntangibleAlpha = 0.6f;

    /// <summary>五道难题发放的谜题卡数量（五种各一张）。</summary>
    private const int PuzzleCardCount = 5;

    /// <summary>五道难题释放后，辉夜曲淡入时长（秒）。</summary>
    private const float FiveDifficultProblemsBgmFadeInSeconds = 2f;

    /// <summary>五道难题漂浮谜题演出辅助（挂在场景根节点的脚本）。</summary>
    private HouraisanKaguyaVisuals? _kaguyaVisuals;

    /// <summary>觉醒后的循环动画名（真月形态 idle，使用 Kaguya.png）。</summary>
    private const string AwakenedIdleAnimation = "awakened_idle";

    /// <summary>是否已觉醒：施放五道难题后切换为 Kaguya.png 立绘，直到战斗结束不再变回。</summary>
    private bool _awakenedToTrueForm;

    // --- 台词 ---

    /// <summary>战斗开始时的开场台词（在首回合之前播放一次）。</summary>
    private static readonly LocString _battleStartLine =
        new LocString("monsters", "TOUHOUANCIENTS-HOURAISAN_KAGUYA_MONSTER.moves.FIVE_DIFFICULT_PROBLEMS.banter1");

    /// <summary>首次施放永夜归返时的台词。</summary>
    private static readonly LocString _eternalNightReturnLine =
        new LocString("monsters", "TOUHOUANCIENTS-HOURAISAN_KAGUYA_MONSTER.moves.ETERNAL_NIGHT_RETURN.banter");

    /// <summary>
    /// 五道难题施放时的台词本地化键。该条文本内部使用 SmartFormat 的 choose 分支，
    /// 依据代码传入的 PlayerCount 变量决定称呼：玩家数为 1 时输出「你」，多人时输出「你们」。
    /// </summary>
    private const string FiveDifficultProblemsLineKey =
        "TOUHOUANCIENTS-HOURAISAN_KAGUYA_MONSTER.moves.FIVE_DIFFICULT_PROBLEMS.banter2";

    /// <summary>是否已播放过首次永夜归返台词（永夜归返在状态机循环中会重复出现，仅首次播放）。</summary>
    private bool _eternalNightReturnBanterPlayed;

    /// <summary>
    /// 是否跳过辉夜的常规台词：单人模式下唯一玩家为妹红时返回 true。
    /// 妹红的彩蛋台词（<c>MOKOU_BANTER</c> / <c>MOKOU_BANTER2</c>）由 <see cref="HouraiPuzzleCard"/> 另行播放，
    /// 此时辉夜的常规台词不触发，避免两套台词混在一起。
    /// </summary>
    private bool ShouldSkipBanter
    {
        get
        {
            if (base.Creature.CombatState is not { } combatState)
            {
                return false;
            }

            if (combatState.Players.Count != 1)
            {
                return false;
            }

            return combatState.Players[0].Character.Id.Entry.Contains("MOKOU", StringComparison.OrdinalIgnoreCase);
        }
    }

    private void PlayBanter(LocString line)
    {
        if (ShouldSkipBanter)
        {
            return;
        }

        TalkCmd.Play(line, base.Creature, VfxColor.White, VfxDuration.VeryLong);
    }

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        _kaguyaVisuals = base.Creature.GetCreatureNode()?.Visuals as HouraisanKaguyaVisuals;
        // 无实体期间立绘半透明：先订阅能力施加 / 移除事件，再施加无实体，
        // 施加后再同步一次透明度（事件可能在施加过程中触发，这里做兜底刷新）
        base.Creature.PowerApplied += AfterPowerApplied;
        base.Creature.PowerRemoved += AfterPowerRemoved;
        // 第一回合无实体
        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
        RefreshIntangibleTransparency();
    }


    /// <summary>
    /// 怪物移出房间时取消能力事件订阅，避免事件残留引用已结束战斗的怪物。
    /// </summary>
    public override void BeforeRemovedFromRoom()
    {
        base.Creature.PowerApplied -= AfterPowerApplied;
        base.Creature.PowerRemoved -= AfterPowerRemoved;
    }

    /// <summary>
    /// 能力被施加回调：获得无实体时把立绘透明度降为 <see cref="IntangibleAlpha"/>。
    /// </summary>
    private void AfterPowerApplied(PowerModel power)
    {
        if (power is IntangiblePower)
        {
            RefreshIntangibleTransparency();
        }
    }

    /// <summary>
    /// 能力被移除回调：失去无实体（含回合结束自然过期归零）时把立绘透明度恢复正常。
    /// </summary>
    private void AfterPowerRemoved(PowerModel power)
    {
        if (power is IntangiblePower)
        {
            RefreshIntangibleTransparency();
        }
    }

    /// <summary>
    /// 根据当前是否拥有无实体刷新立绘透明度：
    /// 拥有时 alpha 为 <see cref="IntangibleAlpha"/>，否则恢复为 1（保留原本的 RGB）。
    /// 显示节点不可用时静默跳过，避免在显示节点已回收的流程中抛异常导致战斗卡死。
    /// </summary>
    private void RefreshIntangibleTransparency()
    {
        AnimatedSprite2D? sprite = Sprite;
        if (sprite == null)
        {
            return;
        }

        Color color = sprite.Modulate;
        color.A = base.Creature.HasPower<IntangiblePower>() ? IntangibleAlpha : 1f;
        sprite.Modulate = color;
    }

    // --- 状态机 ---
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> list = new List<MonsterState>();

        // 五道难题：向每位玩家发放 5 张谜题卡，并施加公主的谜题（战斗开始仅一次）
        MoveState fiveDifficultProblems = new MoveState("FIVE_DIFFICULT_PROBLEMS", FiveDifficultProblemsMove,
            new StatusIntent(PuzzleCardCount), new BuffIntent());

        // 苍白之瀑：2 段攻击
        MoveState paleWhiteWaterfall = new MoveState("PALE_WHITE_WATERFALL", PaleWhiteWaterfallMove,
            new MultiAttackIntent(PaleWhiteWaterfallDamage, PaleWhiteWaterfallHits));

        // 虚假之月：造成伤害并减少玩家下回合的抽牌数
        MoveState falseMoon = new MoveState("FALSE_MOON", FalseMoonMove,
            new SingleAttackIntent(FalseMoonDamage), new DebuffIntent());

        // 永夜归返：获得力量并恢复生命
        MoveState eternalNightReturn = new MoveState("ETERNAL_NIGHT_RETURN", EternalNightReturnMove,
            new BuffIntent(), new HealIntent());

        // 固定序列：五道难题 → 苍白之瀑 → 虚假之月 → 永夜归返 →（回环）苍白之瀑
        fiveDifficultProblems.FollowUpState = paleWhiteWaterfall;
        paleWhiteWaterfall.FollowUpState = falseMoon;
        falseMoon.FollowUpState = eternalNightReturn;
        eternalNightReturn.FollowUpState = paleWhiteWaterfall;

        list.Add(fiveDifficultProblems);
        list.Add(paleWhiteWaterfall);
        list.Add(falseMoon);
        list.Add(eternalNightReturn);

        return new MonsterMoveStateMachine(list, fiveDifficultProblems);
    }

    /// <summary>
    /// 注册动画状态：施放五道难题后切换为觉醒形态循环动画 <see cref="AwakenedIdleAnimation"/>
    /// （真月形态，使用 Kaguya.png）。注册为循环动画后，觉醒形态下受击 hurt 播完能恢复到
    /// awakened_idle 而非切回默认 idle（旧立绘）。
    /// </summary>
    protected override void ConfigureAnimationStateMachine(MonsterAnimationStateMachine animationMachine)
    {
        animationMachine.RegisterLoop(AwakenedIdleAnimation);
    }

    /// <summary>
    /// 施放五道难题后切换为觉醒形态（Kaguya.png 立绘）。仅切换一次，直到战斗结束不再变回。
    /// 默认形态（旧立绘）在场景中已设置水平镜像（flip_h = true）；觉醒形态立绘朝左，
    /// 切换动画前先取消镜像（FlipH = false）。
    /// </summary>
    private void SwitchToTrueForm()
    {
        if (_awakenedToTrueForm)
        {
            return;
        }

        _awakenedToTrueForm = true;
        if (Sprite is { } sprite)
        {
            sprite.FlipH = false;
        }
        Anim.Trigger(AwakenedIdleAnimation);
    }

    // --- 技能方法 ---

    /// <summary>
    /// 五道难题：对每个未死亡的玩家创建 5 张谜题卡（五种各一张），
    /// 打乱顺序后随机加入其抽牌堆；随后对自身施加"公主的谜题"能力
    /// （未解开谜题数由该能力内部初始化为 5）。
    /// </summary>
    private async Task FiveDifficultProblemsMove(IReadOnlyList<Creature> targets)
    {
        PlayBanter(_battleStartLine);
        await Cmd.Wait(1f);

        SwitchToTrueForm();
        // 背景开场为暗色（kaguya_background.tscn 根节点 modulate），释放五道难题后 1 秒转亮。
        (NCombatRoom.Instance?.Background as TouhouAncientBackground)?.FadeTo(Colors.White, 1f);
        
        foreach (Creature target in targets.Where(t => !t.IsDead))
        {
            Player? player = target.Player;
            if (player == null) continue;

            List<CardModel> puzzleCards = new List<CardModel>
            {
                player.Creature.CombatState.CreateCard<DragonNeckJewelCard>(player),
                player.Creature.CombatState.CreateCard<HinezumiNoKawagoromoCard>(player),
                player.Creature.CombatState.CreateCard<SwallowCowrieShellCard>(player),
                player.Creature.CombatState.CreateCard<BuddhaStoneBowlCard>(player),
                player.Creature.CombatState.CreateCard<HouraiNoTamaeCard>(player)
            };
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardsToCombat(puzzleCards, PileType.Draw, null, CardPilePosition.Random),
                1.5f, CardPreviewStyle.HorizontalLayout);
        }

        await Cmd.Wait(2f);

        _kaguyaVisuals?.ShowPuzzles();
        
        await PowerCmd.Apply<PrincessPuzzlePower>(new ThrowingPlayerChoiceContext(), base.Creature, BaseBlockPerPuzzle, base.Creature, null);

        // 台词：传入玩家数，文本内 choose 分支据此在「你」与「你们」之间选择
        // （无法取得战斗状态时按单人处理，保证变量始终存在，避免 choose 落到默认分支；
        //  单人妹红局由 PlayBanter 内部跳过）
        int playerCount = base.Creature.CombatState?.Players.Count ?? 1;
        LocString line = new LocString("monsters", FiveDifficultProblemsLineKey);
        line.Add("PlayerCount", playerCount);
        PlayBanter(line);
        
        if (base.Creature.CombatState.Encounter is TouhouAncientEncounter encounter && !string.IsNullOrEmpty(encounter.BgmFileName))
        {
            EncounterBgm.Start(encounter.BgmFileName, FiveDifficultProblemsBgmFadeInSeconds);
        }
    }

    /// <summary>
    /// 苍白之瀑：2 段攻击。
    /// </summary>
    private async Task PaleWhiteWaterfallMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(PaleWhiteWaterfallDamage)
            .FromMonster(this)
            .WithHitCount(PaleWhiteWaterfallHits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    /// <summary>
    /// 虚假之月：造成伤害，并减少每位存活玩家下回合的抽牌数（原生 Amount=-1）。
    /// </summary>
    private async Task FalseMoonMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FalseMoonDamage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        List<Creature> aliveTargets = targets.Where(t => !t.IsDead).ToList();
        if (aliveTargets.Count > 0)
        {
            await PowerCmd.Apply<DecreaseDrawCardsNextTurnPower>(new ThrowingPlayerChoiceContext(), aliveTargets, 1m, base.Creature, null);
        }
    }

    /// <summary>
    /// 永夜归返：获得 3 点力量并恢复 30 点生命。首次施放时额外播放台词。
    /// </summary>
    private async Task EternalNightReturnMove(IReadOnlyList<Creature> targets)
    {
        // 永夜归返会在状态机循环中重复出现，台词仅在首次施放时播放
        // （单人妹红局由 PlayBanter 内部跳过）
        if (!_eternalNightReturnBanterPlayed)
        {
            _eternalNightReturnBanterPlayed = true;
            PlayBanter(_eternalNightReturnLine);
        }

        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.Creature, EternalNightReturnStrength, base.Creature, null);
        await CreatureCmd.Heal(base.Creature, EternalNightReturnHeal* base.Creature.CombatState.Players.Count);
        var poisonPowers = base.Creature.GetPowerAmount<PoisonPower>();
        if (poisonPowers > 0)
        {
            await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), this.Creature,
                -(Math.Min(10, poisonPowers / 2)), base.Creature, null);
        }

        var doomPower = base.Creature.GetPowerAmount<DoomPower>();
        if (doomPower > 0)
        {
            await PowerCmd.Apply<DoomPower>(new ThrowingPlayerChoiceContext(), this.Creature,
                -(Math.Min(20, doomPower / 2)), base.Creature, null);
        }
    }

    /// <summary>
    /// 谜题进度回调（由 <see cref="PrincessPuzzlePower.CompletePuzzle"/> 调用）：
    /// 玩家解开某道谜题后，按该谜题当前已完成玩家数与总玩家数的比例
    /// 更新漂浮演出中对应谜题图标的透明度（多人模式下等比例下降）。
    /// </summary>
    public void NotifyPuzzleProgress(int puzzleType)
    {
        HouraisanKaguyaVisuals? visuals = _kaguyaVisuals;
        if (visuals == null)
        {
            return;
        }

        PrincessPuzzlePower? puzzlePower = base.Creature.GetPower<PrincessPuzzlePower>();
        if (puzzlePower == null)
        {
            return;
        }

        int completedCount = puzzlePower.GetCompletedPlayerCount(puzzleType);
        int playerCount = base.Creature.CombatState?.Players.Count ?? 1;
        visuals.UpdatePuzzleTransparency(puzzleType, completedCount, playerCount);
    }

    /// <summary>
    /// 获取指定谜题类型尚未完成的玩家显示名列表（漂浮谜题悬停提示用，仅多人模式有内容）。
    /// </summary>
    public IReadOnlyList<string> GetIncompletePuzzlePlayerNames(int puzzleType)
    {
        PrincessPuzzlePower? puzzlePower = base.Creature.GetPower<PrincessPuzzlePower>();
        if (puzzlePower == null)
        {
            return Array.Empty<string>();
        }

        return puzzlePower.GetIncompletePlayerNames(puzzleType);
    }
}
