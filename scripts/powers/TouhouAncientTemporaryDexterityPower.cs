using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

public abstract class TouhouAncientTemporaryDexterityPower : TouhouAncientTemporaryPower
{
    public override LocString Description
    {
        get
        {
            return new LocString("powers",
                this.IsPositive ? "TEMPORARY_DEXTERITY_POWER.description" : "TEMPORARY_DEXTERITY_DOWN.description");
        }
    }

    protected override string SmartDescriptionLocKey
    {
        get
        {
            return !this.IsPositive
                ? "TEMPORARY_DEXTERITY_DOWN.smartDescription"
                : "TEMPORARY_DEXTERITY_POWER.smartDescription";
        }
    }
    

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            List<IHoverTip> items = new List<IHoverTip>();
            List<IHoverTip> hoverTipList = items;
            IEnumerable<IHoverTip> collection;
            switch (this.OriginModel)
            {
                case CardModel card:
                    collection = [HoverTipFactory.FromCard(card)];
                    break;
                case PotionModel model:
                    collection = [HoverTipFactory.FromPotion(model)];
                    break;
                case RelicModel relic:
                    collection = HoverTipFactory.FromRelic(relic);
                    break;
                default:
                    throw new InvalidOperationException();
            }

            hoverTipList.AddRange(collection);
            items.Add(HoverTipFactory.FromPower<DexterityPower>());
            return items.ToArray();
        }
    }
    
    
    public override async Task BeforeApplied(
        Creature target,
        Decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (ShouldIgnoreNextInstance)
        {
            ShouldIgnoreNextInstance = false;
        }
        else
        {
            DexterityPower? strengthPower =
                await PowerCmd.Apply<DexterityPower>(target, (Decimal)this.Sign * amount, applier, cardSource, true);
        }
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        Decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        var temporaryDexterityPower = this;
        if (amount == (Decimal)temporaryDexterityPower.Amount || power != temporaryDexterityPower)
            return;
        if (temporaryDexterityPower.ShouldIgnoreNextInstance)
        {
            temporaryDexterityPower.ShouldIgnoreNextInstance = false;
        }
        else
        {
            DexterityPower? strengthPower = await PowerCmd.Apply<DexterityPower>(temporaryDexterityPower.Owner,
                (Decimal)temporaryDexterityPower.Sign * amount, applier, cardSource, true);
        }
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        var power = this;
        if (side != power.Owner.Side)
            return;
        power.Flash();
        await PowerCmd.Remove((PowerModel)power);
        DexterityPower strengthPower = await PowerCmd.Apply<DexterityPower>(power.Owner,
            (Decimal)(-power.Sign * power.Amount), power.Owner, (CardModel)null);
    }
}