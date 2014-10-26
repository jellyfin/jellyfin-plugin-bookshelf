using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgusTV.DataContracts.Tuning
{
    /// <exclude/>
    public enum GuardInterval
    {
        /// <exclude/>
        GuardNotSet = -1,
        /// <exclude/>
        GuardNotDefined = 0,
        /// <exclude/>
        Guard1_32 = 1,
        /// <exclude/>
        Guard1_16 = 2,
        /// <exclude/>
        Guard1_8 = 3,
        /// <exclude/>
        Guard1_4 = 4,
        /// <exclude/>
        GuardMax = 5,
    }
}
