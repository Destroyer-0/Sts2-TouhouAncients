using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace TouhouAncients.Scripts.relics;

[Pool(typeof(SharedRelicPool))]
public class CrimsonChalice : TouhouAncientRelics
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new MaxHpVar(0)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromCardWithCardHoverTips<Decay>();
    
    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var player = base.Owner;

        // 1. 体力上限翻倍
        var currentMaxHp = player.Creature.MaxHp;
        base.DynamicVars.MaxHp.BaseValue = currentMaxHp;
        await CreatureCmd.GainMaxHp(player.Creature, currentMaxHp);

        // 2. 加入4张腐朽
        var results = new List<CardPileAddResult>(4);
        for (var i = 0; i < 4; i++)
        {
            var decay = player.RunState.CreateCard(ModelDb.Card<Decay>(), player);
            results.Add(await CardPileCmd.Add(decay, PileType.Deck));
        }
        CardCmd.PreviewCardPileAdd(results, 2f);
    }
}
