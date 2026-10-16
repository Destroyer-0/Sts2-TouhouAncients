using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 诅咒之血：拾起时，获得一张腐朽。在你的每个回合开始时，炼制两瓶药水并喝下。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class CursedBlood : TouhouAncientRelics
{
    public override string DefaultFileName => "yuuma_default";
    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("PotionCount", 2), new CardsVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromCardWithCardHoverTips<Decay>();

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        await CardPileCmd.AddCursesToDeck(Enumerable.Repeat(ModelDb.Card<Decay>(),2),base.Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != base.Owner) return;
        var rng = player.RunState.Rng.CombatPotionGeneration;

        // 获取所有可解锁的自指向药水
        var selfTargetPotions = PotionFactory.GetPotionOptions(player)
            .Where(p => p is { CanBeGeneratedInCombat: true, TargetType: TargetType.Self or TargetType.AnyPlayer })
            .ToList();

        if (selfTargetPotions.Count == 0) return;

        // 炼制并喝下的药水不算该房间的奖励/使用记录，故记下历史长度以便事后剔除
        var historyEntry = player.RunState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId);
        var potionChoices = historyEntry?.PotionChoices;
        var potionUsed = historyEntry?.PotionUsed;

        int count = DynamicVars["PotionCount"].IntValue;
        for (int i = 0; i < count; i++)
        {
            var selected = selfTargetPotions[rng.NextInt(selfTargetPotions.Count)].ToMutable();
            selected.Owner = player;

            // 直接入栏并立即使用
            int choiceCount = potionChoices?.Count ?? 0;
            var procResult = await PotionCmd.TryToProcure(selected, player);
            if (!procResult.success) continue;

            // TryToProcure 会写一条 wasPicked=true 的药水历史，会让炼制的药水显示成奖励掉落
            if (potionChoices != null && potionChoices.Count > choiceCount)
            {
                potionChoices.RemoveAt(potionChoices.Count - 1);
            }

            Flash();
            // 使用药水（以自身为目标）
            int usedCount = potionUsed?.Count ?? 0;
            await selected.OnUseWrapper(choiceContext, player.Creature);

            // 使用记录在 OnUseWrapper 末尾才写入，末尾那条即本次喝下的药水
            if (potionUsed != null && potionUsed.Count > usedCount)
            {
                potionUsed.RemoveAt(potionUsed.Count - 1);
            }

            // 等待药水效果完成
            await Cmd.Wait(0.5f);
        }
    }
}
