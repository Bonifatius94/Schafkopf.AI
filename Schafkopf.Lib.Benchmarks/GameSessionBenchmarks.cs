using BenchmarkDotNet.Diagnostics.Windows.Configs;

namespace Schafkopf.Lib.Benchmarks;

[MemoryDiagnoser(false)]
#if Windows
[EtwProfiler(performExtraBenchmarksRun: false)]
#endif
public class GameSessionBenchmark
{
    // [Params(100, 10000, 1000000)]
    // public int NumGames;
    private GameSession session;

    [GlobalSetup]
    public void Init()
    {
        var deck = new CardsDeck();
        var table = new Table(
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent()));
        session = new GameSession(table, deck);
    }

    [Benchmark]
    public void PlayGames_10k()
    {
        for (int i = 0; i < 10000; i++)
            session.ProcessGame();
    }
}
