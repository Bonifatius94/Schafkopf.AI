namespace Schafkopf.Lib.Benchmarks;

[MemoryDiagnoser(false)]
public class TurnAttributesBenchmark
{
    private const int numTurns = 8192;
    private Turn[] turns = new Turn[numTurns];

    [GlobalSetup]
    public void Setup()
    {
        var deck = new CardsDeck();
        var table = new Table(
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent()));
        var session = new GameSession(table, deck);

        for (int i = 0; i < numTurns / 8; i++)
        {
            var history = session.ProcessGame();
            for (int j = 0; j < 8; j++)
                turns[(i*8) + j] = history.Turns[j];
        }
    }

    [Benchmark]
    public void Simd_WinnerId()
    {
        int foo;
        foreach (var turn in turns)
            foo = turn.WinnerId;
    }

    [Benchmark]
    public void Simple_WinnerId()
    {
        int foo;
        foreach (var turn in turns)
            foo = turn.WinnerIdSimple();
    }

    [Benchmark]
    public void Simd_Augen()
    {
        int foo;
        foreach (var turn in turns)
            foo = turn.Augen;
    }

    [Benchmark]
    public void Simple_Augen()
    {
        int foo;
        foreach (var turn in turns)
            foo = turn.AugenSimple();
    }
}
