namespace AnythingToMp3Converter.UI.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumerableExtensions
    {
        public static bool IsNull(this IEnumerable source) => source == null;
        public static bool GotAny<T>(this IEnumerable<T> source, Func<T, bool> predicate) => !source.IsNull() && source.Any(predicate);
        public static IEnumerable<TResult> SelectAs<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector) => source.IsNull() ? Enumerable.Empty<TResult>() : source.Select(selector);
    }
}
