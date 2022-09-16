namespace Schafkopf.Lib.Benchmarks;

[MemoryDiagnoser(false)]
public class DeckShuffleBenchmark
{
    private static readonly CardsDeck deck = new CardsDeck();

    [Benchmark(Baseline = true)]
    public void SimpleShuffle() => deck.ShuffleSimple();

    [Benchmark]
    public void SimdShuffle() => deck.Shuffle();
}

public class DeckAttributesBenchmark
{
    const int decksCount = 1024;
    private CardsDeck[] decks = new CardsDeck[decksCount];

    [GlobalSetup]
    public void Init()
    {
        var call = GameCall.Solo(0, CardColor.Schell);
        foreach (int i in Enumerable.Range(0, decksCount))
        {
            var deck = new CardsDeck();
            deck.Shuffle();
            decks[i] = deck;
        }
    }

    [Benchmark]
    public void Baseline_InitialHands()
    {
        foreach (var deck in decks)
            deck.InitialHands();
    }

    [Benchmark]
    public void Simple_InitialHands()
    {
        foreach (var deck in decks)
            deck.InitialHandsSimple();
    }
}
