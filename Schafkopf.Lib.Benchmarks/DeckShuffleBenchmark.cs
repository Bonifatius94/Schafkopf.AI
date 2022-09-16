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
