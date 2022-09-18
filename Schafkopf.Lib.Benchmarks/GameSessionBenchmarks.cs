namespace Schafkopf.Lib.Benchmarks;

public class GameSessionBenchmark
{
    // [Params(100, 10000, 1000000)]
    // public int NumGames;
    private GameSession session;

    [GlobalSetup]
    public void Init()
    {
        var deck = new CardsDeck();
        var table = new GameTable(
            new Player(0, new RandomAgent()),
            new Player(1, new RandomAgent()),
            new Player(2, new RandomAgent()),
            new Player(3, new RandomAgent()));
        session = new GameSession(table, deck);
    }

    [Benchmark]
    public void PlayGames_10k()
    {
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

    private static readonly Random rng = new Random();

    public void OnGameFinished(GameResult result) { }

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => callToMake ?? possibleCalls[rng.Next(possibleCalls.Length)];

    public Card ChooseCard(GameHistory history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => false;

    public bool CallKontra(GameHistory history)
        => false;

    public bool CallRe(GameHistory history)
        => false;
}
