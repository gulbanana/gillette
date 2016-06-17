using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gillette
{
    static class TypeExtensions
    {
        public static string SanitisedName(this Type type)
        {
            var shortName = type.Name.Replace("[]", "Array");
            if (type == typeof(object) || shortName.Contains("AnonymousType")) shortName = "Dynamic";
            return shortName;
        }
    }
}
