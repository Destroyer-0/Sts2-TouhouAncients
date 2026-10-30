using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.Akyuu;

/// <summary>
/// 妖魔图鉴：每当你遇到敌人时，记录其名称（按怪物种类，记录只在本局游戏内有效）。
/// 再次遇到名称存在于记录中的敌人时，其只有1点生命值。
/// 本场战斗新见的怪物要到战斗结束才入册，因此同场战斗不会互相触发。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class YokaiBestiary : TouhouAncientRelics
{
    private const char Separator = '\t';

    /// <summary>已入册的怪物种类（<c>MonsterModel.Id.Entry</c>），制表符分隔。
    /// BaseLib 的 [SavedProperty] 不支持 List&lt;string&gt;，照缸中之脑 BrainInAVat 用字符串存。</summary>
    [SavedProperty]
    private string YokaiBestiary_Recorded { get; set; } = "";

    private List<string>? _recorded;

    /// <summary>本场战斗新见到、还没入册的种类（战斗内数据，不需要持久化）。</summary>
    private readonly List<string> _pending = [];

    private List<string> Recorded => _recorded ??= string.IsNullOrEmpty(YokaiBestiary_Recorded)
        ? []
        : [.. YokaiBestiary_Recorded.Split(Separator)];

    protected override void AfterCloned()
    {
        base.AfterCloned();
        _recorded = null;
        _pending.Clear();
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    /// <summary>悬停显示已入册的怪物名（名称键 = <c>{Id.Entry}.name</c>，同 <c>MonsterModel.Title</c>），外加灵感来源。</summary>
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>
            {
                new HoverTip(new LocString("relics", Id.Entry + ".inspirationTitle"), "Fumo the Spire")
            };

            var recorded = Recorded;
            if (recorded.Count == 0) return tips;

            var title = new LocString("relics", Id.Entry + ".recordTitle");
            var desc = new LocString("relics", Id.Entry + ".record");
            desc.Add("Monsters", recorded
                .Select(e => MonsterModel.L10NMonsterLookup(e + ".name").GetFormattedText())
                .ToList());
            tips.Add(new HoverTip(title, desc));
            return tips;
        }
    }

    /// <summary>开场已在场的敌人走这里：AfterCreatureAddedToCombat 只派发给战斗中途加入的生物。</summary>
    public override async Task BeforeCombatStart()
    {
        var enemies = base.Owner.Creature?.CombatState?.Enemies;
        if (enemies == null) return;

        foreach (var enemy in enemies) await ProcessEnemy(enemy);
    }

    public override async Task AfterCreatureAddedToCombat(Creature creature) => await ProcessEnemy(creature);

    private async Task ProcessEnemy(Creature creature)
    {
        if (!creature.IsEnemy) return;
        if (creature.Monster == null) return;

        var entry = creature.Monster.Id.Entry;
        if (Recorded.Contains(entry))
        {
            if (creature.CurrentHp <= 1m) return;

            Flash();
            await CreatureCmd.SetCurrentHp(creature, 1m);
            return;
        }

        if (!_pending.Contains(entry)) _pending.Add(entry);
    }

    /// <summary>战斗结束才入册，所以同场战斗遇到两次也不会立刻触发。</summary>
    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (_pending.Count == 0) return Task.CompletedTask;

        var recorded = Recorded;
        foreach (var entry in _pending)
        {
            if (!recorded.Contains(entry)) recorded.Add(entry);
        }
        _pending.Clear();

        _recorded = recorded;
        YokaiBestiary_Recorded = string.Join(Separator, recorded);
        return Task.CompletedTask;
    }
}
