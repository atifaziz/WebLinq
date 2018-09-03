namespace WebLinq.Tests
{
    using System.Collections.Generic;
    using System.Linq;

    static class Extensions
    {
        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] items) =>
            source.Except(items.AsEnumerable());
    }
}