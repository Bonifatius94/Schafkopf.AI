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
