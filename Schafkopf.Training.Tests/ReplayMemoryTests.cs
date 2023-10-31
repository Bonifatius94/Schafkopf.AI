namespace Schafkopf.Training.Tests;

public class ReplayMemoryTests
{
    [Fact]
    public void Test_CanFillCacheUntilOverflow()
    {
        var memory = new ReplayMemory(100);

        for (int i = 0; i < 50; i++)
            memory.Append(new SarsExp());

        Assert.Equal(50, memory.Size);

        for (int i = 0; i < 50; i++)
            memory.Append(new SarsExp());

        Assert.Equal(100, memory.Size);
    }

    [Fact]
    public void Test_CanInsertIntoOverflowingCache()
    {
        var memory = new ReplayMemory(100);

        for (int i = 0; i < 200; i++)
            memory.Append(new SarsExp());

        Assert.Equal(100, memory.Size);
    }

    [Fact(Skip = "requires the states to be initialized with unique data")]
    public void Test_CanReplaceOverflowingDataWithNewData()
    {
        var memory = new ReplayMemory(100);
        var overflowingData = Enumerable.Range(0, 50).Select(x => new SarsExp()).ToArray();
        var insertedData = Enumerable.Range(0, 100).Select(x => new SarsExp()).ToArray();

        foreach (var exp in overflowingData)
            memory.Append(exp);
        foreach (var exp in insertedData)
            memory.Append(exp);

        Assert.True(overflowingData.All(exp => !memory.Contains(exp)));
        Assert.True(insertedData.All(exp => memory.Contains(exp)));
    }
}
