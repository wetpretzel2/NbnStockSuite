using System;
using System.Collections.Generic;
using System.Text;

namespace NbnStock.Core.Models
{
    public enum UnitStatus
    {
        OnHand,
        Installed,
        EwastePendingSubmission,
        EwasteAwaitingApproval,
        ApprovedForDisposal,
        Disposed

    }
}

