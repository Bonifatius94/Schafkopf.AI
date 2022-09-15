using Schafkopf.Lib;

namespace System.Linq;

public static class PickRandomEx
{
    private static readonly Random rng = new Random();

    public static T PickRandom<T>(this IEnumerable<T> items)
        => items.ElementAt(rng.Next(items.Count()));

    public static IEnumerable<T> RandomSubset<T>(
            this IEnumerable<T> items, int count)
        => new EqualDistPermutator_256(items.Count())
            .NextPermutation().Take(count)
            .Select(i => items.ElementAt(i));
}

public static class PowerSetEx
{
    public static IEnumerable<IEnumerable<T>> PowerSet<T>(this IEnumerable<T> items)
    {
        if (!items.Any())
            yield return items;
        else
        {
            var head = items.First();
            var powerset = items.Skip(1).PowerSet().ToList();
            foreach(var set in powerset)
                yield return set.Prepend(head);
            foreach(var set in powerset)
                yield return set;
        }
    }
}
