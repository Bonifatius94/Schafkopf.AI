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
