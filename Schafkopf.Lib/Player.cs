namespace Schafkopf.Lib;

public interface ISchafkopfAIAgent
{
    GameCall MakeCall(IEnumerable<GameCall> possibleCalls, int position, Hand hand, int klopfer);
    Card ChooseCard(GameHistory history, IEnumerable<Card> possibleCards);
    void OnGameFinished(GameResult result);

    bool IsKlopfer(int position, IEnumerable<Card> firstFourCards);
    bool CallKontra(GameHistory history);
    bool CallRe(GameHistory history);
}

public interface ISchafkopfPlayer : ISchafkopfAIAgent
{
    public int Id { get; }
    Hand Hand { get; }
    void NewGame(Hand hand);
    void Discard(Card card);
}

public class Player : ISchafkopfPlayer
{
    public Player(int id, ISchafkopfAIAgent agent)
    {
        Id = id;
        this.agent = agent;
    }

    private ISchafkopfAIAgent agent;

    public int Id { get; private set; }
    public Hand Hand { get; private set; }

    public void Discard(Card card)
        => Hand = Hand.Discard(card);

    public void NewGame(Hand hand)
        => Hand = hand;

    public bool CallKontra(GameHistory history)
        => agent.CallKontra(history);

    public bool CallRe(GameHistory history)
        => agent.CallRe(history);

    public Card ChooseCard(GameHistory history, IEnumerable<Card> possibleCards)
        => agent.ChooseCard(history, possibleCards);

    public bool IsKlopfer(int position, IEnumerable<Card> firstFourCards)
        => agent.IsKlopfer(position, firstFourCards);

    public GameCall MakeCall(IEnumerable<GameCall> possibleCalls, int position, Hand hand, int klopfer)
        => agent.MakeCall(possibleCalls, position, hand, klopfer);

    public void OnGameFinished(GameResult result)
        => agent.OnGameFinished(result);
}
