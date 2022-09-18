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
        var cards = new Card[] {
            new Card(CardType.Sau, (CardColor)(((int)gsuchteFarbe + 1) % 4)),
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
}

public class TestCallGenerator_GenerateSoloOrWenzCalls_NoPreviousCalls
{
    [Fact]
    public void Test_CanPlayWenzOrSolo_Always()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = deck.InitialHands();

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(0, initialHands, GameCall.Weiter()).ToArray();

        possCalls.Where(x => x.Mode == GameMode.Wenz && x.IsTout).Should().HaveCount(1);
        possCalls.Where(x => x.Mode == GameMode.Wenz && !x.IsTout).Should().HaveCount(1);
        possCalls.Where(x => x.Mode == GameMode.Solo && x.IsTout).Should().HaveCount(4);
        possCalls.Where(x => x.Mode == GameMode.Solo && !x.IsTout).Should().HaveCount(4);
    }
}

public class TestCallComperator
{
    #region Init

    private static readonly List<List<GameCall>> callsCategoriesAsc =
        new List<List<GameCall>>() {
            new List<GameCall>() { GameCall.Weiter() },
            new List<GameCall>() {
                GameCall.Sauspiel(0, 1, CardColor.Schell),
                GameCall.Sauspiel(3, 2, CardColor.Gras),
                GameCall.Sauspiel(2, 0, CardColor.Eichel),
                GameCall.Sauspiel(1, 3, CardColor.Eichel),
            },
            new List<GameCall>() {
                GameCall.Wenz(0),
                GameCall.Wenz(1),
                GameCall.Wenz(2),
                GameCall.Wenz(3),
            },
            new List<GameCall>() {
                GameCall.Wenz(0, isTout: true),
                GameCall.Wenz(1, isTout: true),
                GameCall.Wenz(2, isTout: true),
                GameCall.Wenz(3, isTout: true),
            },
            new List<GameCall>() {
                GameCall.Solo(0, CardColor.Schell),
                GameCall.Solo(1, CardColor.Herz),
                GameCall.Solo(2, CardColor.Gras),
                GameCall.Solo(3, CardColor.Eichel),
            },
            new List<GameCall>() {
                GameCall.Solo(0, CardColor.Schell, isTout: true),
                GameCall.Solo(1, CardColor.Herz, isTout: true),
                GameCall.Solo(2, CardColor.Gras, isTout: true),
                GameCall.Solo(3, CardColor.Eichel, isTout: true),
            },
        };

    #endregion Init

    public static IEnumerable<object[]> Categories
        => Enumerable.Range(0, callsCategoriesAsc.Count)
            .Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(Categories))]
    public void Test_CategoricalOrderHolds(int category)
    {
        var lowerCategories = callsCategoriesAsc.Take(category);
        var equals = callsCategoriesAsc.ElementAt(category);
        var higherCategories = callsCategoriesAsc
            .Except(new List<List<GameCall>>() { equals })
            .Except(lowerCategories);
        var lowers = lowerCategories.SelectMany(c => c);
        var highers = higherCategories.SelectMany(c => c);

        var callComp = new GameCallComparer();
        lowers.Should().Match(x => x.All(l =>
            equals.All(e => callComp.Compare(l, e) < 0)));
        equals.Should().Match(x => x.All(e1 =>
            equals.All(e2 => callComp.Compare(e1, e2) == 0)));
        highers.Should().Match(x => x.All(h =>
            equals.All(e => callComp.Compare(e, h) < 0)));
    }
}

public class TestCallGenerator_CallRankFilter
{
    [Fact]
    public void Test_CannotCallSauspiel_WhenOtherPlayerCalledAlready()
    {
        var deck = new CardsDeck();
        deck.Shuffle();
        var initialHands = deck.InitialHands();
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
        var initialHands = deck.InitialHands();
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
        var initialHands = deck.InitialHands();
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
        var initialHands = deck.InitialHands();
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
        var initialHands = deck.InitialHands();
        var previousCall = GameCall.Solo(0, CardColor.Schell, isTout: true);

        var callGen = new GameCallGenerator();
        var possCalls = callGen.AllPossibleCalls(2, initialHands, previousCall).ToArray();

        possCalls.Should().BeEquivalentTo(new List<GameCall>() { GameCall.Weiter() });
    }
}
