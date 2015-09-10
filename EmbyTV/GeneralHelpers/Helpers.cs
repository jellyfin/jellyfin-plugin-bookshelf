using EmbyTV.TunerHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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
