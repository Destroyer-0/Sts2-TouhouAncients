using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;
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

    // --- 出生 Buff ---
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();

        _kaguyaVisuals = base.Creature.GetCreatureNode()?.Visuals as HouraisanKaguyaVisuals;
        // 第一回合无实体
        await PowerCmd.Apply<IntangiblePower>(new ThrowingPlayerChoiceContext(), base.Creature, 1m, base.Creature, null);
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
        MyAnimatedSprite2D.FlipH = false;
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
        SwitchToTrueForm();
        
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
    /// 永夜归返：获得 3 点力量并恢复 30 点生命。
    /// </summary>
    private async Task EternalNightReturnMove(IReadOnlyList<Creature> targets)
    {
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
