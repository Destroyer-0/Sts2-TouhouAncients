using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.monsters;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 蓬莱谜题：五道难题的谜题卡抽象基类（Quest 卡）。
/// 拥有保留与谜题关键词；打出后完成对应谜题并将此牌移出战斗。
/// 彩蛋：萃香（MOKOU）抽到谜题卡时立即完成对应谜题并将此牌移出战斗。
/// 子类通过 <see cref="PuzzleType"/> 指定谜题类型（0-4）。
/// </summary>
public abstract class HouraiPuzzleCard : TouhouAncientCards
{
    public override string? Author => "东方光耀夜 Touhou Lost Branch of Legend";
    public enum PuzzleCardName
    {
        龙颈之玉 = 0,
        火鼠的皮衣 = 1,
        燕之子安贝 = 2,
        佛御石之钵 = 3,
        蓬莱的玉枝 = 4,
    }

    public override bool CanBeGeneratedByModifiers => false;

    public override bool CanBeGeneratedInCombat => false;

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, TouhouAncientKeywords.TouhouAncientPuzzle];

    protected HouraiPuzzleCard(int energyCost) : base(energyCost, CardType.Quest, CardRarity.Quest, TargetType.None, shouldShowInCardLibrary: true)
    {
    }

    /// <summary>
    /// 谜题类型编号（0-4）。
    /// 0 = 龙颈之玉、1 = 火鼠的皮衣、2 = 燕之子安贝、3 = 佛御石之钵、4 = 蓬莱的玉枝。
    /// </summary>
    protected abstract PuzzleCardName PuzzleType { get; }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != this) return;
        await CompletePuzzleForOwner();
    }

    /// <summary>
    /// 完成对应谜题：找到辉夜身上的公主的谜题能力，标记本玩家完成该类型谜题。
    /// 辉夜不在场时容错跳过。
    /// </summary>
    protected async Task CompletePuzzleForOwner()
    {
        Creature? kaguya = base.Owner.Creature.CombatState?.Enemies.FirstOrDefault(c => c.Monster is HouraisanKaguyaMonster);
        if (kaguya == null) return;
        PrincessPuzzlePower? puzzlePower = kaguya.GetPower<PrincessPuzzlePower>();
        if (puzzlePower == null) return;
        puzzlePower.CompletePuzzle((int)PuzzleType, base.Owner);
    }

    /// <summary>
    /// 彩蛋：萃香（MOKOU）抽到谜题卡时立即完成对应谜题并将此牌移出战斗。
    /// </summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        if (!base.Owner.Character.Id.Entry.Contains("MOKOU", StringComparison.OrdinalIgnoreCase)) return;
        Creature? kaguya = base.Owner.Creature.CombatState?.Enemies.FirstOrDefault(c => c.Monster is HouraisanKaguyaMonster);
        if (kaguya == null) return;
        if (CombatState.Players.Count > 1)
        {
            TalkCmd.Play(new LocString("monsters", "TOUHOUANCIENTS-HOURAISAN_KAGUYA_MONSTER.MOKOU_BANTER2"), base.Owner.Creature, VfxColor.Red, VfxDuration.Long);
            return;
        }
        await CompletePuzzleForOwner();
        TalkCmd.Play(new LocString("monsters", "TOUHOUANCIENTS-HOURAISAN_KAGUYA_MONSTER.MOKOU_BANTER"), base.Owner.Creature, VfxColor.Red, VfxDuration.Long);
        await PowerCmd.Apply<StrengthPower>(choiceContext, base.Owner.Creature, 1, base.Owner.Creature, null);
        await PowerCmd.Apply<StrengthPower>(choiceContext, kaguya, 1, base.Owner.Creature, null);
        await CardCmd.Exhaust(choiceContext, card);
    }
}