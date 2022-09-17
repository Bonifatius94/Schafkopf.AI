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
    public void PlayGames()
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