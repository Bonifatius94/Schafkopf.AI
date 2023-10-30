namespace Schafkopf.Lib;

public class RandomGameplay
{
    public IEnumerable<GameLog> Generate(int n, int? seed = null)
    {
        var agent = new RandomAgent(seed);
        var deck = new CardsDeck();
        var table = new Table(
            new Player(0, agent),
            new Player(1, agent),
            new Player(2, agent),
            new Player(3, agent));
        var session = new GameSession(table, deck);

        for (int i = 0; i < n; i++)
            yield return session.ProcessGame();
    }
}

public class RandomAgent : ISchafkopfAIAgent
{
    public RandomAgent(int? seed = null)
    {
        rng = seed != null ? new Random(seed.Value) : new Random();
    }

    private Random rng;

    public void OnGameFinished(GameLog result) { }

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => possibleCalls[rng.Next(possibleCalls.Length)];

    public Card ChooseCard(GameLog history, ReadOnlySpan<Card> possibleCards)
        => possibleCards[rng.Next(possibleCards.Length)];

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => false;

    public bool CallKontra(GameLog history)
        => false;

    public bool CallRe(GameLog history)
        => false;
}
