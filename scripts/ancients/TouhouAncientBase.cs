using System;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.Models;

namespace TouhouAncients.Scripts;

public abstract class TouhouAncientBase : CustomAncientModel
{
    public abstract int? ShowAct { get; }

    public override bool IsValidForAct(ActModel act)
    {
        if (TouhouAncientsConfig.IsAncientBanned(this))
        {
            return false;
        }
        if (!ShowAct.HasValue)
        {
            return base.IsValidForAct(act);
        }

        return act.ActNumber() == ShowAct.Value;
    }
    public override bool ShouldForceSpawn(ActModel act, AncientEventModel? rngChosenAncient)
    {
        return TouhouAncientsConfig.IsAncientForced(this, act.ActNumber());
    }
}