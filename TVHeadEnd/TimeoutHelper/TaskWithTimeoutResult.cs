using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVHeadEnd.TimeoutHelper
{
    public class TaskWithTimeoutResult<T>
    {
        public T Result { get; set; }
        public Boolean HasTimeout { get; set; }
    }
}
