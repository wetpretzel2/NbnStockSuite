using System;
using System.Collections.Generic;
using System.Text;

namespace NbnStock.Core.Models
{
    public enum UnitStatus
    {
        OnHand,
        Installed,
        Faulty,
        AwaitingApproval,
        ApprovedForDisposal,
        Disposed
    }
}

