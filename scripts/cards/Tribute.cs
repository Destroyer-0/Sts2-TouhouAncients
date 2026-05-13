using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.cards;

/// <summary>
/// 供奉：Quest卡牌，不可打出。累计供奉300金币后从牌组中移除。
/// </summary>
[Pool(typeof(EventCardPool))]
public class Tribute : TouhouAncientCards
{
    private const int RequiredGold = 250;
    
    // BaseLib 的 [SavedProperty] 需要放在属性上，不能放在字段上。
    [SavedProperty]
    private int SavedGoldContributed { get; set; }

    public int GoldContributed
    {
        get => SavedGoldContributed;
        set
        {
            AssertMutable();
            SavedGoldContributed = value;
            base.DynamicVars["Remaining"].BaseValue = RequiredGold - SavedGoldContributed;
        }
    }

    public int RemainingGold => RequiredGold - SavedGoldContributed;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Remaining", RequiredGold)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Eternal];

    public Tribute() : base(-1, CardType.Quest, CardRarity.Quest, TargetType.None, shouldShowInCardLibrary: false)
    {
    }

    /// <summary>
    /// 尝试贡献金币到供奉进度。
    /// </summary>
    public async Task<bool> TryContribute(int amount)
    {
        var contribute = Math.Min(amount, RemainingGold);
        if (contribute <= 0) return false;

        GoldContributed += contribute;

        if (GoldContributed >= RequiredGold && base.Pile?.Type == PileType.Deck)
        {
            await CardPileCmd.RemoveFromDeck(this);
            return true;
        }

        return false;
    }
}
