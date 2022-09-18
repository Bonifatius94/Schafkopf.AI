namespace Schafkopf.Lib;

public interface ISchafkopfAIAgent
{
    GameCall MakeCall(
        ReadOnlySpan<GameCall> possibleCalls,
        int position, Hand hand, int klopfer);
    Card ChooseCard(GameLog history, ReadOnlySpan<Card> possibleCards);
    void OnGameFinished(GameResult result);

    bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards);
    bool CallKontra(GameLog history);
    bool CallRe(GameLog history);
}

public class Player
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

    public bool CallKontra(GameLog history)
        => agent.CallKontra(history);

    public bool CallRe(GameLog history)
        => agent.CallRe(history);

    public Card ChooseCard(GameLog history, ReadOnlySpan<Card> possibleCards)
        => agent.ChooseCard(history, possibleCards);

    public bool IsKlopfer(int position, ReadOnlySpan<Card> firstFourCards)
        => agent.IsKlopfer(position, firstFourCards);

    public GameCall MakeCall(
            ReadOnlySpan<GameCall> possibleCalls,
            int position, Hand hand, int klopfer)
        => agent.MakeCall(possibleCalls, position, hand, klopfer);

    public void OnGameFinished(GameResult result)
        => agent.OnGameFinished(result);
}
