using FluentAssertions;

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
}

public class RandomAgent : ISchafkopfAIAgent
{
    public RandomAgent() {}
    public RandomAgent(GameCall callToMake)
        => this.callToMake = callToMake;

    private GameCall? callToMake = null;

    private GameCall call;
    public Hand Hand { get; private set; }

    public int Id => throw new NotImplementedException();

    public void NewGame(GameCall call, Hand hand)
    {
        Hand = hand;
        this.call = call;
    }

    private static readonly Random rng = new Random();

    public Card ChooseCard(Turn state)
        => Hand.ElementAt(rng.Next(0, Hand.CardsCount));

    public void OnGameFinished(GameResult result) { }

    public GameCall MakeCall(
            IEnumerable<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => callToMake ?? possibleCalls.ElementAt(rng.Next(possibleCalls.Count()));

    public Card ChooseCard(GameHistory history, IEnumerable<Card> possibleCards)
        => possibleCards.ElementAt(rng.Next(possibleCards.Count()));

    public bool IsKlopfer(int position, IEnumerable<Card> firstFourCards)
        => false;

    public bool CallKontra(GameHistory history)
        => false;

    public bool CallRe(GameHistory history)
        => false;
}
