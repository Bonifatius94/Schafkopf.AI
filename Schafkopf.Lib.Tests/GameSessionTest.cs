namespace Schafkopf.Lib.Test;

public class TestGameTable
{
    [Fact]
    public void Test_YieldsAllPlayers_WhenIteratingFromZero()
    {
        var players = new Player[] {
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent())
        };
        var table = new GameTable(
            players[0], players[1], players[2], players[3]);

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
        var players = new Player[] {
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent())
        };
        var table = new GameTable(
            players[0], players[1], players[2], players[3]);

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
        var table = new GameTable(
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

        GameHistory history;
        do
        {
            // either play a sauspiel or call weiter
            var table = new GameTable(
                new Player(0, new SauspielAgent(sauspielCall)),
                new Player(1, new RandomAgent(weiter)),
                new Player(2, new RandomAgent(weiter)),
                new Player(3, new RandomAgent(weiter)));
            var session = new GameSession(table, deck);
            history = session.ProcessGame();
        }
        // ensure that actually one sauspiel was played
        while (history.Call.Mode == GameMode.Weiter);
    }

    // TODO: enforce at least one game call of each mode
}

public class SauspielAgent : ISchafkopfAIAgent
{
    public SauspielAgent(GameCall callToMake)
        => this.callToMake = callToMake;

    private GameCall callToMake;

    private static readonly Random rng = new Random();

    public void OnGameFinished(GameResult result) { }

    public GameCall MakeCall(
            IEnumerable<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => possibleCalls.Contains(callToMake) ? callToMake : GameCall.Weiter();

    public Card ChooseCard(GameHistory history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, IEnumerable<Card> firstFourCards)
        => false;

    public bool CallKontra(GameHistory history)
        => false;

    public bool CallRe(GameHistory history)
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
            IEnumerable<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => callToMake != null && possibleCalls.Contains(callToMake.Value) ? callToMake.Value
            : possibleCalls.ElementAt(rng.Next(possibleCalls.Count()));

    public Card ChooseCard(GameHistory history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, IEnumerable<Card> firstFourCards)
        => false;

    public bool CallKontra(GameHistory history)
        => false;

    public bool CallRe(GameHistory history)
        => false;
}
