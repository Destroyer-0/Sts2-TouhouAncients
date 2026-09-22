using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace TouhouAncients.Scripts.relics.DoremySweet;

/// <summary>
/// 先驱之梦：拾起时，将随机一张上一场战斗的非初始卡牌加入你的牌组。
/// </summary>
[Pool(typeof(EventRelicPool))]
public class PioneerDream : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public override bool HasUponPickupEffect => true;

    /// <summary>
    /// 选项条件：没有候选牌时（没有上一场战斗记录，或上一场牌组里只有初始牌）本遗物不出现。
    /// 多人下同样不出现：上一局牌组读自本机存档，各端结果可能不一致。
    /// </summary>
    public override bool CanAppear(Player? player)
    {
        if (player?.Creature == null) return false;

        // 多人下不生成该选项：上一局牌组来自本机存档，各端读到的不是同一份数据，
        // 而事件选项要求各端一致（详见类注释第 4 条）。
        if (player.RunState.Players.Count > 1) return false;

        return GetPreviousRunCards(player).Count > 0;
    }

    public override async Task AfterObtained()
    {
        var player = base.Owner;
        if (player?.Creature == null) return;

        var candidates = GetPreviousRunCards(player);
        if (candidates.Count == 0) return;

        Flash();

        var picked = player.PlayerRng.Rewards.NextItem(candidates);
        if (picked == null) return;

        // LoadCard = CardModel.FromSerializable + 注册进本局的卡牌作用域，
        // 这样上一局那张牌的升级等级与附魔会原样带过来。
        var card = player.RunState.LoadCard(picked, player);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(card, PileType.Deck), 2f);
    }

    /// <summary>
    /// 取「上一局」牌组里除初始牌以外的牌。
    ///
    /// 「上一局」= 运行历史里 <c>StartTime</c> 最大的那一条（历史文件名就是 <c>{StartTime}.run</c>）。
    /// 任何一步读不到（没有历史、文件损坏、没有对应的玩家）都返回空列表：
    /// 选项判定与拾起效果都按「没有牌」处理，不会报错。
    /// </summary>
    public static List<SerializableCard> GetPreviousRunCards(Player? player)
    {
        var result = new List<SerializableCard>();
        if (player?.Creature == null) return result;

        var saveManager = SaveManager.Instance;

        // 历史文件名为 "{StartTime}.run"，取 StartTime 最大的那一条。
        long latestStartTime = long.MinValue;
        string? latestFile = null;
        foreach (var fileName in saveManager.GetAllRunHistoryNames())
        {
            var stem = fileName.Contains('.') ? fileName[..fileName.IndexOf('.')] : fileName;
            if (!long.TryParse(stem, out var startTime)) continue;
            if (startTime <= latestStartTime) continue;
            latestStartTime = startTime;
            latestFile = fileName;
        }
        if (latestFile == null) return result;

        var readResult = saveManager.LoadRunHistory(latestFile);
        var history = readResult.SaveData;
        if (!readResult.Success || history == null) return result;

        var previousPlayer = PickPreviousRunPlayer(history, player);
        if (previousPlayer == null) return result;

        foreach (var card in previousPlayer.Deck)
        {
            var id = card?.Id;
            if (id == null) continue;

            // 用 GetByIdOrNull 而不是 SaveUtil.CardOrDeprecated：已经被移除的旧卡牌（DeprecatedCard）
            // 不应该再被复制进牌组，这里直接跳过。
            var model = ModelDb.GetByIdOrNull<CardModel>(id);
            if (model == null) continue;

            // 初始牌（打击 / 防御 / 角色初始牌）的稀有度都是 Basic。
            if (model.Rarity == CardRarity.Basic) continue;
            if (model.Rarity == CardRarity.Curse) continue;
            if (model.Rarity == CardRarity.Quest) continue;

            result.Add(card!);
        }

        return result;
    }

    /// <summary>
    /// 在上一局的历史里找出「本玩家」对应的记录。
    /// 单人局只有一条记录，直接用它；多人局优先取与本局该玩家同角色的那条，找不到再退回第一条。
    /// </summary>
    private static RunHistoryPlayer? PickPreviousRunPlayer(RunHistory history, Player player)
    {
        var players = history.Players;
        if (players.Count == 0) return null;
        if (players.Count == 1) return players[0];

        var characterId = player.Character.Id;
        return players.FirstOrDefault(p => p.Character == characterId) ?? players[0];
    }
}
