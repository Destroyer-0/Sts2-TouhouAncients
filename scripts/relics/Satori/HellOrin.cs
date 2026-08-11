using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 地狱猫车：每当一个非爪牙的敌人死亡时，获得1计数、1点能量并恢复1点生命。你可以在休息处复燃。
/// 复燃：消耗所有计数，每消耗1层恢复2点生命。如果消耗的计数不小于10，选择一张牌，为它附魔：灵魂之力（失去消耗）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class HellOrin : TouhouAncientRelics
{
    private int _count;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Count;

    /// <summary>
    /// 累计的非爪牙敌人斩杀计数（持久化保存，跨战斗累积）
    /// </summary>
    [SavedProperty]
    public int Count
    {
        get => _count;
        set
        {
            AssertMutable();
            _count = value;
            InvokeDisplayAmountChanged();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        // 每次斩杀获得的尸体计数
        new DynamicVar("CorpsePerKill", 1),
        // 每次斩杀恢复的生命值
        new DynamicVar("HealPerKill", 1),
        // 复燃时每消耗 1 层计数恢复的生命值
        new DynamicVar("HealPerCount", 3),
        // 复燃时触发附魔的计数阈值
        new DynamicVar("EnchantThreshold", 10),
        // 附魔层数
        new DynamicVar("EnchantAmount", 1),
        // 每次斩杀获得的能量
        new EnergyVar(1),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        new List<IHoverTip>
        {
            new HoverTip(
                new LocString("rest_site_ui", "OPTION_HELL_ORIN.name"),
                DescriptionForTip())
        }.Concat(HoverTipFactory.FromEnchantment<SoulsPower>());

    private LocString DescriptionForTip()
    {
        var desc = new LocString("rest_site_ui", "OPTION_HELL_ORIN.description");
        desc.Add("HealPerCount", base.DynamicVars["HealPerCount"].BaseValue);
        desc.Add("EnchantThreshold", base.DynamicVars["EnchantThreshold"].BaseValue);
        desc.Add("EnchantmentName", ModelDb.Enchantment<SoulsPower>().Title.GetFormattedText());
        return desc;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!creature.IsEnemy) return; // 只针对敌人死亡
        // 与原版 Feed / BookRepairKnife 的斩杀判定一致：
        // 排除爪牙（MinionPower）、幻影（IllusionPower 自带 MinionPower）以及复活中的敌人（如残杀千足虫 ReattachPower）
        if (!creature.Powers.All(p => p.ShouldOwnerDeathTriggerFatal())) return;

        Flash();
        Count += (int)base.DynamicVars["CorpsePerKill"].BaseValue;
        await PlayerCmd.GainEnergy(base.DynamicVars.Energy.BaseValue, base.Owner);
        await CreatureCmd.Heal(base.Owner.Creature, base.DynamicVars["HealPerKill"].BaseValue);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != base.Owner) return false;
        options.Add(new HellOrinRestSiteOption(player, this));
        return true;
    }

    /// <summary>
    /// 复燃：消耗所有计数，每消耗1层恢复2点生命。若消耗的计数不小于10，选择一张牌，为它附魔：灵魂之力。
    /// </summary>
    public async Task Reignite()
    {
        if (Count <= 0) return;

        Flash();
        int spent = Count;
        Count = 0;
        await CreatureCmd.Heal(base.Owner.Creature, spent * base.DynamicVars["HealPerCount"].BaseValue);

        if (spent >= (int)base.DynamicVars["EnchantThreshold"].BaseValue)
        {
            SoulsPower soulsPower = ModelDb.Enchantment<SoulsPower>();
            // 牌组中没有可被附魔灵魂之力（失去消耗）的牌时，跳过选牌流程直接结束
            if (!PileType.Deck.GetPile(base.Owner).Cards.Any(soulsPower.CanEnchant)) return;

            CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.EnchantSelectionPrompt, 1);
            foreach (CardModel item in await CardSelectCmd.FromDeckForEnchantment(base.Owner, soulsPower, 1, prefs))
            {
                CardCmd.Enchant(soulsPower.ToMutable(), item, base.DynamicVars["EnchantAmount"].BaseValue);
                CardCmd.Preview(item);
            }
        }
    }
}

/// <summary>
/// 复燃休息处选项
/// </summary>
public class HellOrinRestSiteOption(Player owner, HellOrin relic) : RestSiteOption(owner)
{
    public override string OptionId => "HELL_ORIN";

    public override LocString Description
    {
        get
        {
            if (IsEnabled)
            {
                LocString locString = new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");
                locString.Add("HealPerCount", relic.DynamicVars["HealPerCount"].BaseValue);
                locString.Add("EnchantThreshold", relic.DynamicVars["EnchantThreshold"].BaseValue);
                locString.Add("EnchantmentName", ModelDb.Enchantment<SoulsPower>().Title.GetFormattedText());
                return locString;
            }

            return new LocString("rest_site_ui", "OPTION_" + OptionId + ".descriptionDisabled");
        }
    }
    
    public override bool IsEnabled => relic.Count > 0;

    public override async Task<bool> OnSelect()
    {
        await relic.Reignite();
        return true;
    }
    
    
    public static void PlayRestSiteHealSfx()
    {
        //NDebugAudioManager.Instance?.Play("sleep.tres");
        NDebugAudioManager.Instance?.Play("SOTE_SFX_SleepBlanket_v1.mp3", 1f, PitchVariance.Small);
    }
    public override async Task DoLocalPostSelectVfx(CancellationToken ct = default(CancellationToken))
    {
        PlayRestSiteHealSfx();
        NRestSiteRoom.Instance?.AddChildSafely(NRestSmokeVfx.Create());
        NRestSiteRoom.Instance?.AddChildSafely(NDesaturateTransitionVfx.Create());
        await Cmd.CustomScaledWait(1.5f, 2.5f, ignoreCombatEnd: false, ct);
    }
}
