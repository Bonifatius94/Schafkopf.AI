namespace Schafkopf.Training;

public struct SarsExp : IEquatable<SarsExp>
{
    public SarsExp() { }

    public GameState StateBefore = new GameState();
    public GameState StateAfter = new GameState();
    public GameAction Action = new GameAction();
    public double Reward = 0.0;
    public bool IsTerminal = false;

    public bool Equals(SarsExp other)
        => StateBefore.Equals(other.StateBefore)
            && StateAfter.Equals(other.StateAfter)
            && Action.Equals(other.Action)
            && Reward == other.Reward
            && IsTerminal == other.IsTerminal;

    public override int GetHashCode() => 0;
}

public class ReplayMemory
{
    private static readonly Random rng = new Random();

    public ReplayMemory(int size)
    {
        totalSize = size;
        memory = new SarsExp[totalSize];
        for (int i = 0; i < size; i++)
            memory[i] = new SarsExp();
    }

    private int totalSize;
    private bool isFilled = false;
    private bool overflow = false;
    private int insertPos = 0;
    private SarsExp[] memory;

    public int Size => isFilled ? totalSize : insertPos;

    public void Append(SarsExp exp)
    {
        var origExp = memory[insertPos];
        origExp.StateBefore.LoadFeatures(exp.StateBefore.State);
        origExp.StateAfter.LoadFeatures(exp.StateAfter.State);
        memory[insertPos++] = origExp;

        overflow = insertPos == totalSize;
        isFilled = isFilled || overflow;
        insertPos %= totalSize;
    }

    public void AppendBatched(SarsExp[] exps)
    {
        bool overflow = false;
        foreach (var exp in exps)
        {
            Append(exp);
            overflow |= this.overflow;
        }
        this.overflow = overflow;
    }

    public void SampleRandom(SarsExp[] cache)
    {
        int maxId = isFilled ? totalSize : insertPos;
        for (int i = 0; i < cache.Length; i++)
            cache[i] = memory[rng.Next() % maxId];
    }

    public void SampleBatched(SarsExp[] cache)
    {
        int maxId = isFilled ? totalSize : insertPos;
        int offset = rng.Next(0, totalSize - cache.Length);
        for (int i = 0; i < cache.Length; i++)
            cache[i] = memory[(offset + i) % maxId];
    }

    public void Shuffle()
    {
        for (int i = 0; i < totalSize; i++)
        {
            int j = rng.Next(i, totalSize);
            var temp = memory[j];
            memory[j] = memory[i];
            memory[i] = temp;
        }
    }

    public bool Contains(SarsExp exp)
        => memory.Contains(exp);
}
