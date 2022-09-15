namespace Schafkopf.Lib;

public class RandomAgent : ISchafkopfAIAgent
{
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

    public GameCall MakeCall(IEnumerable<GameCall> possibleCalls, int position, Hand hand, int klopfer)
        => possibleCalls.ElementAt(rng.Next(possibleCalls.Count()));

    public Card ChooseCard(GameHistory history, IEnumerable<Card> possibleCards)
        => possibleCards.ElementAt(rng.Next(possibleCards.Count()));

    public bool IsKlopfer(int position, IEnumerable<Card> firstFourCards)
        => false;

    public bool CallKontra(GameHistory history)
        => false;

    public bool CallRe(GameHistory history)
        => false;
}
