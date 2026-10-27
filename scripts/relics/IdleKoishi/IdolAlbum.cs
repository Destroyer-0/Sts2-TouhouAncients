using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using TouhouAncients.Scripts.Enchantment;
using TouhouAncients.Scripts.Hooks;

namespace TouhouAncients.Scripts.relics.IdleKoishi;

/// <summary>
/// 偶像专辑：拾起时为至多 3 张牌附魔蔷薇；休息处可重播再附魔一张；
/// 进入嗨到爆时把一张带蔷薇附魔的牌移回手牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class IdolAlbum : TouhouAncientRelics, IHypedEnteredListener
{
    private const int EnchantCount = 3;
    private const int RoseBonusPerPlay = 5;

    private int _roseBonus;

    /// <summary>本回合蔷薇伤害加成（由 <see cref="Rose"/> 累加，回合末清零）。</summary>
    public int RoseBonusThisTurn => _roseBonus;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("EnchantCount", EnchantCount),
        new StringVar("EnchantmentName", ModelDb.Enchantment<Rose>().Title.GetFormattedText()),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        [.. HoverTipFactory.FromEnchantment<Rose>()];

    public void AddRoseBonus(int amount) => _roseBonus += amount;

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player == null) return;

        Flash();
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 0, EnchantCount);
        var selected = await CardSelectCmd.FromDeckForEnchantment(player, ModelDb.Enchantment<Rose>(), 1, prefs);

        foreach (var card in selected)
        {
            CardCmd.Enchant<Rose>(card, 1m);
            var vfx = NCardEnchantVfx.Create(card);
            if (vfx != null)
            {
                NRun.Instance?.GlobalUi.CardPreviewContainer.AddChildSafely(vfx);
            }
        }
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner) return false;

        options.Add(new IdolAlbumRestSiteOption(player, this));
        return true;
    }

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, MegaCrit.Sts2.Core.Combat.CombatSide side,
        IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants)
    {
        var player = base.Owner;
        if (player?.Creature == null) return Task.CompletedTask;
        if (side != player.Creature.Side || !participants.Contains(player.Creature)) return Task.CompletedTask;

        _roseBonus = 0;
        return Task.CompletedTask;
    }

    public async Task AfterHypedEntered(PlayerChoiceContext choiceContext)
    {
        var player = base.Owner;
        if (player?.Creature?.CombatState == null) return;

        var candidates = new[] { PileType.Draw, PileType.Discard, PileType.Exhaust }
            .SelectMany(pile => pile.GetPile(player).Cards)
            .Where(card => card.Enchantment is Rose)
            .ToList();
        if (candidates.Count == 0) return;

        var card = player.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        if (card == null) return;

        Flash();
        await CardPileCmd.Add(card, PileType.Hand);
    }

    /// <summary>休息处重播：为一张牌附魔蔷薇。返回 false 表示取消，选项不消耗。</summary>
    public async Task<bool> Replay()
    {
        var player = base.Owner;
        if (player == null) return false;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1) { Cancelable = true };
        var selected = (await CardSelectCmd.FromDeckForEnchantment(player, ModelDb.Enchantment<Rose>(), 1, prefs))
            .FirstOrDefault();
        if (selected == null) return false;

        Flash();
        CardCmd.Enchant<Rose>(selected, 1m);
        return true;
    }
}

/// <summary>偶像专辑的休息处选项「重播」。</summary>
public class IdolAlbumRestSiteOption(Player owner, IdolAlbum relic) : RestSiteOption(owner)
{
    public const string OptionIdValue = "IDOL_ALBUM";

    public override string OptionId => OptionIdValue;

    public override async Task<bool> OnSelect() => await relic.Replay();
}
