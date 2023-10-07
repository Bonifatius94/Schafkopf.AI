namespace Schafkopf.Training;

public class SarsExp
{
    public GameState StateBefore { get; init; }
    public GameState StateAfter { get; init; }
    // TODO: think about whether this is a good action representation
    public int Action { get; init; }
    public float Reward { get; init; }
    public bool IsTerminal { get; init; }
}

public class ReplayMemory
{
    private const int memorySize = 10000;

    private bool isFilled = false;
    private int i = 0;
    private SarsExp[] experience = new SarsExp[memorySize];

    public void Append(SarsExp exp)
    {
        experience[i++] = exp;
        isFilled = isFilled || i == memorySize;
        i %= memorySize;
    }
}
