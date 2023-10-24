namespace Schafkopf.Lib.Test;

public class TestCallGenerator_GenerateSauspielCalls_NoPreviousCalls
{
    private static ISet<CardColor> rufbareSauspielFarben =
        new HashSet<CardColor>() { CardColor.Schell, CardColor.Gras, CardColor.Eichel };

    public static IEnumerable<object[]> RufbareFarbenPerms
        => rufbareSauspielFarben.PowerSet()
            .Select(set => new object[] { set });

    [Theory]
    [MemberData(nameof(RufbareFarbenPerms))]
    public void Test_CanCallGsuchteOfFarben_GivenRufbareFarben(
        IEnumerable<CardColor> rufbareFarben)
    {
        var cards = rufbareSauspielFarben
            .Select(f => (rufbareFarben.Contains(f)
                ? CardType.Sieben : CardType.Sau, f))
            .Select(t => new Card(t.Item1, t.Item2))
            .ToArray();
        var otherCards = new Card[] {
            new Card(CardType.Ober, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Eichel),
        };
        cards = cards.Concat(otherCards).ToArray();
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(hand).Chunk(8).Select(h => new Hand(h));
        var initialHands = new Hand[] { hand }.Concat(otherHands).ToArray();

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(0, initialHands, GameCall.Weiter());

        var possSauspiele = possCalls.ToArray().Where(c => c.Mode == GameMode.Sauspiel);
        possSauspiele.Select(x => x.GsuchteFarbe).Should().BeEquivalentTo(rufbareFarben);
    }

    public static IEnumerable<object[]> AllRufbareFarben
        => rufbareSauspielFarben.Select(x => new object[] { x });
    [Theory]
    [MemberData(nameof(AllRufbareFarben))]
    public void Test_CannotCallSauspiel_WhenGesperrt(
        CardColor gsuchteFarbe)
    {
        var cards = new Card[] {
            new Card(CardType.Sau, gsuchteFarbe),
            new Card(CardType.Ober, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Zehn, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Eichel),
        };
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(hand).Chunk(8).Select(h => new Hand(h));
        var initialHands = new Hand[] { hand }.Concat(otherHands).ToArray();

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(0, initialHands, GameCall.Weiter()).ToArray();

        possCalls.Should().Match(x => x.All(call => call.Mode != GameMode.Sauspiel));
    }

    [Theory]
    [MemberData(nameof(AllRufbareFarben))]
    public void Test_CannotCallSauspiel_WhenFreiAufFarbe(
        CardColor gsuchteFarbe)
    {
        var rufbareFarben = new CardColor[] { CardColor.Schell, CardColor.Gras, CardColor.Eichel };
        var sauFarbe = rufbareFarben.Except(new CardColor[] { gsuchteFarbe }).First();
        var cards = new Card[] {
            new Card(CardType.Sau, sauFarbe),
            new Card(CardType.Ober, CardColor.Eichel),
            new Card(CardType.Ober, CardColor.Gras),
            new Card(CardType.Sau, CardColor.Herz),
            new Card(CardType.Koenig, CardColor.Herz),
            new Card(CardType.Zehn, CardColor.Herz),
            new Card(CardType.Neun, CardColor.Herz),
            new Card(CardType.Unter, CardColor.Eichel),
        };
        var hand = new Hand(cards);
        var otherHands = CardsDeck.AllCards.Except(cards).Chunk(8).Select(h => new Hand(h));
        var initialHands = new Hand[] { hand }.Concat(otherHands).ToArray();

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(0, initialHands, GameCall.Weiter()).ToArray();

        possCalls.Should().Match(x => x.All(call => call.Mode != GameMode.Sauspiel));
    }
}

public class TestCallGenerator_GenerateSoloOrWenzCalls_NoPreviousCalls
{
    [Fact]
    public void Test_CanPlayWenzOrSolo_Always()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(initialHands);

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(0, initialHands, GameCall.Weiter()).ToArray();

        possCalls.Where(x => x.Mode == GameMode.Wenz && x.IsTout).Should().HaveCount(1);
        possCalls.Where(x => x.Mode == GameMode.Wenz && !x.IsTout).Should().HaveCount(1);
        possCalls.Where(x => x.Mode == GameMode.Solo && x.IsTout).Should().HaveCount(4);
        possCalls.Where(x => x.Mode == GameMode.Solo && !x.IsTout).Should().HaveCount(4);
    }
}

public class TestCallGenerator_CallRankFilter
{
    [Fact]
    public void Test_CannotCallSauspiel_WhenOtherPlayerCalledAlready()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(initialHands);
        var previousCall = GameCall.Sauspiel(0, 1, CardColor.Schell);

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(2, initialHands, previousCall).ToArray();

        possCalls.Should().Match(calls =>
            calls.All(c => c.Mode != GameMode.Sauspiel));
    }

    [Fact]
    public void Test_CannotCallWenz_WhenOtherPlayerCalledAlreadyWenzOrHigher()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(initialHands);
        var previousCall = GameCall.Wenz(0);

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(2, initialHands, previousCall).ToArray();

        possCalls.Should().Match(calls =>
            calls.All(c => c.Mode != GameMode.Wenz || c.IsTout));
    }

    [Fact]
    public void Test_CannotCallWenzTout_WhenOtherPlayerCalledAlreadyWenzToutOrHigher()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(initialHands);
        var previousCall = GameCall.Wenz(0, isTout: true);

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(2, initialHands, previousCall).ToArray();

        possCalls.Should().Match(calls =>
            calls.All(c => c.Mode != GameMode.Wenz));
    }

    [Fact]
    public void Test_CannotCallSolo_WhenOtherPlayerCalledAlreadySoloOrHigher()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(initialHands);
        var previousCall = GameCall.Solo(0, CardColor.Schell);

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(2, initialHands, previousCall).ToArray();

        possCalls.Should().Match(calls =>
            calls.All(c => c.Mode != GameMode.Solo || c.IsTout));
    }

    [Fact]
    public void Test_CannotCallAnything_WhenOtherPlayerCalledAlreadySoloTout()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = new Hand[4];
        deck.InitialHands(initialHands);
        var previousCall = GameCall.Solo(0, CardColor.Schell, isTout: true);

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(2, initialHands, previousCall).ToArray();

        possCalls.Should().BeEquivalentTo(new List<GameCall>() { GameCall.Weiter() });
    }
}
