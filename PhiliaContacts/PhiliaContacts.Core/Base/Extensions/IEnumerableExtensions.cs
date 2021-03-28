using System;
using System.Collections.Generic;

namespace PhiliaContacts.Core.Base.Extensions
{
    public static class IEnumerableExtensions
    {
        public static void IterateAndAct<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }
    }
}
