using System;
using System.Collections.Generic;

namespace Feeder.Common.Helpers
{
    public static class UtilExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            //source.ThrowIfNull("source");
            //action.ThrowIfNull("action");
            foreach (var _element in source)
            {
                action(_element);
            }
        }
    }
}
