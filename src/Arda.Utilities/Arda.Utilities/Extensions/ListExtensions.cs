using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Arda.Utilities.Extensions
{
    public static class ListExtensions
    {
        public static bool IsNullOrEmpty<T>([AllowNull][NotNullWhen(false)] this IEnumerable<T>? source)
        {
            return source is null || !source.Any();
        }

        public static IEnumerable<T> DiscardNullValues<T>(this IEnumerable<T> source)
        {
            return source.Where(x => x is not null);
        }

        public static IEnumerable<IEnumerable<T>> Partition<T>(this IEnumerable<T> items, int partitionSize)
        {
            return items.Select((item, inx) => new { item, inx })
                        .GroupBy(x => x.inx / partitionSize)
                        .Select(g => g.Select(x => x.item));
        }

        [return: NotNull]
        public static List<T> Flatten<T>(T? source, Func<T, List<T>?> childSelector)
        {
            var result = new List<T>();

            InnerFlatten(source);

            return result;

            void InnerFlatten(T? source)
            {
                if (source is null) return;

                if (result.Contains(source)) return;

                var children = childSelector(source);

                result.Add(source);

                if (!children.IsNullOrEmpty())
                {
                    result.AddRange(children.SelectMany(x => Flatten(x, childSelector)));
                }
            }
        }

        [return: NotNull]
        public static List<T> Flatten<T>(this List<T> source, Func<T, List<T>?> childSelector)
        {
            return source.SelectMany(x => Flatten(x, childSelector)).ToList();
        }

        public static void Nest<T, K>(this List<T> source, Func<T, K> idSelector, Func<T, K?> parentIdSelector, Func<T, List<T>> childSelector)
            where K : IEquatable<K>
        {
            var roots = source.Where(x => parentIdSelector(x) is null);
            foreach (var root in roots)
            {
                InnerFindNested(root);
            }

            void InnerFindNested(T parent)
            {
                var children = childSelector(parent);
                var parentId = idSelector(parent);
                var items = source.Where(x => parentId.Equals(parentIdSelector(x))).ToList();
                children = items;
                foreach (var item in items)
                {
                    source.Remove(item);
                }
                foreach (var item in items)
                {
                    InnerFindNested(item);
                }
            }
        }
    }
}
