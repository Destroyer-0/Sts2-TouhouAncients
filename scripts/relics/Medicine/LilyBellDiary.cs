using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using TouhouAncients.Scripts.cards;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 铃色的日记本：
/// 在每场战斗开始时，将 CardCount 张铃兰加入你的手牌。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class LilyBellDiary : TouhouAncientRelics
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<LilyBell>();
    
    public override async Task BeforeCombatStart()
    {
        var player = base.Owner;

        // 创建梦想封印并加入牌组
        var card = player.RunState.CreateCard(ModelDb.Card<LilyBell>(), player);
        var result = await CardPileCmd.Add(card, PileType.Deck);
        CardCmd.PreviewCardPileAdd(result);
    }
}
