using System;
using System.Collections.Generic;
using System.Linq;

namespace Metricano.PostSharpAspects
{
    internal static class Extensions
    {
        public static string ToCsv(this IEnumerable<string> values)
        {
            return string.Join(",", values);
        }

        public static string ToCsv<T>(this IEnumerable<T> values)
        {
            return ToCsv(values.Select(v => v.ToString()));
        }

        public static string ToCsv<T>(this IEnumerable<T> values, Func<T, string> toString)
        {
            return ToCsv(values.Select(toString));
        }
    }
}