using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using TouhouAncients.Scripts.cardTags;
using TouhouAncients.Scripts.powers;

namespace TouhouAncients.Scripts.relics;

/// <summary>
/// 阴阳玉：转换。阴：每当你打出一张技能牌时，获得1临时力量。
/// 阳：每当你打出一张攻击牌时，获得1临时敏捷。
/// </summary>
[Pool(typeof(SharedRelicPool))]
public class YinYangOrb : TouhouAncientRelics
{

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    /// <summary>
    /// 打出牌时根据当前模式触发效果。
    /// </summary>
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != base.Owner) return;

        if (cardPlay.Card.Type == CardType.Skill)
        {
            // 阴：获得1临时力量
            Flash();
            await PowerCmd.Apply<YinYangOrbStrengthPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
        }
        else if (cardPlay.Card.Type == CardType.Attack)
        {
            // 阳：获得1临时敏捷
            Flash();
            await PowerCmd.Apply<YinYangOrbDexterityPower>(base.Owner.Creature, 1m, base.Owner.Creature, null);
        }
    }
}
