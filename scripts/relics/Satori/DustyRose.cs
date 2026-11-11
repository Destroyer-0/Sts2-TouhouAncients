using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 尘封蔷薇：每回合开始时把 1 张禁闭之眼放入手牌。
/// 古明地恋玩家改为「拾起时，初始遗物替换为先古版本」（同欧洛巴斯之触）。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class DustyRose : TouhouAncientRelics
{
    private const string StarterRelicKey = "StarterRelic";
    private const string UpgradedRelicKey = "UpgradedRelic";

    /// <summary>恋 mod 的角色 Entry：只有恋玩家才切成替换形态。</summary>
    private const string KoishiCharacterEntry = "KOISHI-KOISHI";

    private List<IHoverTip> _upgradeTips = new();
    private ModelId? _starterRelic;
    private ModelId? _upgradedRelic;

    [SavedProperty]
    public ModelId? StarterRelic
    {
        get => _starterRelic;
        set
        {
            _starterRelic = value;
            UpdateText();
        }
    }

    [SavedProperty]
    public ModelId? UpgradedRelic
    {
        get => _upgradedRelic;
        set
        {
            _upgradedRelic = value;
            UpdateText();
        }
    }

    /// <summary>替换形态：只有恋玩家且初始遗物有先古版时 <see cref="SetupForPlayer"/> 才会填值。</summary>
    private bool IsStarterUpgrade => _starterRelic != null;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar(StarterRelicKey),
        new StringVar(UpgradedRelicKey)
    ];

    /// <summary>替换形态挂两件遗物的悬停；正常形态保持禁闭之眼（canonical 也走这条）。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        IsStarterUpgrade ? _upgradeTips : HoverTipFactory.FromCardWithCardHoverTips<TheKoishiEye>();

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _upgradeTips = new List<IHoverTip>();
    }

    /// <summary>
    /// 选项生成阶段调用（此时遗物自身 Owner 尚未绑定，所以玩家由调用方传入）。
    /// 非恋玩家直接返回，遗物保持原效果，描述走 <c>cond:</c> 的兜底分支。
    /// </summary>
    public void SetupForPlayer(Player player)
    {
        if (!string.Equals(player.Character.Id.Entry, KoishiCharacterEntry, StringComparison.OrdinalIgnoreCase)) return;

        var starterRelic = player.Relics.FirstOrDefault(r => r.Rarity == RelicRarity.Starter);
        if (starterRelic == null) return;

        // 复用原版对照表（BaseLib/RitsuLib 会补自己注册的配对）；查不到时返回 Circlet，视为无先古版
        var upgraded = ModelDb.Relic<TouchOfOrobas>().GetUpgradedStarterRelic(starterRelic);
        if (upgraded.Id == ModelDb.Relic<Circlet>().Id) return;

        StarterRelic = starterRelic.Id;
        UpgradedRelic = upgraded.Id;
    }

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        // 替换形态没有每回合加牌的效果
        if (player != base.Owner || IsStarterUpgrade)
        {
            return;
        }

        if (player.Creature.CombatState == null) return;
        var koishiEye = player.Creature.CombatState.CreateCard<TheKoishiEye>(player);
        Flash();
        await CardPileCmd.AddGeneratedCardsToCombat([koishiEye], PileType.Hand, creator: base.Owner);
    }

    public override async Task AfterObtained()
    {
        if (!IsStarterUpgrade || base.Owner == null) return;

        var starterRelic = base.Owner.GetRelicById(_starterRelic!);
        if (starterRelic == null) return;

        var upgradedRelic = ModelDb.GetById<RelicModel>(_upgradedRelic!);
        if (upgradedRelic.Id == starterRelic.Id) return;

        Flash();
        await RelicCmd.Replace(starterRelic, upgradedRelic.ToMutable());
    }

    /// <summary>按已保存的目标重建描述变量与悬停；克隆/读档后 setter 会重新走一遍。</summary>
    private void UpdateText()
    {
        _upgradeTips.Clear();

        if (_starterRelic != null)
        {
            var starterRelic = SaveUtil.RelicOrDeprecated(_starterRelic);
            ((StringVar)base.DynamicVars[StarterRelicKey]).StringValue = starterRelic.Title.GetFormattedText();
            _upgradeTips.AddRange(starterRelic.HoverTips);
        }

        if (_upgradedRelic != null)
        {
            var upgradedRelic = SaveUtil.RelicOrDeprecated(_upgradedRelic);
            ((StringVar)base.DynamicVars[UpgradedRelicKey]).StringValue = upgradedRelic.Title.GetFormattedText();
            _upgradeTips.AddRange(upgradedRelic.HoverTips);
        }
    }
}