namespace Schafkopf.Lib.Benchmarks;

[MemoryDiagnoser(false)]
public class CardsComparerBenchmark
{
    const int pairsCount = 1024;
    private (Card, Card)[] cardPairs = new (Card, Card)[pairsCount];
    private CardComparer comp;

    [GlobalSetup]
    public void Init()
    {
        var call = GameCall.Solo(0, CardColor.Schell);
        var deck = new CardsDeck();
        comp = new CardComparer(call.Mode, call.Trumpf);
        foreach (int i in Enumerable.Range(0, pairsCount / 16))
        {
            deck.Shuffle();
            for (int j = 0; j < 16; j++)
                cardPairs[i] = (deck[j], deck[j+1]);
        }
    }

    [Benchmark(Baseline = true)]
    public void Simd_Compare()
    {
        foreach ((var c1, var c2) in cardPairs)
            comp.CompareSimd(c1, c2);
    }

    [Benchmark]
    public void Simple_Compare()
    {
        foreach ((var c1, var c2) in cardPairs)
            comp.Compare(c1, c2);
    }
}
