namespace Schafkopf.Lib.Benchmarks;

[MemoryDiagnoser(false)]
public class HandAttributesBenchmark
{
    const int handsCount = 1024;
    private Hand[] hands = new Hand[handsCount];

    private static readonly CardColor[] farben =
        new CardColor[] { CardColor.Schell, CardColor.Gras, CardColor.Eichel };

    [GlobalSetup]
    public void Init()
    {
        var deck = new CardsDeck();
        var call = GameCall.Solo(0, CardColor.Schell);
        foreach (int i in Enumerable.Range(0, handsCount / 4))
        {
            deck.Shuffle();
            var deckHands = deck.InitialHands(call);
            int offset = i * 4;
            hands[offset] = deckHands[0];
            hands[offset + 1] = deckHands[1];
            hands[offset + 2] = deckHands[2];
            hands[offset + 3] = deckHands[3];
        }
    }

    [Benchmark]
    public void Baseline_HasFarbe()
    {
        foreach (var hand in hands)
            foreach (var farbe in farben)
                hand.HasFarbe(farbe);
    }

    [Benchmark]
    public void Simple_HasFarbe()
    {
        foreach (var hand in hands)
            foreach (var farbe in farben)
                hand.HasFarbeSimple(farbe);
    }

    [Benchmark]
    public void Baseline_HasTrumpf()
    {
        foreach (var hand in hands)
            hand.HasTrumpf();
    }

    [Benchmark]
    public void Simple_HasTrumpf()
    {
        foreach (var hand in hands)
            hand.HasTrumpfSimple();
    }

    [Benchmark]
    public void Baseline_FarbeCount()
    {
        foreach (var hand in hands)
            foreach (var farbe in farben)
                hand.FarbeCount(farbe);
    }

    [Benchmark]
    public void Simple_FarbeCount()
    {
        foreach (var hand in hands)
            foreach (var farbe in farben)
                hand.FarbeCountSimple(farbe);
    }
}
