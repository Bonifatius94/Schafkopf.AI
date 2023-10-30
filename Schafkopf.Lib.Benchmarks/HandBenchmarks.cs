namespace Schafkopf.Lib.Benchmarks;

[MemoryDiagnoser(false)]
public class HandAttributesBenchmark_HasFarbe
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
            var deckHands = new Hand[4];
            deck.InitialHands(call, deckHands);
            int offset = i * 4;
            hands[offset] = deckHands[0];
            hands[offset + 1] = deckHands[1];
            hands[offset + 2] = deckHands[2];
            hands[offset + 3] = deckHands[3];
        }
    }

    [Benchmark(Baseline = true)]
    public void Simd_HasFarbe()
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
}

[MemoryDiagnoser(false)]
public class HandAttributesBenchmark_HasTrumpf
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
            var deckHands = new Hand[4];
            deck.InitialHands(call, deckHands);
            int offset = i * 4;
            hands[offset] = deckHands[0];
            hands[offset + 1] = deckHands[1];
            hands[offset + 2] = deckHands[2];
            hands[offset + 3] = deckHands[3];
        }
    }

    [Benchmark(Baseline = true)]
    public void Simd_HasTrumpf()
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
}

[MemoryDiagnoser(false)]
public class HandAttributesBenchmark_FarbeCount
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
            var deckHands = new Hand[4];
            deck.InitialHands(call, deckHands);
            int offset = i * 4;
            hands[offset] = deckHands[0];
            hands[offset + 1] = deckHands[1];
            hands[offset + 2] = deckHands[2];
            hands[offset + 3] = deckHands[3];
        }
    }

    [Benchmark(Baseline = true)]
    public void Simd_FarbeCount()
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

[MemoryDiagnoser(false)]
public class HandAttributesBenchmark_FirstFourCards
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
            var deckHands = new Hand[4];
            deck.InitialHands(call, deckHands);
            int offset = i * 4;
            hands[offset] = deckHands[0];
            hands[offset + 1] = deckHands[1];
            hands[offset + 2] = deckHands[2];
            hands[offset + 3] = deckHands[3];
        }
    }

    [Benchmark(Baseline = true)]
    public void Simd_FirstFourCards()
    {
        var cache = new Card[4];
        foreach (var hand in hands)
            hand.FirstFour(cache);
    }

    [Benchmark]
    public void Simple_FirstFourCards()
    {
        foreach (var hand in hands)
            hand.Take(4).ToArray();
    }
}
