using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace TouhouAncients.Scripts.powers;

public abstract class TouhouAncientTemporaryPower : TouhouAncientPowerModel, ITemporaryPower
{
    public abstract AbstractModel OriginModel { get; }

    private bool _shouldIgnoreNextInstance;

    public override PowerType Type => !this.IsPositive ? PowerType.Debuff : PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;


    public PowerModel InternallyAppliedPower => (PowerModel)ModelDb.Power<StrengthPower>();

    protected virtual bool IsPositive => true;

    protected int Sign => !this.IsPositive ? -1 : 1;

    public override LocString Title
    {
        get
        {
            switch (this.OriginModel)
            {
                case CardModel cardModel:
                    return cardModel.TitleLocString;
                case PotionModel potionModel:
                    return potionModel.Title;
                case RelicModel relicModel:
                    return relicModel.Title;
                default:
                    throw new InvalidOperationException();
            }
        }
    }


    public void IgnoreNextInstance() => this._shouldIgnoreNextInstance = true;

    public bool ShouldIgnoreNextInstance
    {
        get => _shouldIgnoreNextInstance;
        set => _shouldIgnoreNextInstance = value;
    }
    
}