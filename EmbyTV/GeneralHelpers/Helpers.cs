using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EmbyTV.TunerHost;

namespace EmbyTV.GeneralHelpers
{
    public static class Helpers
    {
        public static IEnumerable<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal) && typeof(ITunerHost).IsAssignableFrom(t) && t.IsPublic);
        }

    }
}
