using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ArgusTV.DataContracts.Tuning
{
    /// <exclude/>
    public enum TransmissionMode
    {
        /// <exclude/>
        ModeNotSet = -1,
        /// <exclude/>
        ModeNotDefined = 0,
        /// <exclude/>
        Mode2K = 1,
        /// <exclude/>
        Mode8K = 2,
        /// <exclude/>
        Mode4K = 3,
        /// <exclude/>
        Mode2KInterleaved = 4,
        /// <exclude/>
        Mode4KInterleaved = 5,
        /// <exclude/>
        ModeMax = 6,
    }
}
