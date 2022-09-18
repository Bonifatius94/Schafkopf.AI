namespace Schafkopf.Lib.Test;

public class TestGameTable
{
    [Fact]
    public void Test_YieldsAllPlayers_WhenIteratingFromZero()
    {
        var table = new Table(
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent()));

        var playerIter = table.PlayersInDrawingOrder();

        playerIter.Select(x => x.Id).Should()
            .BeEquivalentTo(Enumerable.Range(0, 4));
    }

    public static IEnumerable<object[]> NumShifts
        => Enumerable.Range(0, 5).Select(x => new object[] { x });
    [Theory]
    [MemberData(nameof(NumShifts))]
    public void Test_YieldsAllPlayers_WhenIteratingShifted(int shifts)
    {
        var table = new Table(
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent()));

        for (int i = 0; i < shifts; i++)
            table.Shift();
        var playerIter = table.PlayersInDrawingOrder();

        var expOrder = Enumerable.Range(0, 4).Select(i => (i + shifts) % 4);
        playerIter.Select(x => x.Id).Should()
            .BeEquivalentTo(expOrder, cfg => cfg.WithStrictOrdering());
    }
}

public class TestGameSession
{
    [Fact]
    public void Test_PlaySomeGames()
    {
        var deck = new CardsDeck();
        var table = new Table(
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent()));
        var session = new GameSession(table, deck);

        for (int i = 0; i < 10000; i++)
            session.ProcessGame();
    }

    public static IEnumerable<object[]> gsuchteFarben =
        new List<CardColor>() { CardColor.Schell, CardColor.Gras, CardColor.Eichel }
        .Select(c => new object[] { c });

    [Theory]
    [MemberData(nameof(gsuchteFarben))]
    public void Test_CanPlaySauspielForAnyGsuchteFarbe(CardColor gsuchteFarbe)
    {
        var deck = new CardsDeck();
        var sauspielCall = GameCall.Sauspiel(0, 1, gsuchteFarbe);
        var weiter = GameCall.Weiter();

        GameLog history;
        do
        {
            // either play a sauspiel or call weiter
            var table = new Table(
                new Player(0, new SauspielAgent(sauspielCall)),
                new Player(1, new RandomAgent(weiter)),
                new Player(2, new RandomAgent(weiter)),
                new Player(3, new RandomAgent(weiter)));
            var session = new GameSession(table, deck);
            history = session.ProcessGame();
        }
        // ensure that actually one sauspiel was played
        while (history.Call.Mode == GameMode.Weiter);

        history.Call.Mode.Should().Be(GameMode.Sauspiel);
        history.TurnCount.Should().Be(8);
    }

    public static IEnumerable<object[]> soloTrumpf
        => new List<CardColor>() {
                CardColor.Schell, CardColor.Herz,
                CardColor.Gras, CardColor.Eichel
            }
            .Select(x => new object[] { x });

    [Theory]
    [MemberData(nameof(soloTrumpf))]
    public void Test_CanPlaySolo(CardColor soloTrumpf)
    {
        var deck = new CardsDeck();
        var soloCall = GameCall.Solo(0, soloTrumpf);
        var weiter = GameCall.Weiter();

        var table = new Table(
            new Player(0, new SauspielAgent(soloCall)),
            new Player(1, new RandomAgent(weiter)),
            new Player(2, new RandomAgent(weiter)),
            new Player(3, new RandomAgent(weiter)));
        var session = new GameSession(table, deck);
        var history = session.ProcessGame();

        history.Call.Mode.Should().Be(GameMode.Solo);
        history.TurnCount.Should().Be(8);
    }

    [Fact]
    public void Test_CanPlayWenz()
    {
        var deck = new CardsDeck();
        var wenzCall = GameCall.Wenz(0);
        var weiter = GameCall.Weiter();

        var table = new Table(
            new Player(0, new SauspielAgent(wenzCall)),
            new Player(1, new RandomAgent(weiter)),
            new Player(2, new RandomAgent(weiter)),
            new Player(3, new RandomAgent(weiter)));
        var session = new GameSession(table, deck);
        var history = session.ProcessGame();

        history.Call.Mode.Should().Be(GameMode.Wenz);
        history.TurnCount.Should().Be(8);
    }
}

public class SauspielAgent : ISchafkopfAIAgent
{
    public SauspielAgent(GameCall callToMake)
        => this.callToMake = callToMake;

    private GameCall callToMake;

    private static readonly Random rng = new Random();

    public void OnGameFinished(GameResult result) { }

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => possibleCalls.ToArray().Contains(callToMake)
            ? callToMake : GameCall.Weiter();

    public Card ChooseCard(GameLog history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => false;

    public bool CallKontra(GameLog history)
        => false;

    public bool CallRe(GameLog history)
        => false;
}

public class RandomAgent : ISchafkopfAIAgent
{
    public RandomAgent() {}
    public RandomAgent(GameCall callToMake)
        => this.callToMake = callToMake;

    private GameCall? callToMake = null;

    private static readonly Random rng = new Random();

    public void OnGameFinished(GameResult result) { }

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => callToMake != null && possibleCalls.ToArray().Contains(callToMake.Value)
            ? callToMake.Value
            : possibleCalls[rng.Next(possibleCalls.Length)];

    public Card ChooseCard(GameLog history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => false;

    public bool CallKontra(GameLog history)
        => false;

    public bool CallRe(GameLog history)
        => false;
}
