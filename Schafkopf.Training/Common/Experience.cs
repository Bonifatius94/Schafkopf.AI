namespace Schafkopf.Training;

public struct SarsExp : IEquatable<SarsExp>
{
    public SarsExp() { }

    public GameState StateBefore = new GameState();
    public GameState StateAfter = new GameState();
    public Card Action = new Card();
    public double Reward = 0.0;
    public bool IsTerminal = false;

    public bool Equals(SarsExp other)
        => StateBefore.Equals(other.StateBefore)
            && StateAfter.Equals(other.StateAfter)
            && Action == other.Action
            && Reward == other.Reward
            && IsTerminal == other.IsTerminal;

    public override int GetHashCode() => 0;
}

public struct PPOExp : IEquatable<PPOExp>
{
    public PPOExp() { }

    public GameState StateBefore = new GameState();
    public Card Action = new Card();
    public double Reward = 0.0;
    public bool IsTerminal = false;
    public double OldProb = 0.0;
    public double OldBaseline = 0.0;

    public bool Equals(PPOExp other)
        => StateBefore.Equals(other.StateBefore)
            && Action == other.Action
            && Reward == other.Reward
            && IsTerminal == other.IsTerminal
            && OldProb == other.OldProb
            && OldBaseline == other.OldBaseline;

    public override int GetHashCode() => 0;
}
