namespace Schafkopf.Training;

public class SarsExp<TState, TAction> : IEquatable<SarsExp<TState, TAction>>
    where TState : IEquatable<TState>, new() where TAction : IEquatable<TAction>, new()
{
    public SarsExp() { }

    public TState StateBefore = new TState();
    public TState StateAfter = new TState();
    public TAction Action = new TAction();
    public double Reward = 0.0;
    public bool IsTerminal = false;

    public bool Equals(SarsExp<TState, TAction>? other)
        => other != null
            && StateBefore.Equals(other.StateBefore)
            && StateAfter.Equals(other.StateAfter)
            && Action.Equals(other.Action)
            && Reward == other.Reward
            && IsTerminal == other.IsTerminal;

    public override int GetHashCode() => 0;
}

public class PPOExp<TState, TAction> : IEquatable<PPOExp<TState, TAction>>
    where TState : IEquatable<TState>, new() where TAction : IEquatable<TAction>, new()
{
    public PPOExp() { }

    public TState StateBefore = new TState();
    public TAction Action = new TAction();
    public double Reward = 0.0;
    public bool IsTerminal = false;
    public double OldProb = 0.0;
    public double OldBaseline = 0.0;

    public bool Equals(PPOExp<TState, TAction>? other)
        => other != null
            && StateBefore.Equals(other.StateBefore)
            && Action.Equals(other.Action)
            && Reward == other.Reward
            && IsTerminal == other.IsTerminal
            && OldProb == other.OldProb
            && OldBaseline == other.OldBaseline;

    public override int GetHashCode() => 0;
}
