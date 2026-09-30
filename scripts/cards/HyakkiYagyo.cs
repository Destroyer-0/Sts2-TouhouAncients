using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.relics;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 百鬼夜行：2c（升级后1c）技能 保留。
/// 打出随机{Cards}张誊写记录的牌，然后将它们在本场战斗中从誊写记录中移除。
/// 誊写记录全部移除后，将此牌在本场战斗中移出游戏。
/// </summary>
[Pool(typeof(EventCardPool))]
public class HyakkiYagyo : TouhouAncientCards
{
    public override string? Author => "抽风男";

    private const int energyCost = 2;
    private const CardType type = CardType.Skill;
    private const CardRarity rarity = CardRarity.Ancient;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    /// <summary>持久化的誊写记录：每种角色最多一张，跨战斗与读档保留。</summary>
    private List<SerializableCard> _records = [];

    public IReadOnlyList<SerializableCard> Records => _records;
    
    /// <summary>本场战斗剩余的誊写记录；战斗开始时从持久记录复制，打出的牌在这里移除。</summary>
    private List<SerializableCard> _combatRecords = [];

    public HyakkiYagyo() : base(energyCost, type, rarity, targetType, shouldShowInCardLibrary)
    {
    }

    /// <summary>每次打出时从誊写记录中打出（并消耗）的张数。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    /// <summary>誊写记录（每种角色最多一张）。</summary>
    [SavedProperty]
    private List<SerializableCard> HyakkiYagyo_Records
    {
        get => _records;
        set
        {
            AssertMutable();
            _records.Clear();
            _records.AddRange(value);
        }
    }

    /// <summary>本场战斗剩余的誊写记录张数（0 表示这张牌在本场战斗中已无事可做）。</summary>
    private int CombatRecordCount => _combatRecords.Count;

    /// <summary>自己在战斗牌堆里（牌组原件不算）。</summary>
    private bool IsInCombatPile => Pile is { IsCombatPile: true };

    /// <summary>牌组里的原件；自己是战斗副本时为原件，牌组原件为 null。</summary>
    private HyakkiYagyo? DeckOrigin => DeckVersion as HyakkiYagyo;

    /// <summary>持久记录的持有者：战斗副本读写的是牌组原件那一份。</summary>
    private HyakkiYagyo RecordOwner => DeckOrigin ?? this;

    protected override void AfterCloned()
    {
        base.AfterCloned();
        // 克隆是浅拷贝字段，本场记录池必须换一份，否则战斗副本会改到牌组原件那份。
        _combatRecords = [];
    }

    /// <summary>
    /// 战斗开始时把持久记录复制成本场记录池。记录池为空说明本场无事可做，把这张牌自己移出游戏。
    /// </summary>
    public override async Task BeforeCombatStart()
    {
        // 牌组原件也在这个钩子的分发链上，只有战斗副本参与本场判定。
        if (!IsInCombatPile) return;

        _combatRecords = [.. RecordOwner._records];
        if (_combatRecords.Count > 0) return;

        await RemoveHyakkiYagyoFromCombat();
    }

    /// <summary>战斗中才进入牌堆的百鬼夜行（例如复制出来的）：同样从持久记录生成本场记录池。</summary>
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (card != this) return Task.CompletedTask;
        if (!IsInCombatPile) return Task.CompletedTask;

        _combatRecords = [.. RecordOwner._records];
        return Task.CompletedTask;
    }

    /// <summary>战斗结束时清空本场记录池；持久记录不受影响，下一场战斗照常复制。</summary>
    public override Task AfterCombatEnd(CombatRoom room)
    {
        _combatRecords = [];
        return Task.CompletedTask;
    }
    

    /// <summary>
    /// 打出随机{Cards}张誊写记录的牌，然后把它们在本场战斗中从誊写记录中移除。
    /// </summary>
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player player = Owner;
        List<SerializableCard> records =
            TakeRandomCombatRecords(DynamicVars.Cards.IntValue, player.RunState.Rng.CombatCardSelection);

        foreach (SerializableCard record in records)
        {
            if (CombatManager.Instance.IsOverOrEnding || player.Creature.IsDead) break;

            var combatState = player.Creature.CombatState;
            if (combatState == null) break;

            // 誊写记录只是牌样：临场造一张战斗副本打出，打完即不属于任何牌堆。
            
            var cardModel = FromSerializable(record);
            player.Creature.CombatState.AddCard(cardModel,Owner);
            await CardCmd.AutoPlay(choiceContext, cardModel.CreateDupe(player), null);
        }

        if (CombatRecordCount <= 0)
        {
            await CardPileCmd.RemoveFromCombat(this);
        }
    }

    /// <summary>
    /// 「已誊写」悬停：title 固定，description 是当前誊写记录里的卡名（升级过的带「+」，各语言自己的分隔符），
    /// 后接每张被誊写牌的卡面。没有记录时不显示（图鉴里的 canonical 没有记录，自然也不显示）。
    /// </summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            if (IsCanonical)
            {
                return [];
            }
            List<TranscribedCard> cards = TranscribedCards.ToList();
            if (cards.Count == 0) return [];

            string entry = ModelDb.Relic<HyakkiYagyoScroll>().Id.Entry;
            LocString description = new("relics", entry + ".transcribedDescription");
            description.Add("Cards", cards.Select(card => card.Title).ToList());

            return new IHoverTip[]
                {
                    new HoverTip(new LocString("relics", entry + ".transcribedTitle"), description),
                }
                .Concat(cards.Select(card => HoverTipFactory.FromCard(card.Model, card.Upgraded)));
        }
    }

    /// <summary>
    /// 当前誊写记录对应的卡牌（读的是记录持有者那一份）；模型已缺失的条目跳过。
    /// 记录只存在牌组里那一张上，所以读不到持有者时就是空。
    /// </summary>
    private IEnumerable<TranscribedCard> TranscribedCards =>
        (CombatManager.Instance.IsInProgress ? _combatRecords : _records)
        .Where(saved => saved.Id != null)
        .Select(saved =>
        {
            CardModel? model = ModelDb.GetByIdOrNull<CardModel>(saved.Id!);
            return model == null
                ? null
                : new TranscribedCard(model, saved.CurrentUpgradeLevel > 0);
        })
        .OfType<TranscribedCard>();


    /// <summary>一条誊写记录：卡面模型与是否升级过。</summary>
    private sealed record TranscribedCard(CardModel Model, bool Upgraded)
    {
        /// <summary>卡名。CardModel 的 Title 已经为升级过的牌带上「+」。</summary>
        public string Title => Model.Title;
    }

    /// <summary>
    /// 把一张牌誊写进这张牌的记录：按来源角色的卡牌池去重，同池已有记录时替换。
    /// 战斗中誊写时同步写进本场记录池，本场已打出的该池记录因此重新可用。
    /// </summary>
    public void Record(CardPoolModel pool, CardModel card)
    {
        HyakkiYagyo owner = RecordOwner;
        owner.AssertMutable();

        SerializableCard record = card.ToSerializable();
        SetRecord(owner._records, pool.Id, record);
    }

    /// <summary>从本场记录池里随机取最多 <paramref name="count"/> 张，并把它们在本场从记录池移除。</summary>
    private List<SerializableCard> TakeRandomCombatRecords(int count, Rng rng)
    {
        List<SerializableCard> taken = [];
        if (count <= 0) return taken;

        List<SerializableCard> candidates = [.. _combatRecords];
        candidates.UnstableShuffle(rng);

        foreach (SerializableCard record in candidates.Take(count))
        {
            _combatRecords.Remove(record);
            taken.Add(record);
        }

        return taken;
    }

    /// <summary>把这张牌自己在本场战斗中移出游戏（进入 None 牌堆）；只影响自己，其他百鬼夜行不受影响。</summary>
    private async Task RemoveHyakkiYagyoFromCombat()
    {
        if (!IsInCombatPile) return;
        await CardPileCmd.RemoveFromCombat(this, skipVisuals: true);
    }

    /// <summary>在记录列表里按来源卡牌池写入一张牌：已有同池记录则替换，否则追加。</summary>
    private void SetRecord(List<SerializableCard> records, ModelId poolId, SerializableCard record)
    {
        for (int i = 0; i < records.Count; i++)
        {
            if (PoolIdOf(records[i]) == poolId)
            {
                records[i] = record;
                return;
            }
        }

        records.Add(record);
    }

    /// <summary>记录里那张牌的来源卡牌池；牌已不存在（模型缺失）时返回 null。</summary>
    public static ModelId? PoolIdOf(SerializableCard record)
    {
        if (record.Id == null) return null;
        return ModelDb.GetByIdOrNull<CardModel>(record.Id)?.Pool.Id;
    }

    protected override void OnUpgrade()
    {
        base.EnergyCost.UpgradeBy(-1);
        // 升级后 1c
    }
}
