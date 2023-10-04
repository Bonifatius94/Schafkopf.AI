namespace Schafkopf.Lib.Benchmarks;

[MemoryDiagnoser(false)]
public class GameCallBenchmark
{
    private const int numHands = 1024;
    private List<Hand[]> hands = new List<Hand[]>();

    private GameCallGenerator callGen = new GameCallGenerator();

    [GlobalSetup]
    public void Init()
    {
        var deck = new CardsDeck();
        for (int i = 0; i < numHands; i++)
        {
            deck.Shuffle();
            var initHands = new Hand[4];
            deck.InitialHands(initHands);
            hands.Add(initHands);
        }
    }

    [Benchmark(Baseline = true)]
    public void Simd_AllPossibleCalls()
    {
        var lastCall = GameCall.Weiter();
        foreach (var h in hands)
            for (int id = 0; id < 4; id++)
                callGen.AllPossibleCalls(id, h, lastCall);
    }

    [Benchmark]
    public void Simple_AllPossibleCalls()
    {
        var lastCall = GameCall.Weiter();
        foreach (var h in hands)
            for (int id = 0; id < 4; id++)
                callGen.AllPossibleCallsSimple(id, h, lastCall);
    }
}
