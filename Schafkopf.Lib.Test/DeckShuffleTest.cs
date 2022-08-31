using System.Collections.Concurrent;
using FluentAssertions;

namespace Schafkopf.Lib.Test;

public class DeckSchuffleTest
{
    #region Helpers

    private (int, int) asTuple_2(IList<int> perm)
        => (perm.ElementAt(0), perm.ElementAt(1));

    private (int, int, int, int, int) asTuple_5(IList<int> perm)
        => (perm.ElementAt(0), perm.ElementAt(1), perm.ElementAt(2), perm.ElementAt(3), perm.ElementAt(4));

    private (int, int, int, int, int, int, int) asTuple_7(IList<int> perm)
        => (perm.ElementAt(0), perm.ElementAt(1), perm.ElementAt(2), perm.ElementAt(3),
            perm.ElementAt(4), perm.ElementAt(5), perm.ElementAt(6));

    private int fact(int n) => Enumerable.Range(1, n).Aggregate((x, y) => x * y);

    private void incrementCount<T>(ConcurrentDictionary<T, int> dict, T key)
        => dict.AddOrUpdate(key, (perm) => 1, (perm, count) => count + 1);

    #endregion Helpers

    [Fact]
    public void Test_YieldsEquallyDistPermutations_2()
    {
        int numItems = 2;
        int numPerms = fact(numItems);
        const int numDraws = 100000;
        var permGen = new EqualDistPermutator(numItems);
        var permCounts = new ConcurrentDictionary<(int, int), int>();

        for (int i = 0; i < numDraws; i++)
            incrementCount(permCounts, asTuple_2(permGen.NextPermutation().ToList()));

        permCounts.Average(x => (double)x.Value / numDraws)
            .Should().BeApproximately(1.0 / numPerms, 0.01);
        double entropy = permCounts
            .Select(count => (double)count.Value / numDraws)
            .Select(p => -1 * p * Math.Log(p, numPerms)).Sum();
        entropy.Should().BeGreaterThan(0.99);
    }

    [Fact]
    public void Test_YieldsEquallyDistPermutations_5()
    {
        int numItems = 5;
        int numPerms = fact(numItems);
        const int numDraws = 1000000;
        var permGen = new EqualDistPermutator(numItems);
        var permCounts = new ConcurrentDictionary<(int, int, int, int, int), int>();

        for (int i = 0; i < numDraws; i++)
            incrementCount(permCounts, asTuple_5(permGen.NextPermutation().ToList()));

        var relProbs = permCounts.Select(count =>
            (double)count.Value / numDraws).ToList();
        relProbs.Average().Should().BeApproximately(1.0 / numPerms, 0.01);
        double entropy = relProbs.Select(p => -1 * p * Math.Log(p, numPerms)).Sum();
        entropy.Should().BeGreaterThan(0.99);
    }

    [Fact]
    public void Test_YieldsEquallyDistPermutations_7()
    {
        int numItems = 7;
        int numPerms = fact(numItems);
        const int numDraws = 10000000;
        var permGen = new EqualDistPermutator(numItems);
        var permCounts = new ConcurrentDictionary<(int, int, int, int, int, int, int), int>();

        for (int i = 0; i < numDraws; i++)
            incrementCount(permCounts, asTuple_7(permGen.NextPermutation().ToList()));

        var relProbs = permCounts.Select(count =>
            (double)count.Value / numDraws).ToList();
        relProbs.Average().Should().BeApproximately(1.0 / numPerms, 0.01);
        double entropy = relProbs.Select(p => -1 * p * Math.Log(p, numPerms)).Sum();
        entropy.Should().BeGreaterThan(0.99);
    }
}
